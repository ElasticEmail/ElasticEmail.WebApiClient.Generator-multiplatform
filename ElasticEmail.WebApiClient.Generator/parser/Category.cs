using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElasticEmail
{
    public static partial class APIDocParser
    {
        public class Category
        {
            private string name;
            public string Name
            {
                get
                {
                    if (name == "Export")
                        return "Eksport";
                    else return name;
                    
                }
                set
                {
                    name = value;
                }
            }
            public string NameLocal
            {
                get
                {
                    return name;
                }
                set
                {
                    name = value;
                }
            }
            public string UriPath { get; set; }
            public string Summary { get; set; }
            public List<Function> Functions = new List<Function>();
        }
    }
}