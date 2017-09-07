using System;


namespace ElasticEmail
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ApiCategoryAttribute : Attribute
    {
        public string Category { get; private set; }
        public string UriPath { get; private set; }

        public ApiCategoryAttribute(string category, string uriPath)
        {
            Category = category;
            UriPath = uriPath;
        }
    }
}