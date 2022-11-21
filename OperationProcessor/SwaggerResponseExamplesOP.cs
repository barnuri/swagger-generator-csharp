using Newtonsoft.Json;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System.Linq;
using System.Reflection;

public class SwaggerResponseExamplesOP : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var exampleAtts = context.MethodInfo.GetCustomAttributes<SwaggerResponseExampleAttribute>();
        if (!exampleAtts.Any()) { return true; }
        var example = JsonConvert.DeserializeObject(exampleAtts.First().Json);
        var responses = context.OperationDescription.Operation.Responses;
        foreach (var response in responses.Values)
        {
            response.Examples = example;
        }
        return true;
    }
}