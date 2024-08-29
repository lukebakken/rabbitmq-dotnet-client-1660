using Genie.Common;
using Mediator;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using Chr.Avro.Abstract;
using Genie.Web.Api.Common;
using Google.Protobuf.WellKnownTypes;
using Google.Protobuf;
using Genie.Common.Web;
namespace Genie.Web.Api.Mediator.Commands;

public record RabbitMQCommand(ObjectPool<RabbitMQPooledObject> GeniePool, SchemaBuilder SchemaBuilder, bool FireAndForget) : IRequest;

public class RabbitMQCommandHandler(GenieContext genieContext) : BaseCommandHandler(genieContext), IRequestHandler<RabbitMQCommand>
{
    public async ValueTask<Unit> Handle(RabbitMQCommand command, CancellationToken cancellationToken)
    {
        bool useAvro = false;

        var grpc = MockPartyCreator.GetParty();

        var pooledObj = command.GeniePool.Get();

        //var pooledObj = new RabbitMQPooledObject();

        if (pooledObj.Counter == 0)
            await pooledObj.Configure(command.SchemaBuilder, this.Context);

        var bytes = Any.Pack(grpc).ToByteArray();

        var props = new BasicProperties { ReplyTo = command.FireAndForget ? null : pooledObj.EventChannel };
        await pooledObj.Ingress!.BasicPublishAsync(this.Context.RabbitMQ.Exchange, this.Context.RabbitMQ.RoutingKey, props, bytes);

        var success = command.FireAndForget || pooledObj.ReceiveSignal.WaitOne(30000);

        pooledObj.Counter++;
        command.GeniePool.Return(pooledObj);

        //pooledObj.Reset();


        if (command.FireAndForget)
            return await Task.FromResult(new Unit());
        else if (pooledObj.Result?.Status == Genie.Common.Types.EventTaskJobStatus.Errored)
            throw new Exception("Actor Error: " + pooledObj.Result?.Exception);
        else if (!success)
            throw new Exception("No Response from server............................................");
        else
            return new Unit();
    }
}