using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ElasticEmail.WebApiClient.Generator
{
    public static class StringUtitilities
    {
        public static string LimitLength(this string source, int maxLength)
        {
            if (source == null || source.Length <= maxLength)
                return source;

            return source.Substring(0, maxLength);
        }
        /// <summary>
        /// String dictionary is used because it forces the key to be lower case
        /// </summary>
        public static string MultipleReplaceIgnoreCase(this string input, StringDictionary replacements)
        {
            List<string> keys = new List<string>();
            foreach (string key in replacements.Keys)
            {
                keys.Add(Regex.Escape(key));
            }
            var regex = new Regex(String.Join("|", keys.ToArray()), RegexOptions.IgnoreCase);
            return regex.Replace(input, m => replacements[m.Value]);
        }
    }
}
