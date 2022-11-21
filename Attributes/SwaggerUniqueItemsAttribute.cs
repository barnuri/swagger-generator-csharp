using System;

/// <summary>
/// allow only unique items in list
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SwaggerUniqueItemsAttribute : Attribute
{
}