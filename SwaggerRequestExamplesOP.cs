using Newtonsoft.Json;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class SwaggerRequestExamplesOP : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var exampleAtt = context.MethodInfo.GetCustomAttribute<SwaggerRequestExamples>();
        if (exampleAtt == null || (exampleAtt.JsonExamples ?? new string[] { }).Count() <= 0) { return true; }
        var examples = new Dictionary<string, OpenApiExample>();
        foreach (var ex in (exampleAtt.JsonExamples ?? new string[] { }))
        {
            examples[$"example - {examples.Keys.Count + 1}"] = new OpenApiExample { Value = JsonConvert.DeserializeObject(ex) };
        }
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

[AttributeUsage(AttributeTargets.Method)]
public class SwaggerRequestExamples : Attribute
{
    public string[] JsonExamples { get; }

    public SwaggerRequestExamples(params string[] jsonExamples)
    {
        JsonExamples = jsonExamples;
    }
}