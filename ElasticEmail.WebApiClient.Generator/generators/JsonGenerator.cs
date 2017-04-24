using ElasticEmail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EEClientGenerator.generators
{
    public static partial class APIDoc
    {
        public static class JsonGenerator
        {
            public static string Generate(APIDocParser.Project project)
            {
                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects
                };
                return Newtonsoft.Json.JsonConvert.SerializeObject(project, settings);
            }
        }
    }
}
