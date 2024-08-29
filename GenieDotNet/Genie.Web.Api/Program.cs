using Chr.Avro.Confluent;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Genie.Common;
using Genie.Common.Performance;
using Genie.Common.Utils;
using Genie.Web.Api.Common;
using Genie.Web.Api.Rest;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using ZLogger;
using ZLogger.Providers;


var app = Build(args);
Configure(app);
app.Run();


static WebApplication Build(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddGrpc();


    var genieContext = GenieContext.Build().GenieContext;
    builder.Services.AddSingleton(genieContext);

    // Shared
    var config = AvroSupport.GetSchemaRegistryConfig();
    var schemaRegistry = new CachedSchemaRegistryClient(config);
    var schemaBuilder = AvroSupport.GetSchemaBuilder();

    builder.Services.AddSingleton(schemaRegistry);
    builder.Services.AddSingleton(schemaBuilder);

    // Object Pools
    builder.Services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();


    builder.Services.TryAddSingleton(serviceProvider =>
    {
        var provider = serviceProvider.GetRequiredService<ObjectPoolProvider>();
        var policy = new DefaultPooledObjectPolicy<RabbitMQPooledObject>();
        return provider.Create(policy);
    });

    

    // Mediator
    builder.Services.AddMediator(options =>
    {
        options.Namespace = "Genie.Mediator";
        options.ServiceLifetime = ServiceLifetime.Transient;
    });


    builder.Logging
        .AddZLoggerRollingFile(options => {

            // File name determined by parameters to be rotated
            options.FilePathSelector = (timestamp, sequenceNumber) => $"c:\\temp\\web\\{timestamp.ToLocalTime():yyyy-MM-dd}_{sequenceNumber:000}.log";

            // The period of time for which you want to rotate files at time intervals.
            options.RollingInterval = RollingInterval.Day;

            // Limit of size if you want to rotate by file size. (KB)
            options.RollingSizeKB = 1024;
        })
        .AddZLoggerConsole(options =>
         {
             options.UseJsonFormatter();
         })
        // Format as json and configure output
        .AddZLoggerConsole(options =>
        {
            options.UseJsonFormatter(formatter =>
            {
                formatter.IncludeProperties = IncludeProperties.ParameterKeyValues;
            });
        })
        // Further common settings
        .AddZLoggerConsole(options =>
        {
            // Enable LoggerExtensions.BeginScope
            options.IncludeScopes = true;
        });

    return builder.Build();
}

static void Configure(WebApplication app)
{
    PartyEndpoints.Map(app);
}
