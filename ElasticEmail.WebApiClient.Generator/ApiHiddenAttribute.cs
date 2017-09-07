using System;


namespace ElasticEmail
{
    // By default all parameters and fields are visible to the public API (although hiding them doesn't make them inaccessible)
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ApiHiddenAttribute : Attribute
    {
    }
}