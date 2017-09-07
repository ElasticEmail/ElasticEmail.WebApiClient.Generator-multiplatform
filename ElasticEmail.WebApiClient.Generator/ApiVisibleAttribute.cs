using System;


namespace ElasticEmail
{
    // By default all classes, enums and methods are not visible to the public API (although they are accessible if are marked public)
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ApiVisibleAttribute : Attribute
    {
    }
}