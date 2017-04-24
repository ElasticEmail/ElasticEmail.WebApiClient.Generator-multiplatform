using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElasticEmail
{
    public static partial class APIDocParser
    {
        public class Class
        {
            public string Name { get; set; }
            public string Summary { get; set; }
            public List<EnumField> Fields = new List<EnumField>();
            public bool IsEnum { get; set; }
        }
    }
}