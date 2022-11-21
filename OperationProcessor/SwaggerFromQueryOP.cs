using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System.Linq;
using System.Reflection;

public class SwaggerFromQueryOP : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var classFromQueryParams = context.MethodInfo.GetParameters()
                                                        .Where(x => x.GetCustomAttribute<FromQueryAttribute>() != null && x.ParameterType.IsClass).ToList();
        if (!classFromQueryParams.Any()) { return true; }
        var qParams = context.OperationDescription.Operation?.Parameters;
        if (qParams == null || !qParams.Any()) { return true; }

        foreach (var param in classFromQueryParams)
        {
            var classType = param.ParameterType;
            foreach (var qParam in qParams)
            {
                var paramMatchedPropType = param.ParameterType.GetProperties().FirstOrDefault(x => x.Name.ToLower() == qParam.Name?.ToLower());
                if (paramMatchedPropType == null) { continue; }
                var jsonPropertyAtt = paramMatchedPropType.GetCustomAttribute<JsonPropertyAttribute>();
                if (jsonPropertyAtt == null) { continue; }
                qParam.Position = jsonPropertyAtt.Order != 0 ? jsonPropertyAtt.Order : 0;
            }
        }
        var newList = context.OperationDescription.Operation!.Parameters.OrderBy(x => x.Position).ToList();
        context.OperationDescription.Operation.Parameters.Clear();
        newList.ForEach(x => context.OperationDescription.Operation!.Parameters.Add(x));

        return true;
    }
}

