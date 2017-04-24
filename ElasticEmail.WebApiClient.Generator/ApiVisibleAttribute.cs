using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EEClientGenerator
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ApiVisibleAttribute : Attribute
    {
    }
}
