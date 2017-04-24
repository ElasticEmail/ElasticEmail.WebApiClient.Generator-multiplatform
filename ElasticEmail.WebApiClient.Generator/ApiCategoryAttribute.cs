using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EEClientGenerator
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
