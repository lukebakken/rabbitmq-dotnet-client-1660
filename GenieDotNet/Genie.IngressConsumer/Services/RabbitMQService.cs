using Genie.Common;
using Genie.Common.Adapters.RabbitMQ;
using Genie.Common.Types;
using Genie.Common.Utils;
using Google.Protobuf.WellKnownTypes;
using Microsoft.IO;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Buffers;



namespace Genie.IngressConsumer.Services;


public class RabbitMQService
{
    private static readonly RecyclableMemoryStreamManager manager = new();

    public static async Task Start()
    {
        var context = GenieContext.Build().GenieContext;

        await RabbitMq(context);
        Console.WriteLine("RabbitMQ exited");
    }

    public static async Task<(IChannel IngressChannel, IChannel EventChannel)> Channels()
    {
        var args = new Dictionary<string, object>();
        args.Add("x-max-length", 10000);

        var context = GenieContext.Build().GenieContext;

        var conn = await RabbitUtils.GetConnection(context.RabbitMQ, true);

        var ingressChannel = await conn.CreateChannelAsync();

        await ingressChannel.ExchangeDeclareAsync(context.RabbitMQ.Exchange, ExchangeType.Direct);
        await ingressChannel.QueueDeclareAsync(context.RabbitMQ.Queue, false, false, false, args);
        await ingressChannel.QueueBindAsync(context.RabbitMQ.Queue, context.RabbitMQ.Exchange, context.RabbitMQ.RoutingKey, null);

        var eventChannel = await conn.CreateChannelAsync();
        return (ingressChannel, eventChannel);
    }


    public static async Task RabbitMq(GenieContext context)
    {
        try
        {
            var schemaBuilder = AvroSupport.GetSchemaBuilder();
            var serializer = AvroSupport.GetSerializerBuilder().BuildDelegate<EventTaskJob>(schemaBuilder.BuildSchema<EventTaskJob>());

            using CancellationTokenSource cts = new();
            Console.WriteLine("Starting RabbitMQ Pump: " + cts.Token);

            var channels = await Channels();

            var consumer = new AsyncEventingBasicConsumer(channels.IngressChannel);

            var timerService = new CounterConsoleLogger();

            var pump = RabbitMQPump<byte[]>.Run(
                consumer,
                async message =>
                {
                    try
                    {
                        timerService.Process();

                        Grpc.PartyBenchmarkRequest proto = Any.Parser.ParseFrom(message.Body.ToArray()).Unpack<Grpc.PartyBenchmarkRequest>();

                        if (!string.IsNullOrEmpty(message.BasicProperties.ReplyTo))
                        {
                            using var ms = manager.GetStream();
                            serializer(new EventTaskJob
                            {
                                //Id = proto.Request.CosmosBase.Identifier.Id,
                                Job = "Report",
                                Status = EventTaskJobStatus.Completed
                            }, new Chr.Avro.Serialization.BinaryWriter(ms));

                            await channels.EventChannel.BasicPublishAsync(message.BasicProperties.ReplyTo, context.RabbitMQ.RoutingKey, ms.GetReadOnlySequence().ToArray());
                        }

                    }
                    catch (Exception ex)
                    {
                        timerService.ProcessError();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error:" + ex.ToString());

                        if (!string.IsNullOrEmpty(message.BasicProperties.ReplyTo))
                        {
                            using var ms = manager.GetStream();
                            serializer(new EventTaskJob
                            {
                                Exception = ex.Message,
                                Status = EventTaskJobStatus.Errored
                            }, new Chr.Avro.Serialization.BinaryWriter(ms));

                            await channels.EventChannel.BasicPublishAsync(message.BasicProperties.ReplyTo, context.RabbitMQ.RoutingKey, ms.GetReadOnlySequence().ToArray());
                        }
                    }
                },
                maxDegreeOfParallelism: 16,
                cts.Token);


            string consumerTag = await channels.IngressChannel.BasicConsumeAsync(context.RabbitMQ.Queue, true, consumer);
            await pump.Completion;
        }

        catch (Exception ex)
        {
            await File.AppendAllTextAsync(@"c:\temp\rabbiterror.log", ex.ToString());
        }

    }
}