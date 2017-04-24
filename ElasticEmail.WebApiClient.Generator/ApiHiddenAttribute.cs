using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EEClientGenerator
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ApiHiddenAttribute : Attribute
    {
    }
}
