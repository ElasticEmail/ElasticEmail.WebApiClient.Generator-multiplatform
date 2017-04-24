using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElasticEmail
{
    public static partial class APIDocParser
    {
        public class Project
        {
            public string Version { get; set; }
            public SortedDictionary<string, Category> Categories = new SortedDictionary<string, Category>();
            public List<Class> Classes = new List<Class>();

            public Class GetClass(string name)
            {
                Class cls = Classes.FirstOrDefault(f => f.Name == name);
                if (cls == null)
                {
                    cls = new Class();
                    cls.Name = name;
                    Classes.Add(cls);
                }
                return cls;
            }

            public Category GetCategory(string name)
            {
                Category cat;
                Categories.TryGetValue(name, out cat);
                if (cat == null)
                {
                    cat = new Category();
                    cat.Name = name;
                    Categories.Add(cat.Name, cat);
                }
                return cat;
            }
        }
    }
}