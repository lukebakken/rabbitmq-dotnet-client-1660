using Chr.Avro.Abstract;
using Chr.Avro.Confluent;
using Chr.Avro.Serialization;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Genie.Common;
using Genie.Common.Adapters.RabbitMQ;
using Genie.Common.Performance;
using Genie.Common.Types;
using Genie.Common.Utils;
using Genie.Grpc;
using Microsoft.IO;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Buffers;
using System.Threading;

namespace Genie.Web.Api.Common;

public class RabbitMQPooledObject : GeniePooledObject
{
    public IConnection? Connect { get; set; }
    public IChannel? Ingress { get; set; }
    public IChannel? Events { get; set; }
    public AutoResetEvent ReceiveSignal = new(false);
    //private EventingBasicConsumer? Consumer;
    public EventTaskJob? Result { get; set; }
    private static readonly RecyclableMemoryStreamManager manager = new();
    private ISerializer<PartyBenchmarkRequest> Serializer { get; set; }
    private BinaryDeserializer<EventTaskJob> Deserializer { get; set; }
    private AsyncEventingBasicConsumer AsyncHandler { get; set; }

    public void Reset()
    {
        Connect?.Dispose();
        Ingress?.Dispose();
        Events?.Dispose();

        Connect = null;
        Ingress = null;
        Events = null;
        ReceiveSignal = new(false);
        //Consumer = null;
    }

    public async Task Configure(SchemaBuilder schemaBuilder, GenieContext genieContext)
    {
        ReceiveSignal = new(false);

        var args = new Dictionary<string, object>();
        args.Add("x-max-length", 10000);

        this.Connect = await RabbitUtils.GetConnection(genieContext.RabbitMQ, false);


        this.Ingress = await this.Connect.CreateChannelAsync();
        this.Events =  await this.Connect.CreateChannelAsync();


        await this.Ingress.ExchangeDeclareAsync(genieContext.RabbitMQ.Exchange, ExchangeType.Direct);
        await this.Ingress.QueueDeclareAsync(genieContext.RabbitMQ.Queue, false, false, false, args);
        await this.Ingress.QueueBindAsync(genieContext.RabbitMQ.Queue, genieContext.RabbitMQ.Exchange, genieContext.RabbitMQ.RoutingKey, null);

        await this.Events.ExchangeDeclareAsync(this.EventChannel, ExchangeType.Direct);
        await this.Events.QueueDeclareAsync(this.EventChannel, false, false, false, args);
        await this.Events.QueueBindAsync(this.EventChannel, this.EventChannel, genieContext.RabbitMQ.RoutingKey, null);

        AsyncHandler = new AsyncEventingBasicConsumer(this.Events);
        var result = await this.Events.BasicConsumeAsync(this.EventChannel, true, AsyncHandler, new CancellationToken());
        var schema = schemaBuilder.BuildSchema<EventTaskJob>();
        var deserializerBuilder = AvroSupport.GetBinaryDeserializerBuilder();
        Deserializer = deserializerBuilder.BuildDelegate<EventTaskJob>(schema);

        AsyncHandler.Received += EventReceived;

        var config = AvroSupport.GetSchemaRegistryConfig();
        var schemaRegistry = new CachedSchemaRegistryClient(config);

    }

    private Task EventReceived(object sender, BasicDeliverEventArgs @event)
    {
        var reader = new Chr.Avro.Serialization.BinaryReader(@event.Body.ToArray());
        Result = Deserializer(ref reader);
        ReceiveSignal.Set();

        return Task.CompletedTask;
    }
}