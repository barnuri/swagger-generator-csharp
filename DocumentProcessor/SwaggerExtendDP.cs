using Newtonsoft.Json;
using NJsonSchema;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

public class SwaggerExtendDP : IDocumentProcessor
{
    public void Process(DocumentProcessorContext context)
    {
        var types = new List<Type>();
        AppDomain.CurrentDomain.GetAssemblies().ToList().ForEach(x => types.AddRange(x.GetTypes()));
        types = types.Where(t => context.SchemaResolver.HasSchema(t, false)).ToList();
        foreach (var type in types)
        {
            object? instance = null;
            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch { }
            var schema = context.SchemaResolver.GetSchema(type, false);
            var excludedProperties = new List<string>();
            var excludeAttributes = new Type[]
            {
                typeof(SwaggerExcludeAttribute),
                typeof(InversePropertyAttribute),
                typeof(ForeignKeyAttribute),
                typeof(JsonIgnoreAttribute),
            };
            excludedProperties = excludedProperties.Distinct().ToList();

            var uniqueItemsProperties = new List<string>();
            uniqueItemsProperties.AddRange(PowerfulGetProperties(type).Where(t => t.GetCustomAttribute(typeof(SwaggerUniqueItemsAttribute)) != null).Select(t => t.Name.ToLower()));
            uniqueItemsProperties.AddRange(PowerfulGetFields(type).Where(t => t.GetCustomAttribute(typeof(SwaggerUniqueItemsAttribute)) != null).Select(t => t.Name.ToLower()));

            var ignoreInheritProps = new List<string>();
            ignoreInheritProps.AddRange(PowerfulGetProperties(type).Where(t => t.GetCustomAttribute(typeof(SwaggerIgnoreInheritPropsAttribute)) != null).Select(t => t.Name.ToLower()));
            ignoreInheritProps.AddRange(PowerfulGetFields(type).Where(t => t.GetCustomAttribute(typeof(SwaggerIgnoreInheritPropsAttribute)) != null).Select(t => t.Name.ToLower()));

            var customDefaultValsProperties = new List<(string name, object? initVal, object? defaultVal)>();
            if (instance != null && !type.IsEnum)
            {
                customDefaultValsProperties.AddRange(PowerfulGetProperties(type).Select(t => (name: t.Name.ToLower(), initVal: t.GetValue(instance), defaultVal: GetDefault(t.PropertyType))).Where(t => JsonConvert.SerializeObject(t.defaultVal) != JsonConvert.SerializeObject(t.initVal)).ToList() ?? new());
                customDefaultValsProperties.AddRange(PowerfulGetFields(type).Select(t => (name: t.Name.ToLower(), initVal: t.GetValue(instance), defaultVal: GetDefault(t.FieldType))).Where(t => JsonConvert.SerializeObject(t.defaultVal) != JsonConvert.SerializeObject(t.initVal)).ToList() ?? new());
            }

            var schemasToProcess = new[] { schema }.Concat(schema.AllOf ?? new List<JsonSchema>()).Where(x => x != null);
            foreach (var s in schemasToProcess)
            {
                s.Properties?.Keys.Where(x => uniqueItemsProperties.Any(y => y == x.ToLower())).ToList().ForEach(x => s.Properties[x].UniqueItems = true);
                s.Properties?.Keys.Where(x => excludedProperties.Any(y => y == x.ToLower())).ToList().ForEach(x => s.Properties.Remove(x));
                s.Properties?.Keys.Where(x => customDefaultValsProperties.Any(y => y.name == x.ToLower())).ToList().ForEach(x => s.Properties[x].Default = customDefaultValsProperties.First(y => y.name == x.ToLower()).initVal);
                s.Properties?.Keys.Where(x => ignoreInheritProps.Any(y => y == x.ToLower())).ToList().ForEach(x => (s.Properties[x].ExtensionData = (s.Properties[x].ExtensionData ?? new Dictionary<string, object>()))["x-ignore-inherit"] = true);
            }
        }
    }

    public object? GetDefault(Type t) => GetType().GetMethod("GetDefaultGeneric", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.MakeGenericMethod(t).Invoke(this, null);

    private T? GetDefaultGeneric<T>() => default(T);

    public PropertyInfo[] PowerfulGetProperties(Type type)
        => type.GetProperties((BindingFlags)(-1))
               .Concat(type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
               .Distinct()
               .ToArray();

    public FieldInfo[] PowerfulGetFields(Type type)
        => type.GetFields((BindingFlags)(-1))
               .Concat(type.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
               .Distinct()
               .Where(x => !x.Name.Contains("k__BackingField"))
               .ToArray();
}
