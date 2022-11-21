using System;

/// <summary>
/// give json examples for http requests in swagger
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class SwaggerRequestExampleAttribute : Attribute
{
    public string Name { get; set; }
    public string Json { get; set; }
    public SwaggerRequestExampleAttribute(string name, string json)
    {
        Name = name;
        Json = json;
    }
    public SwaggerRequestExampleAttribute(string name, Type swaggerRequestExampleGetterType)
    {
        var instance = Activator.CreateInstance(swaggerRequestExampleGetterType) as ISwaggerExample;
        Name = name;
        Json = instance!.GetExample();
    }
}

