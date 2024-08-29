using Chr.Avro.Abstract;
using Chr.Avro;
using NetTopologySuite.Features;
using Chr.Avro.Serialization;
using System.Linq.Expressions;
using GC = Genie.Common.Types;
using BinaryWriter = Chr.Avro.Serialization.BinaryWriter;

namespace Genie.Common.Utils.Cosmos;

public class NetopoolgyFeatureCollectionDeserializerBuilderCase(IBinaryDeserializerBuilder builder) : BinaryUnionDeserializerBuilderCase(builder)
{
    private readonly BinaryStringDeserializerBuilderCase stringDeserializer = new();

    public override BinaryDeserializerBuilderCaseResult BuildExpression(Type type, Schema schema, BinaryDeserializerBuilderContext context)
    {
        if ((type == typeof(Feature) || type == typeof(IFeature)) && schema is StringSchema)
        {
            var expression = stringDeserializer.BuildExpression(typeof(string), schema, context).Expression;
            var deserialize = typeof(GC.NetTopologyFeatureCollection).GetMethod(nameof(GC.NetTopologyFeatureCollection.Deserialize), [typeof(string)]);

            try
            {
                return BinaryDeserializerBuilderCaseResult.FromExpression(
                    BuildConversion(
                        Expression.Call(deserialize!, expression!),
                        type));
            }
            catch (InvalidOperationException exception)
            {
                throw new UnsupportedTypeException(type, $"Failed to map {schema} to {type}.", exception);
            }
        }

        return BinaryDeserializerBuilderCaseResult.FromException(
                new UnsupportedTypeException(type, $"{nameof(BinaryDeserializerBuilderCaseResult)} can only be applied to the {nameof(type)} type."));
    }
}

public class NetopoolgyFeatureCollectionSerializerBuilderCase(IBinarySerializerBuilder serializerBuilder) : BinaryUnionSerializerBuilderCase(serializerBuilder)
{
    public override BinarySerializerBuilderCaseResult BuildExpression(Expression value, Type type, Schema schema, BinarySerializerBuilderContext context)
    {
        if (type == typeof(Feature) || type == typeof(IFeature))
        {
            return BinarySerializerBuilderCaseResult.FromExpression(
                   Expression.Call(context.Writer,
                   typeof(BinaryWriter).GetMethod(nameof(BinaryWriter.WriteString), [typeof(string)])!,
                   GetExpression(value, typeof(string))));

            Expression GetExpression(Expression value, Type target)
            {
                var methodInfo = typeof(GC.NetTopologyFeatureCollection).GetMethod(nameof(GC.NetTopologyFeatureCollection.Serialize), [typeof(Feature)]);
                value = Expression.Call(methodInfo!, value);
                return BuildStaticConversion(value, target);
            }
        }

        return BinarySerializerBuilderCaseResult.FromException(new UnsupportedTypeException(type,
            $"{nameof(NetopoolgyFeatureCollectionSerializerBuilderCase)} can only be applied to the {nameof(type)} type."));
    }
}

public class NetopoolgyFeatureCollectionSchemaBuilderCase : SchemaBuilderCase, ISchemaBuilderCase
{
    public HashSet<string> Dupes = [];

    public SchemaBuilderCaseResult BuildSchema(Type type, SchemaBuilderContext context)
    {
        SchemaBuilder schemaBuilder = new(SchemaBuilder.CreateDefaultCaseBuilders(nullableReferenceTypeBehavior: NullableReferenceTypeBehavior.All));

        if (type == typeof(Feature) || type == typeof(IFeature))
            return SchemaBuilderCaseResult.FromSchema(new StringSchema());
        else if (!string.IsNullOrEmpty(type.Namespace) && type.Namespace.StartsWith("NetTopologySuite"))
            return SchemaBuilderCaseResult.FromSchema(new NullSchema());
        else if (type == typeof(IAttributesTable))
            return SchemaBuilderCaseResult.FromSchema(new NullSchema());
        return SchemaBuilderCaseResult.FromException(new UnsupportedTypeException(type,
            $"{nameof(NetopoolgyFeatureCollectionSchemaBuilderCase)} can only be applied to the {nameof(type)} type."));
    }
}
