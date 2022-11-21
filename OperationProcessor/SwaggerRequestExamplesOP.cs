using Newtonsoft.Json;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class SwaggerRequestExamplesOP : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var exampleAtts = context.MethodInfo.GetCustomAttributes<SwaggerRequestExampleAttribute>();
        if (!exampleAtts.Any()) { return true; }
        var examples = new Dictionary<string, OpenApiExample>();
        foreach (var exampleAtt in exampleAtts)
        {
            examples[exampleAtt.Name] = new OpenApiExample { Value = JsonConvert.DeserializeObject<dynamic>(exampleAtt.Json) };
        }
        if (context.OperationDescription.Operation?.RequestBody?.Content == null) { return true; }
        var reqContent = context.OperationDescription.Operation.RequestBody.Content;
        foreach (var key in reqContent.Keys)
        {
            var content = reqContent[key];
            foreach (var exKey in examples.Keys)
            {
                content.Examples[exKey] = examples[exKey];
            }
        }
        return true;
    }
}