using Avro.Generic;
using Azure.Storage.Blobs;
using Confluent.Kafka;
using Genie.Common.Settings;
using MaxMind.Db;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace Genie.Common
{
    public class GenieContext
    {
        public bool Simple { get; set; }

        public RabbitMQSettings RabbitMQ { get; set; }


        public GenieContext(IConfigurationRoot Configuration)
        {
            Simple = bool.Parse(Configuration["Benchmark:Simple"]!);

            RabbitMQ = new(Configuration["Rabbit:exchange"]!, Configuration["Rabbit:queue"]!, Configuration["Rabbit:routingKey"]!,
                Configuration["Rabbit:user"]!, Configuration["Rabbit:pass"]!, Configuration["Rabbit:vhost"]!, Configuration["Rabbit:host"]!);

        }

        public static (GenieContext GenieContext, HostApplicationBuilder Host) Build()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();

            var host = Host.CreateApplicationBuilder();
            host.Configuration.AddConfiguration(configuration);

            return new(new GenieContext(host.Configuration), host);
        }
    }
}
