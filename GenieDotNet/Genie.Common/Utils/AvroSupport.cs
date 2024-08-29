using Chr.Avro.Abstract;
using Chr.Avro.Confluent;
using Chr.Avro.Serialization;
using Confluent.SchemaRegistry;
using Genie.Common.Utils.Cosmos;

namespace Genie.Common.Utils;



public class AvroSupport
{
    public static SchemaRegistryConfig GetSchemaRegistryConfig() => new() { Url = "http://registry:8081" };

    public static SchemaBuilder GetSchemaBuilder() => new(SchemaBuilder.CreateDefaultCaseBuilders(nullableReferenceTypeBehavior: NullableReferenceTypeBehavior.All)
        .Prepend(builder => new NetopoolgyFeatureCollectionSchemaBuilderCase()));

    public static SchemaRegistrySerializerBuilder GetSerializerBuilder(SchemaRegistryConfig registryConfig, SchemaBuilder schemaBuilder) =>
        new (registryConfig, schemaBuilder, serializerBuilder: new BinarySerializerBuilder(
                    BinarySerializerBuilder.CreateDefaultCaseBuilders().Prepend(builder => new NetopoolgyFeatureCollectionSerializerBuilderCase(builder))));

    public static BinarySerializerBuilder GetSerializerBuilder() =>
        new (BinarySerializerBuilder.CreateDefaultCaseBuilders().Prepend(builder => new NetopoolgyFeatureCollectionSerializerBuilderCase(builder)));

    public static BinaryDeserializerBuilder GetBinaryDeserializerBuilder() => new(BinaryDeserializerBuilder.CreateDefaultCaseBuilders()
            .Prepend(builder => new NetopoolgyFeatureCollectionDeserializerBuilderCase(builder)));

    public static T GetTypedMessage<T>(byte[] val, AsyncSchemaRegistryDeserializer<T> deserializer)
    {
        return deserializer.DeserializeAsync(val, false, new Confluent.Kafka.SerializationContext()).GetAwaiter().GetResult();
    }
}
