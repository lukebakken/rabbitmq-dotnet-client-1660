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

public class RabbitMQServiceSingleThreaded
{
    private static readonly RecyclableMemoryStreamManager manager = new();

    public static async Task Start()
    {
        var context = GenieContext.Build().GenieContext;


        await RabbitMq(context);
        Console.WriteLine("RabbitMQ exited");
    }

    public static async Task<(IConnection Connection, IChannel IngressChannel, IChannel EventChannel)> Channels()
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
        return (conn, ingressChannel, eventChannel);
    }

    public static async Task RabbitMq(GenieContext context)
    {
        try
        {
            var schemaBuilder = AvroSupport.GetSchemaBuilder();
            var serializer = AvroSupport.GetSerializerBuilder().BuildDelegate<EventTaskJob>(schemaBuilder.BuildSchema<EventTaskJob>());

            using CancellationTokenSource cts = new();
            Console.WriteLine("Starting RabbitMQ Pump: " + cts.Token);

            var (Connection, IngressChannel, EventChannel) = await Channels();
            Connection.ConnectionBlocked += Connection_ConnectionBlocked;
            Connection.CallbackException += Connection_CallbackException;
            Connection.ConnectionRecoveryError += Connection_ConnectionRecoveryError;
            Connection.ConnectionShutdown += Connection_ConnectionShutdown;
            Connection.ConnectionUnblocked += Connection_ConnectionUnblocked;
            Connection.RecoveringConsumer += Connection_RecoveringConsumer;
            Connection.RecoverySucceeded += Connection_RecoverySucceeded;

            IngressChannel.CallbackException += Channel_CallbackException;
            IngressChannel.BasicReturn += Channel_BasicReturn;
            IngressChannel.ChannelShutdown += Channel_ChannelShutdown;
            IngressChannel.FlowControl += Channel_FlowControl;

            EventChannel.CallbackException += Channel_CallbackException;
            EventChannel.BasicReturn += Channel_BasicReturn;
            EventChannel.ChannelShutdown += Channel_ChannelShutdown;
            EventChannel.FlowControl += Channel_FlowControl;

            var consumer = new AsyncEventingBasicConsumer(IngressChannel);

            var timerService = new CounterConsoleLogger();

            bool useAvro = false;
            AutoResetEvent WaitHandle = new(false);

            BasicDeliverEventArgs? message = null;
            consumer.Received += (sender, ea) =>
            {
                message = ea;
                WaitHandle.Set();
                return Task.CompletedTask;
            };

            try
            {
                string consumerTag = await IngressChannel.BasicConsumeAsync(context.RabbitMQ.Queue, true, consumer);
                while (true)
                {
                    var wait = WaitHandle.WaitOne(30000);
                    if (!wait)
                    {
                        continue;
                    }

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

                        await EventChannel.BasicPublishAsync(message.BasicProperties.ReplyTo, context.RabbitMQ.RoutingKey, ms.GetReadOnlySequence().ToArray());
                    }
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

                    await EventChannel.BasicPublishAsync(message.BasicProperties.ReplyTo, context.RabbitMQ.RoutingKey, ms.GetReadOnlySequence().ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            await File.AppendAllTextAsync(@"c:\temp\rabbiterror.log", ex.ToString());
        }
    }

    private static void Channel_FlowControl(object? sender, FlowControlEventArgs e)
    {
        Console.WriteLine("{0} [INFO] saw channel.flow, active: {1}", DateTime.Now, e.Active);
    }

    private static void Channel_ChannelShutdown(object? sender, ShutdownEventArgs e)
    {
        Console.WriteLine("{0} [INFO] saw channel shutdown", DateTime.Now);
        Console.WriteLine("cause: {0}", e.Cause);
        Console.WriteLine("classId: {0}", e.ClassId);
        Console.WriteLine("exception: {0}", e.Exception);
    }

    private static void Channel_BasicReturn(object? sender, BasicReturnEventArgs e)
    {
        Console.WriteLine("{0} [INFO] saw basic.return, replyCode: {1}", DateTime.Now, e.ReplyCode);
    }

    private static void Channel_CallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        Console.WriteLine("{0} [INFO] saw channel callback exception", DateTime.Now);
        Console.WriteLine("exception: {0}", e.Exception);
    }

    private static void Connection_RecoverySucceeded(object? sender, EventArgs e)
    {
        Console.WriteLine("{0} [INFO] connection recovery succeeded", DateTime.Now);
    }

    private static void Connection_RecoveringConsumer(object? sender, RecoveringConsumerEventArgs e)
    {
        Console.WriteLine("{0} [INFO] connection is recovering consumer, consumerTag: {1}", DateTime.Now, e.ConsumerTag);
    }

    private static void Connection_ConnectionUnblocked(object? sender, EventArgs e)
    {
        Console.WriteLine("{0} [INFO] connection is unblocked", DateTime.Now);
    }

    private static void Connection_ConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        Console.WriteLine("{0} [INFO] connection is blocked", DateTime.Now);
    }

    private static void Connection_ConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        Console.WriteLine("{0} [INFO] saw connection shutdown", DateTime.Now);
        Console.WriteLine("cause: {0}", e.Cause);
        Console.WriteLine("classId: {0}", e.ClassId);
        Console.WriteLine("exception: {0}", e.Exception);
    }

    private static void Connection_ConnectionRecoveryError(object? sender, ConnectionRecoveryErrorEventArgs e)
    {
        Console.WriteLine("{0} [INFO] saw connection recovery error", DateTime.Now);
        Console.WriteLine("exception: {0}", e.Exception);
    }

    private static void Connection_CallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        Console.WriteLine("{0} [INFO] saw connection callback exception", DateTime.Now);
        Console.WriteLine("exception: {0}", e.Exception);
    }
}