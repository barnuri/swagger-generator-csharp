using System;

/// <summary>
/// give json examples for http requests in swagger
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class SwaggerResponseExampleAttribute : Attribute
{
    public string Name { get; set; }
    public string Json { get; set; }
    public SwaggerResponseExampleAttribute(string name, string json)
    {
        Name = name;
        Json = json;
    }
    public SwaggerResponseExampleAttribute(string name, Type swaggerRequestExampleGetterType)
    {
        var instance = Activator.CreateInstance(swaggerRequestExampleGetterType) as ISwaggerExample;
        Name = name;
        Json = instance!.GetExample();
    }
}

