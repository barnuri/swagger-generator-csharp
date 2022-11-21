using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class SwaggerFieldsDP : IDocumentProcessor
{
    public void Process(DocumentProcessorContext context)
    {
        var types = new List<Type>();
        AppDomain.CurrentDomain.GetAssemblies().ToList().ForEach(x => types.AddRange(x.GetTypes()));
        types = types.Where(t => context.SchemaResolver.HasSchema(t, false)).ToList();
        foreach (var type in types)
        {
            var schema = context.SchemaResolver.GetSchema(type, false);
            var missingFields = type.GetFields().Where(field => !schema.Properties.Any(x => x.Key == CamelCase(field.Name))).ToList();
            foreach (var field in missingFields)
            {
                var fieldSchema = GetApiSchema(field.FieldType, context);
                if (schema.Properties.Any(x => x.Key == CamelCase(field.Name))) { continue; }
                schema.Properties.Add(new KeyValuePair<string, JsonSchemaProperty>(CamelCase(field.Name), fieldSchema));
            }
        }
    }

    private string CamelCase(string name) => new DefaultContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() }.GetResolvedPropertyName(name);

    private (Type type, bool isNullable) GetTypeForSwagger(Type type, DocumentProcessorContext context, bool getGenericProp = false)
    {
        bool isNullable = false;
        if ((type.FullName ?? "").Contains("System.Nullable"))
        {
            isNullable = true;
            type = type.GetGenericArguments()[0];
        }
        if (getGenericProp)
        {
            try
            {
                type = type.GetGenericArguments()[0];
            }
            catch
            {
                type = typeof(object);
            }
        }

        return (type, isNullable);
    }

    private JsonSchemaProperty? SimpleType(Type type, DocumentProcessorContext context, int deep = 0)
    {
        var res = GetTypeForSwagger(type, context);
        type = res.type;
        var fieldSchema = new JsonSchemaProperty();
        fieldSchema.IsNullableRaw = res.isNullable;
        var typeCode = Type.GetTypeCode(type);
        if (deep > 20) { return new JsonSchemaProperty { Type = JsonObjectType.Object }; }
        if (typeCode == TypeCode.Boolean) { fieldSchema.Type = JsonObjectType.Boolean; }
        else if (typeCode == TypeCode.String) { fieldSchema.Type = JsonObjectType.String; }
        else if (typeCode == TypeCode.Byte) { fieldSchema.Type = JsonObjectType.Integer; }
        else if (typeCode == TypeCode.SByte) { fieldSchema.Type = JsonObjectType.Number; }
        else if (typeCode == TypeCode.UInt16) { fieldSchema.Type = JsonObjectType.Integer; }
        else if (typeCode == TypeCode.Decimal) { fieldSchema.Type = JsonObjectType.Number; }
        else if (typeCode == TypeCode.Int16) { fieldSchema.Type = JsonObjectType.Integer; }
        else if (typeCode == TypeCode.DateTime)
        {
            fieldSchema.Type = JsonObjectType.String;
            fieldSchema.Format = "date-time";
        }
        else if (typeCode == TypeCode.UInt32)
        {
            fieldSchema.Type = JsonObjectType.Integer;
            fieldSchema.Format = "int32";
        }
        else if (typeCode == TypeCode.UInt64)
        {
            fieldSchema.Type = JsonObjectType.Integer;
            fieldSchema.Format = "int64";
        }
        else if (typeCode == TypeCode.Int32)
        {
            fieldSchema.Type = JsonObjectType.Integer;
            fieldSchema.Format = "int32";
        }
        else if (typeCode == TypeCode.Int64)
        {
            fieldSchema.Type = JsonObjectType.Integer;
            fieldSchema.Format = "int64";
        }
        else if (typeCode == TypeCode.Double)
        {
            fieldSchema.Type = JsonObjectType.Number;
            fieldSchema.Format = "double";
        }
        else if (typeCode == TypeCode.Single)
        {
            fieldSchema.Type = JsonObjectType.Number;
            fieldSchema.Format = "float";
        }
        else if (typeof(ICollection).IsAssignableFrom(type))
        {
            fieldSchema.Type = JsonObjectType.Array;
            var arrayType = GetTypeForSwagger(type, context, true).type;
            fieldSchema.Item = GetApiSchema(arrayType, context);
        }
        else if (type.GetTypeInfo().IsEnum)
        {
            fieldSchema.Type = JsonObjectType.Number;
        }
        else if (!type.GetTypeInfo().IsClass)
        {
            fieldSchema.Type = JsonObjectType.Object;
        }
        else { return null; }

        if (fieldSchema.Type == JsonObjectType.String)
        {
            fieldSchema.IsNullableRaw = true;
        }

        return fieldSchema;
    }

    private JsonSchemaProperty GetApiSchema(Type type, DocumentProcessorContext context, int deep = 0)
    {
        var fieldSchema = SimpleType(type, context, deep);
        var res = GetTypeForSwagger(type, context);
        type = res.type;
        if (fieldSchema == null)
        {
            var props = type.GetProperties().Select(x => (name: x.Name, type: x.PropertyType));
            var fields = type.GetFields().Select(x => (name: x.Name, type: x.FieldType));
            foreach (var prop in props.Concat(fields))
            {
                // create new or get schema for nestad props & fields
                if (context.SchemaResolver.HasSchema(type, false)) { continue; }
                FactoryMethod(type, context, deep);
            }
            // ref schema
            fieldSchema = new JsonSchemaProperty { Reference = context.SchemaResolver.GetSchema(type, false) };
        }

        return fieldSchema;
    }

    private JsonSchemaProperty FactoryMethod(Type type, DocumentProcessorContext context, int deep = 0)
    {
        if (deep > 20) { return new JsonSchemaProperty { Type = JsonObjectType.Object }; }
        var fieldSchema = SimpleType(type, context, deep);
        var res = GetTypeForSwagger(type, context);
        type = res.type;
        if (fieldSchema == null)
        {
            fieldSchema = new JsonSchemaProperty { Type = JsonObjectType.Object };

            var props = type.GetProperties().Select(x => (name: x.Name, type: x.PropertyType));
            var fields = type.GetFields().Select(x => (name: x.Name, type: x.FieldType));
            foreach (var prop in props.Concat(fields))
            {
                // create new schema
                fieldSchema.Properties.Add(new KeyValuePair<string, JsonSchemaProperty>(CamelCase(prop.name), GetApiSchema(prop.type, context, deep + 1)));
            }
        }

        return fieldSchema;
    }
}



