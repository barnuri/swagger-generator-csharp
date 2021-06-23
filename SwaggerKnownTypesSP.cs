using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

public class SwaggerKnownTypesSP : ISchemaProcessor
{
    public void Process(SchemaProcessorContext context)
    {
        foreach (var knownType in context.Type.GetCustomAttributes(typeof(KnownTypeAttribute), true).Cast<KnownTypeAttribute>().Where(t => t.Type != context.Type))
        {
            AddKnownType(context, knownType.Type!);
        }
        foreach (var bsonKnownTypes in context.Type.GetCustomAttributes<BsonKnownTypesAttribute>(true))
        {
            foreach (var known in bsonKnownTypes.KnownTypes.Where(t => t != context.Type))
            {
                AddKnownType(context, known);
            }
        }
        for (int i = 0; i < context.Schema.AnyOf.Count; i++)
        {
            var schema = context.Schema.AnyOf.ToList()[i];
            if (schema.Title == context.Type.Name)
            {
                context.Schema.AnyOf.Remove(schema);
                i--;
            }
        }
    }

    private void AddKnownType(SchemaProcessorContext context, Type anyOfType)
    {
        var anyOfSchema = JsonSchema.FromType(anyOfType, new JsonSchemaGeneratorSettings
        {
            SerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }
        });
        if (!anyOfType.IsSubclassOf(context.Type) || anyOfSchema.Title == context.Type.Name)
        {
            return;
        }
        context.Schema.AnyOf.Add(anyOfSchema);
    }
}