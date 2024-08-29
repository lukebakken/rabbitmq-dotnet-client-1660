using Chr.Avro.Abstract;
using Genie.Web.Api.Common;
using Genie.Web.Api.Mediator.Commands;
using Mediator;
using Microsoft.Extensions.ObjectPool;
using System.Net;

namespace Genie.Web.Api.Rest
{
    public static class PartyEndpoints
    {
        public static void Map(WebApplication app)
        {
            app.MapGet("rabbit", async
                (ObjectPool<RabbitMQPooledObject> geniePool,
                SchemaBuilder schemaBuilder,
                IMediator mediator) =>
            {
                var cmd = new RabbitMQCommand(geniePool, schemaBuilder, false);
                var result = await mediator.Send(cmd);
                return HttpStatusCode.OK;
            });
        }
    }
}

