﻿using ElasticEmail.WebApiClient.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ElasticEmail.generators
{
    public static partial class APIDoc
    {
        public static class CSGenerator
        {
            #region Help methods and variables
            static bool netstandardCompatible; //if true compatible with frameworks: from net45 up to netstandard
            static Dictionary<string, string> paramCLRTypeToCS = new Dictionary<string, string>
            {
                { "String", "string" },
                { "Int32", "int" },
                { "Int64", "long" },
                { "Double", "double" },
                { "Decimal", "decimal" },
                { "Boolean", "bool" },
                { "DateTime", "DateTime" },
                { "Guid", "Guid" },
                { "TextResponse", "string" },
                { "XmlResponse", "string" },
                { "HtmlResponse", "string" },
                { "JavascriptResponse", "string" },
                { "JsonResponse", "string" }
            };

            static string GetCSTypeName(APIDocParser.DataType dataType, string voidName = "void", bool forParam = false)
            {
                // Void
                if (dataType == null || dataType.TypeName == null)
                    return voidName;

                // File
                if (dataType.IsFile)
                {
                    string fileTypeName = "ApiTypes.FileData";
                    if (dataType.IsList) fileTypeName = (forParam ? "IEnumerable" : "List") + "<" + fileTypeName + ">";
                    return fileTypeName;
                }

                // Dictionary
                string typeName = dataType.TypeName;
                if (dataType.IsDictionary)
                {
                    string[] subtypes = typeName.Split(',');
                    for (int i = 0; i < 2; i++)
                    {
                        var tmpName = subtypes[i];
                        bool wasFound = paramCLRTypeToCS.TryGetValue(tmpName, out subtypes[i]);
                        if (!wasFound) subtypes[i] = "ApiTypes." + tmpName;
                    }
                    // subtypes.ForEach((f) => { paramCLRTypeToCS.TryGetValue(f, out f); });
                    typeName = "Dictionary<" + subtypes[0] + ", " + subtypes[1] + ">";
                    return typeName;
                }

                // Normal types check. Else Api custom type.
                if (dataType.IsPrimitive)
                {
                    if (paramCLRTypeToCS.TryGetValue(dataType.TypeName, out typeName) == false)
                    {
                        throw new Exception("Unknown type - " + dataType.TypeName);
                        //typeName = "unknown";
                    }
                }
                else
                    typeName = "ApiTypes." + typeName;

                // List
                if (dataType.IsList) typeName = (forParam ? "IEnumerable" : "List") + "<" + typeName + ">";
                // Array
                else if (dataType.IsArray) typeName += "[]";
                // Nullable
                if (dataType.IsNullable) typeName += "?";

                return typeName ?? "";
            }

            public static string FormatCSDefaultValue(APIDocParser.Parameter param)
            {
                if (param.HasDefaultValue)
                {
                    if (param.DefaultValue == null)
                        return "null";
                    if (!param.Type.IsPrimitive) //enums
                        return "ApiTypes." + param.Type.TypeName + "." + param.DefaultValue;

                    string def = param.DefaultValue;
                    def = def.ToLowerInvariant();
                    if (param.Type.TypeName == "String") def = "\"" + def + "\"";
                    return def;
                }

                return string.Empty;
            }
            #endregion

            #region Code variables      
                  
            #region ApiUtilitiesCodeBelow .net45
            public static string ApiUtilitiesCodeUsingSection= 
                @"using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Collections.Specialized;";
            public static string ApiUtilitiesClassBody =
                @"    
    
    public static class ApiUtilities
    {
        public static byte[] HttpPostFile(string url, List<ApiTypes.FileData> fileData, NameValueCollection parameters)
        {
            try
            {
                string boundary = DateTime.Now.Ticks.ToString(""x"");
                byte[] boundarybytes = Encoding.ASCII.GetBytes(""\r\n--"" + boundary + ""\r\n"");

                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
                wr.ContentType = ""multipart/form-data; boundary="" + boundary;
                wr.Method = ""POST"";
                wr.KeepAlive = true;
                wr.Credentials = CredentialCache.DefaultCredentials;
                wr.Headers.Add(HttpRequestHeader.AcceptEncoding, ""gzip, deflate"");
                wr.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                Stream rs = wr.GetRequestStream();

                string formdataTemplate = ""Content-Disposition: form-data; name=\""{0}\""\r\n\r\n{1}"";
                foreach (string key in parameters.Keys)
                {
                    rs.Write(boundarybytes, 0, boundarybytes.Length);
                    string formitem = string.Format(formdataTemplate, key, parameters[key]);
                    byte[] formitembytes = Encoding.UTF8.GetBytes(formitem);
                    rs.Write(formitembytes, 0, formitembytes.Length);
                }

                if(fileData != null)
                {
                    foreach (var file in fileData)
                    {
                        rs.Write(boundarybytes, 0, boundarybytes.Length);
                        string headerTemplate = ""Content-Disposition: form-data; name=\""filefoobarname\""; filename=\""{0}\""\r\nContent-Type: {1}\r\n\r\n"";
                        string header = string.Format(headerTemplate, file.FileName, file.ContentType);
                        byte[] headerbytes = Encoding.UTF8.GetBytes(header);
                        rs.Write(headerbytes, 0, headerbytes.Length);
                        rs.Write(file.Content, 0, file.Content.Length);
                    }
                }
                byte[] trailer = Encoding.ASCII.GetBytes(""\r\n--"" + boundary + ""--\r\n"");
                rs.Write(trailer, 0, trailer.Length);
                rs.Close();

                using (WebResponse wresp = wr.GetResponse())
                {
                    MemoryStream response = new MemoryStream();
                    wresp.GetResponseStream().CopyTo(response);
                    return response.ToArray();
                }
            }
            catch (WebException webError)
            {
                // Throw exception with actual error message from response
                throw new WebException(((HttpWebResponse)webError.Response).StatusDescription, webError, webError.Status, webError.Response);
            }
        }

        public static byte[] HttpPutFile(string url, ApiTypes.FileData fileData, NameValueCollection parameters)
        {
            try
            {
                string queryString = BuildQueryString(parameters);

                if (queryString.Length > 0) url += ""?"" + queryString.ToString();

                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
                wr.ContentType = fileData.ContentType ?? ""application/octet-stream"";
                wr.Method = ""PUT"";
                wr.KeepAlive = true;
                wr.Credentials = CredentialCache.DefaultCredentials;
                wr.Headers.Add(HttpRequestHeader.AcceptEncoding, ""gzip, deflate"");
                wr.Headers.Add(""Content-Disposition: attachment; filename=\"""" + fileData.FileName + ""\""; size="" + fileData.Content.Length);
                wr.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                Stream rs = wr.GetRequestStream();
                rs.Write(fileData.Content, 0, fileData.Content.Length);

                using (WebResponse wresp = wr.GetResponse())
                {
                    MemoryStream response = new MemoryStream();
                    wresp.GetResponseStream().CopyTo(response);
                    return response.ToArray();
                }
            }
            catch (WebException webError)
            {
                // Throw exception with actual error message from response
                throw new WebException(((HttpWebResponse)webError.Response).StatusDescription, webError, webError.Status, webError.Response);
            }
        }

        public static ApiTypes.FileData HttpGetFile(string url, NameValueCollection parameters)
        {
            try
            {
                string queryString = BuildQueryString(parameters);

                if (queryString.Length > 0) url += ""?"" + queryString.ToString();

                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
                wr.Method = ""GET"";
                wr.KeepAlive = true;
                wr.Credentials = CredentialCache.DefaultCredentials;
                wr.Headers.Add(HttpRequestHeader.AcceptEncoding, ""gzip, deflate"");
                wr.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (WebResponse wresp = wr.GetResponse())
                {
                    MemoryStream response = new MemoryStream();
                    wresp.GetResponseStream().CopyTo(response);
                    if (response.Length == 0) throw new FileNotFoundException();
                    string cds = wresp.Headers[""Content-Disposition""];
                    if (cds == null)
                    {
                        // This is a special case for critical exceptions
                        ApiResponse<string> apiRet = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse<string>>(Encoding.UTF8.GetString(response.ToArray()));
                        if (!apiRet.success) throw new ApplicationException(apiRet.error);
                        return null;
                    }
                    else
                    {
                        ContentDisposition cd = new ContentDisposition(cds);
                        ApiTypes.FileData fileData = new ApiTypes.FileData();
                        fileData.Content = response.ToArray();
                        fileData.ContentType = wresp.ContentType;
                        fileData.FileName = cd.FileName;
                        return fileData;
                    }
                }
            }
            catch (WebException webError)
            {
                // Throw exception with actual error message from response
                throw new WebException(((HttpWebResponse)webError.Response).StatusDescription, webError, webError.Status, webError.Response);
            }
        }

        static string BuildQueryString(NameValueCollection parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return null;

            StringBuilder query = new StringBuilder();
            string amp = string.Empty;
            foreach (string key in parameters.AllKeys)
            {
                foreach (string value in parameters.GetValues(key))
                {
                    query.Append(amp);
                    query.Append(WebUtility.UrlEncode(key));
                    query.Append(""="");
                    query.Append(WebUtility.UrlEncode(value));
                    amp = ""&"";
                }
            }

            return query.ToString();
        }

    }

    public class CustomWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, ""gzip, deflate"");
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return request;
        }
    }
";
            #endregion ApiUtilitiesCodeBelow .net45

            #region ApiUtilitiesCodeNetStandard
            public static string ApiUtilitiesCodeUsingSectionNetStandard =
  @"using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Collections.Specialized;
using System.Threading.Tasks;";
            public static string ApiUtilitiesClassBodyNetStandard =
                @"    
    
    public static class ApiUtilities
    {
        public static async Task<ApiResponse<T>> PostAsync<T>(string requestAddress, Dictionary<string, string> values)
        {
            using (HttpClient client = new HttpClient())
            {
                using (var postContent = new FormUrlEncodedContent(values))
                {
                    using (HttpResponseMessage response = await client.PostAsync(Api.ApiUri + requestAddress, postContent))
                    {
                        await response.EnsureSuccessStatusCodeAsync();
                        using (HttpContent content = response.Content)
                        {
                            string result = await content.ReadAsStringAsync();
                            ApiResponse<T> apiResponse = ApiResponseValidator.Validate<T>(result);
                            return apiResponse;
                        }
                    }
                }
            }
        }
  
        public static async Task<byte[]> HttpPostFileAsync(string url, List<ApiTypes.FileData> fileData, Dictionary<string, string> parameters)
        {
            try
            {

                string boundary = DateTime.Now.Ticks.ToString(""x"");
                byte[] boundarybytes = Encoding.ASCII.GetBytes(""\r\n--"" + boundary + ""\r\n"");

                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
                wr.ContentType = ""multipart/form-data; boundary="" + boundary;
                wr.Method = ""POST"";
                wr.Credentials = CredentialCache.DefaultCredentials;

                Stream rs = await wr.GetRequestStreamAsync();

                string formdataTemplate = ""Content-Disposition: form-data; name=\""{0}\""\r\n\r\n{1}"";
                foreach (string key in parameters.Keys)
                {
                    rs.Write(boundarybytes, 0, boundarybytes.Length);
                    string formitem = string.Format(formdataTemplate, key, parameters[key]);
                    byte[] formitembytes = Encoding.UTF8.GetBytes(formitem);
                    rs.Write(formitembytes, 0, formitembytes.Length);
                }

                if (fileData != null)
                {
                    foreach (var file in fileData)
                    {
                        rs.Write(boundarybytes, 0, boundarybytes.Length);
                        string headerTemplate = ""Content-Disposition: form-data; name=\""filefoobarname\""; filename=\""{0}\""\r\nContent-Type: {1}\r\n\r\n"";
                        string header = string.Format(headerTemplate, file.FileName, file.ContentType);
                        byte[] headerbytes = Encoding.UTF8.GetBytes(header);
                        rs.Write(headerbytes, 0, headerbytes.Length);
                        rs.Write(file.Content, 0, file.Content.Length);
                    }
                }

                byte[] trailer = Encoding.ASCII.GetBytes(""\r\n--"" + boundary + ""--\r\n"");
                rs.Write(trailer, 0, trailer.Length);


                using (WebResponse wresp = await wr.GetResponseAsync())
                {
                    MemoryStream response = new MemoryStream();
                    wresp.GetResponseStream().CopyTo(response);
                            return response.ToArray();
                }
            }
            catch (WebException webError)
            {
                // Throw exception with actual error message from response
                throw new WebException(((HttpWebResponse)webError.Response).StatusDescription, webError, webError.Status, webError.Response);
            }
        }

        public static async Task<ApiTypes.FileData> HttpGetFileAsync(string url, Dictionary<string, string> parameters)
        {
            try
            {
                url = Api.ApiUri + url;
                string queryString = BuildQueryString(parameters);

                if (queryString.Length > 0) url += ""?"" + queryString.ToString();

                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
                wr.Method = ""GET"";
                wr.Credentials = CredentialCache.DefaultCredentials;

                using (WebResponse wresp = await wr.GetResponseAsync())
                {
                    MemoryStream response = new MemoryStream();
                    wresp.GetResponseStream().CopyTo(response);
                    if (response.Length == 0) throw new FileNotFoundException();
                    string cds = wresp.Headers[""Content-Disposition""];
                    if (cds == null)
                    {
                        // This is a special case for critical exceptions
                        ApiResponse<string> apiRet = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse<string>>(Encoding.UTF8.GetString(response.ToArray()));
                        if (!apiRet.success) throw new Exception(apiRet.error);
                        return null;
                    }
                    else
                    {
                        ApiTypes.FileData fileData = new ApiTypes.FileData();
                        fileData.Content = response.ToArray();
                        fileData.ContentType = wresp.ContentType;
                        fileData.FileName = GetFileNameFromContentDisposition(cds);
                        return fileData;
                    }
                }
            }
            catch (WebException webError)
            {
                // Throw exception with actual error message from response
                throw new WebException(((HttpWebResponse)webError.Response).StatusDescription, webError, webError.Status, webError.Response);
            }
        }

        public static async Task<byte[]> HttpPutFileAsync(string url, ApiTypes.FileData fileData, Dictionary<string, string> parameters)
        {
            try
            {
                string queryString = BuildQueryString(parameters);

                if (queryString.Length > 0) url += ""?"" + queryString.ToString();

                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
                wr.ContentType = fileData.ContentType ?? ""application/octet-stream"";
                wr.Method = ""PUT"";
                
                wr.Credentials = CredentialCache.DefaultCredentials;
                wr.Headers[HttpRequestHeader.AcceptEncoding] = ""gzip, deflate"";
                wr.Headers[""Content-Disposition""] = ""attachment; filename=\"""" + fileData.FileName + ""\""; size="" + fileData.Content.Length;

                Stream rs = await wr.GetRequestStreamAsync();
                rs.Write(fileData.Content, 0, fileData.Content.Length);

                using (WebResponse wresp = await wr.GetResponseAsync())
                {
                    MemoryStream response = new MemoryStream();
                    wresp.GetResponseStream().CopyTo(response);
                    return response.ToArray();
                }
            }
            catch (WebException webError)
            {
                // Throw exception with actual error message from response
                throw new WebException(((HttpWebResponse)webError.Response).StatusDescription, webError, webError.Status, webError.Response);
            }
        }

        static string BuildQueryString(Dictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return null;

            StringBuilder query = new StringBuilder();
            string amp = string.Empty;
            foreach (KeyValuePair<string, string> kvp in parameters)
            {
                query.Append(amp);
                query.Append(WebUtility.UrlEncode(kvp.Key));
                query.Append("" = "");
                query.Append(WebUtility.UrlEncode(kvp.Value));
                amp = ""&"";
            }

            return query.ToString();
        }

        static string GetFileNameFromContentDisposition(string contentDisposition)
        {
            string[] chunks = contentDisposition.Split(';');
            string searchPhrase = ""filename="";
            foreach (string chunk in chunks)
            {
                int index = contentDisposition.IndexOf(searchPhrase);
                if (index > 0)
                {
                    return contentDisposition.Substring(index + searchPhrase.Length);
                }
            }
            return """";
        }
    }

    public static class HttpResponseMessageExtensions
    {
        public static async Task EnsureSuccessStatusCodeAsync(this HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var content = await response.Content.ReadAsStringAsync();

            if (response.Content != null)
                response.Content.Dispose();
            throw new SimpleHttpResponseException(response.StatusCode, content);
        }
    }

    public class SimpleHttpResponseException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }

        public SimpleHttpResponseException(HttpStatusCode statusCode, string content) : base(content)
        {
            StatusCode = statusCode;
        }
    }

    public class ApiResponseValidator
    {
        public static ApiResponse<T> Validate<T>(string apiResponse)
        {
            ApiResponse<T> apiRet = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse<T>>(apiResponse);
            if (!apiRet.success)
            {
                throw new Exception(apiRet.error);
            }
            return apiRet;
        }
    }
";
            #endregion ApiUtilitiesCodeNetStandard

            public static string ApiUtilitiesCodeBlock1 =
    @"


namespace ElasticEmail.WebApiClient
{
    #region Utilities
    public class ApiResponse<T>
    {
        public bool success = false;
        public string error = null;
        public T Data
        {
            get;
            set;
        }
    }

    public class VoidApiResponse
    {
    }";
public static string ApiUtilitiesCodeBlock2 =
 @"
    #endregion

    public static class Api
    {
        public static string ApiKey = ""00000000-0000-0000-0000-000000000000"";
        public static string ApiUri = ""https://api.elasticemail.com/v2"";

";
            public static string FileDataCode = @"    /// <summary>
    /// File response from the server
    /// </summary>
    public class FileData
    {
        /// <summary>
        /// File content
        /// </summary>
        public byte[] Content { get; set; }

        /// <summary>
        /// MIME content type, optional for uploads
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Name of the file this class contains
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Saves this file to given destination
        /// </summary>
        /// <param name=""path"">Path string exluding file name</param>
        public void SaveToDirectory(string path)
        {
            File.WriteAllBytes(Path.Combine(path, FileName), Content);
        }

        /// <summary>
        /// Saves this file to given destination
        /// </summary>
        /// <param name=""pathWithFileName"">Path string including file name</param>
        public void SaveTo(string pathWithFileName)
        {
            File.WriteAllBytes(pathWithFileName, Content);
        }

        /// <summary>
        /// Reads a file to this class instance
        /// </summary>
        /// <param name=""pathWithFileName"">Path string including file name</param>
        public void ReadFrom(string pathWithFileName)
        {
            Content = File.ReadAllBytes(pathWithFileName);
            FileName = Path.GetFileName(pathWithFileName);
            ContentType = null;
        }

        /// <summary>
        /// Creates a new FileData instance from a file
        /// </summary>
        /// <param name=""pathWithFileName"">Path string including file name</param>
        /// <returns></returns>
        public static FileData CreateFromFile(string pathWithFileName)
        {
            FileData fileData = new FileData();
            fileData.ReadFrom(pathWithFileName);
            return fileData;
        }
    }

";
            #endregion

            #region Generating methods

            public static string Generate(APIDocParser.Project project, bool netStandardCompatible)
            {
                var cs = new StringBuilder();
                netstandardCompatible = netStandardCompatible;
                cs.AppendLine("/*");
                cs.AppendLine(ApiLicenseSupplier.ApiLicense);
                cs.AppendLine("*/");
                cs.AppendLine();

                if (netstandardCompatible)
                {
                    cs.Append(ApiUtilitiesCodeUsingSectionNetStandard);
                    cs.Append(ApiUtilitiesCodeBlock1);
                    cs.Append(ApiUtilitiesClassBodyNetStandard);
                    cs.Append(ApiUtilitiesCodeBlock2);
                }
                else
                {
                    cs.Append(ApiUtilitiesCodeUsingSection);
                    cs.Append(ApiUtilitiesCodeBlock1);
                    cs.Append(ApiUtilitiesClassBody);
                    cs.Append(ApiUtilitiesCodeBlock2);
                }

                foreach (var cat in project.Categories.OrderBy(f => f.Value.Name))
                    cs.Append(GenerateCategoryCode(cat));

                cs.AppendLine($@"
    }}
    #region Api Types
    public static class ApiTypes
    {{
    {FileDataCode}
    #pragma warning disable 0649");

                foreach (var cls in project.Classes.OrderBy(f => f.Name))
                    cs.Append(GenerateClassCode(cls));

                cs.AppendLine($@"
    #pragma warning restore 0649
    #endregion
    }}
    }}");

                return cs.ToString();
            }

            public static string GenerateCategoryCode(KeyValuePair<string, APIDocParser.Category> cat)
            {
                StringBuilder cs = new StringBuilder();

                cs.AppendLine($@"
        #region {cat.Value.Name} functions
        /// <summary>
        /// {cat.Value.Summary}
        /// </summary>
        public static class {cat.Value.Name}
        {{");

                foreach (var func in cat.Value.Functions.OrderBy(f => f.Name))
                    cs.Append(GenerateFunctionCode(func, cat));

                cs.AppendLine("        }");
                cs.AppendLine("        #endregion");
                cs.AppendLine();

                return cs.ToString();
            }

            public static string GenerateFunctionCode(APIDocParser.Function func, KeyValuePair<string, APIDocParser.Category> cat)
            {
                StringBuilder cs = new StringBuilder();

                cs.AppendLine("            /// <summary>");
                cs.AppendLine("            /// " + func.Summary);
                cs.AppendLine("            /// </summary>");
                cs.AppendLine(string.Join("\r\n", func.Parameters.Select(f => "            /// <param name=\"" + f.Name + "\">" + f.Description + "</param>")));
                if (func.ReturnType.TypeName != null) cs.AppendLine("            /// <returns>" + GetCSTypeName(func.ReturnType).Replace("<", "(").Replace(">", ")") + "</returns>");
                if (netstandardCompatible)
                {
                    if (GetCSTypeName(func.ReturnType) == "void")
                    {
                        cs.Append("            public static async Task " + func.Name + "Async(");
                    }
                    else
                    {
                        cs.Append("            public static async Task<" + GetCSTypeName(func.ReturnType) + "> " + func.Name + "Async(");
                    }
                }
                else
                {
                    cs.Append("            public static " + GetCSTypeName(func.ReturnType) + " " + func.Name + "(");
                }
                bool addComma = false;
                foreach (var param in func.Parameters)
                {
                    if (param.Name == "apikey")
                        continue;

                    if (addComma) cs.Append(", ");
                    cs.Append(GetCSTypeName(param.Type, forParam: true) + " " + param.Name);
                    if (param.HasDefaultValue)
                        cs.Append(" = " + FormatCSDefaultValue(param));
                    addComma = true;
                }
                cs.AppendLine(")");
                cs.AppendLine("            {");
                if (!func.Parameters.Any(f => f.IsFilePostUpload | f.IsFilePutUpload) && !func.ReturnType.IsFile)
                {
                    if (!netstandardCompatible)
                    {
                        cs.AppendLine("                WebClient client = new CustomWebClient();");
                    }
                }
                if (netstandardCompatible)
                {
                    cs.Append(
@"                Dictionary<string, string> values = new Dictionary<string, string>();
                values.Add(""apikey"", Api.ApiKey);
");
                }
                else
                {
                    cs.Append(
    @"                NameValueCollection values = new NameValueCollection();
                values.Add(""apikey"", Api.ApiKey);
");
                }

                foreach (var param in func.Parameters)
                    cs.Append(AppendParamToNVC(param));
                bool uploadMethosIsPostAsync;
                cs.Append(ChooseUploadMethod(func, cat, out uploadMethosIsPostAsync));
                if (!func.ReturnType.IsFile)
                {
                    if (netstandardCompatible)
                    {
                        if (uploadMethosIsPostAsync)
                        {
                            if (func.ReturnType.TypeName != null)
                            {
                                cs.AppendLine("                return apiResponse.Data;");
                            }
                        }
                        else
                        {
                            cs.AppendLine("                ApiResponse<" + GetCSTypeName(func.ReturnType, "VoidApiResponse") + "> apiRet = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse<" + GetCSTypeName(func.ReturnType, "VoidApiResponse") + ">>(Encoding.UTF8.GetString(apiResponse));");
                            cs.AppendLine("                if (!apiRet.success) throw new Exception(apiRet.error);");

                            if (func.ReturnType.TypeName != null)
                            {
                                cs.AppendLine("                return apiRet.Data;");
                            }
                        }
                    }
                    else
                    {
                        cs.AppendLine("                ApiResponse<" + GetCSTypeName(func.ReturnType, "VoidApiResponse") + "> apiRet = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse<" + GetCSTypeName(func.ReturnType, "VoidApiResponse") + ">>(Encoding.UTF8.GetString(apiResponse));");
                        cs.AppendLine("                if (!apiRet.success) throw new ApplicationException(apiRet.error);");

                        if (func.ReturnType.TypeName != null)
                        {
                            cs.AppendLine("                return apiRet.Data;");
                        }
                    }
                }

                cs.AppendLine("            }");
                cs.AppendLine();

                return cs.ToString();
            }

            public static string AppendParamToNVC(APIDocParser.Parameter param)
            {
                StringBuilder cs = new StringBuilder();

                if (param.Name == "apikey" || param.IsFilePostUpload || param.IsFilePutUpload)
                    return string.Empty;
                string cspar = param.Name;
                cspar += (param.Type.TypeName == "DateTime" && param.Type.IsNullable) ? ".Value" : string.Empty;

                if (param.Type.IsPrimitive == false && param.Type.IsEnum == false)
                    cspar = "Newtonsoft.Json.JsonConvert.SerializeObject(" + cspar + ")";
                else if (param.Type.TypeName != "String" && param.Type.IsList == false && param.Type.IsArray == false)
                    cspar += ".ToString(" + (param.Type.TypeName == "DateTime" ? "\"M/d/yyyy h:mm:ss tt\"" : string.Empty) + ")";

                if (param.Type.IsArray || param.Type.IsDictionary)
                {
                    if (param.HasDefaultValue)
                    {
                        cs.AppendLine("                if (" + param.Name + " != " + FormatCSDefaultValue(param) + ")");
                        cs.AppendLine("                {");
                    }
                    if (param.Type.IsDictionary)
                    {
                        cs.AppendLine((param.HasDefaultValue ? "    " : "") + "                foreach (" + GetCSTypeName(param.Type).Replace("Dictionary", "KeyValuePair") + " _item in " + param.Name + ")");
                        cs.AppendLine((param.HasDefaultValue ? "    " : "") + "                {");
                        cs.AppendLine((param.HasDefaultValue ? "    " : "") + "                    values.Add(\"" + param.Name + "_\" + _item.Key, _item.Value);");
                        cs.AppendLine((param.HasDefaultValue ? "    " : "") + "                }");
                    }
                    else
                    {
                        cs.AppendLine((param.HasDefaultValue ? "    " : "") + "                foreach (" + GetCSTypeName(param.Type).Replace("[]", string.Empty) + " _item in " + param.Name + ")");
                        cs.AppendLine((param.HasDefaultValue ? "    " : "") + "                {");
                        cs.AppendLine((param.HasDefaultValue ? "    " : "") + "                    values.Add(\"" + param.Name + "\", _item" + (param.Type.TypeName == "String" ? ".ToString()" : "") + ");");
                        cs.AppendLine((param.HasDefaultValue ? "    " : "") + "                }");
                    }
                    if (param.HasDefaultValue)
                    {
                        cs.AppendLine("                }");
                    }
                }
                else
                {
                    cs.AppendLine("                " + (param.HasDefaultValue ? "if (" + param.Name + " != " + FormatCSDefaultValue(param) + ") " : "") + "values.Add(\"" + param.Name + "\", " + (param.Type.IsList && (param.Type.IsPrimitive || param.Type.IsEnum) ? "string.Join(\",\", " + cspar + ")" : cspar) + ");");
                }

                return cs.ToString();
            }

            public static string ChooseUploadMethod(APIDocParser.Function func, KeyValuePair<string, APIDocParser.Category> cat, out bool uploadMethosIsPostAsync)
            {
                StringBuilder cs = new StringBuilder();
                uploadMethosIsPostAsync = false;
                if (func.Parameters.Any(f => f.IsFilePostUpload == true))
                {
                    var subParam = func.Parameters.First(f => f.IsFilePostUpload);
                    string filesLineToAppend = null; // subParam.Name;
                    if (subParam.HasDefaultValue && FormatCSDefaultValue(subParam) == "null")
                        filesLineToAppend = subParam.Name + " == null ? null : ";
                    if (!func.Parameters.Any(f => f.IsFilePostUpload == true && f.Type.IsList == true))
                        filesLineToAppend += "new List<ApiTypes.FileData>() { " + subParam.Name + " }";
                    else
                        filesLineToAppend += subParam.Name + ".ToList()";
                    if (netstandardCompatible)
                    {
                        cs.AppendLine("                byte[] apiResponse = await ApiUtilities.HttpPostFileAsync(\"/" + cat.Value.UriPath.ToLower() + "/" + func.Name.ToLower() + "\", " + filesLineToAppend + ", values);");
                    }
                    else
                    {
                        cs.AppendLine("                byte[] apiResponse = ApiUtilities.HttpPostFile(Api.ApiUri + \"/" + cat.Value.UriPath.ToLower() + "/" + func.Name.ToLower() + "\", " + filesLineToAppend + ", values);");
                    }
                }
                else if (func.Parameters.Any(f => f.IsFilePutUpload == true))
                {
                    if (netstandardCompatible)
                    {
                        cs.AppendLine("                byte[] apiResponse = await ApiUtilities.HttpPutFileAsync(\"/" + cat.Value.UriPath.ToLower() + "/" + func.Name.ToLower() + "\", " + func.Parameters.First(f => f.IsFilePutUpload).Name + ", values);");
                    }
                    else
                    {
                        cs.AppendLine("                byte[] apiResponse = ApiUtilities.HttpPutFile(Api.ApiUri + \"/" + cat.Value.UriPath.ToLower() + "/" + func.Name.ToLower() + "\", " + func.Parameters.First(f => f.IsFilePutUpload).Name + ", values);");
                    }
                }
                else if (func.ReturnType.IsFile)
                {
                    if (netstandardCompatible)
                    {
                        cs.AppendLine("                return await ApiUtilities.HttpGetFileAsync(\"/" + cat.Value.UriPath.ToLower() + "/" + func.Name.ToLower() + "\", values);");
                    }
                    else
                    {
                        cs.AppendLine("                return ApiUtilities.HttpGetFile(Api.ApiUri + \"/" + cat.Value.UriPath.ToLower() + "/" + func.Name.ToLower() + "\", values);");
                    }
                }
                else
                {
                    if (netstandardCompatible)
                    {
                        uploadMethosIsPostAsync = true;
                        cs.AppendLine("                ApiResponse<" + GetCSTypeName(func.ReturnType, "VoidApiResponse") + "> apiResponse = await ApiUtilities.PostAsync<" + GetCSTypeName(func.ReturnType, "VoidApiResponse") + ">(\"/" + cat.Value.UriPath.ToLower() + "/" + func.Name.ToLower() + "\", values);");
                    }
                    else
                    {
                        cs.AppendLine("                byte[] apiResponse = client.UploadValues(Api.ApiUri + \"/" + cat.Value.UriPath.ToLower() + "/" + func.Name.ToLower() + "\", values);");
                    }
                }

                return cs.ToString();
            }

            public static string GenerateClassCode(APIDocParser.Class cls)
            {
                StringBuilder cs = new StringBuilder();

                cs.AppendLine("    /// <summary>");
                cs.AppendLine("    /// " + cls.Summary);
                cs.AppendLine("    /// </summary>");
                cs.AppendLine("    public " + (cls.IsEnum ? "enum " : "class ") + cls.Name);
                cs.AppendLine("    {");
                foreach (var fld in cls.Fields)
                {
                    cs.AppendLine("        /// <summary>");
                    cs.AppendLine("        /// " + fld.Description);
                    cs.AppendLine("        /// </summary>");
                    if (cls.IsEnum)
                        cs.AppendLine("        " + fld.Name + " = " + ((APIDocParser.EnumField)fld).Value + ",");
                    else
                        cs.AppendLine("        public " + GetCSTypeName(fld.Type) + " " + fld.Name + " {get; set;}");
                    cs.AppendLine();
                }
                cs.AppendLine("    }");
                cs.AppendLine();

                return cs.ToString();
            }

            #endregion
        }
    }
}