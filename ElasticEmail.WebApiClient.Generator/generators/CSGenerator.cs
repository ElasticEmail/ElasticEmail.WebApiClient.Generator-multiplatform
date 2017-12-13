using ElasticEmail.WebApiClient.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ElasticEmail
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
                    string subOut1 = string.Empty;
                    string subOut2 = string.Empty;
                    string subType1 = GetCSTypeName(dataType.SubTypes[0]);
                    string subType2 = GetCSTypeName(dataType.SubTypes[1]);
                    if (paramCLRTypeToCS.TryGetValue(subType1, out subOut1) == false) subOut1 = subType1;
                    if (paramCLRTypeToCS.TryGetValue(subType2, out subOut2) == false) subOut2 = subType2;
                    typeName = "Dictionary<" + subOut1 + ", " + subOut2 + ">";
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
                    def = def?.ToLowerInvariant();
                    if (param.Type.TypeName.Equals("String", StringComparison.OrdinalIgnoreCase)) def = "\"" + def + "\"";
                    return def;
                }

                return string.Empty;
            }
            #endregion

            #region Code variables      

            #region ApiUtilitiesCodeBelow .net45
            public static string ApiUtilitiesCodeUsingSection =
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
                url = Api.ApiUri + url;
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
                url = Api.ApiUri + url;
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

            #region MimeHelper
            public static string MimeHelper =
    @"
   #region MIME HELPER
    internal class MimeMapping
    {
        private static Dictionary<string, string> mimeMappings = null;
        private static object locker = new object();
        private static readonly char[] _pathSeparatorChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.VolumeSeparatorChar }; // from Path.GetFileName()

        public static string GetMimeMapping(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(""fileName"");
            }
            EnsureMapping();
        fileName = GetFileName(fileName); // strip off path separators

            // some MIME types have complex extensions (like "".exe.config""), so we need to work left-to-right
            for (int i = 0; i<fileName.Length; i++)
            {
                if (fileName[i] == '.')
                {
                    // potential extension - consult dictionary
                    string mimeType;
                    if (mimeMappings.TryGetValue(fileName.Substring(i), out mimeType))
                    {
                        // found!
                        return mimeType;
                    }
}
            }

            // If we reached this point, either we couldn't find an extension, or the extension we found
            // wasn't recognized. In either case, the "".*"" mapping is guaranteed to exist as a fallback.
            return mimeMappings["".*""];
        }
        private static string GetFileName(string path)
{
    int pathSeparatorIndex = path.LastIndexOfAny(_pathSeparatorChars);
    return (pathSeparatorIndex >= 0) ? path.Substring(pathSeparatorIndex) : path;
}
private static void EnsureMapping()
{
    // Ensure initialized only once
    if (mimeMappings == null)
    {
        lock (locker)
        {
            if (mimeMappings == null)
            {
                PopulateMappings();
            }
        }
    }
}
private static void PopulateMappings()
{
    mimeMappings = new Dictionary<string, string>()
            {
            { "".*"", ""application/octet-stream"" },
            {"".323"", ""text/h323""},
            {"".aaf"", ""application/octet-stream""},
            {"".aca"", ""application/octet-stream""},
            {"".accdb"", ""application/msaccess""},
            {"".accde"", ""application/msaccess""},
            {"".accdt"", ""application/msaccess""},
            {"".acx"", ""application/internet-property-stream""},
            {"".afm"", ""application/octet-stream""},
            {"".ai"", ""application/postscript""},
            {"".aif"", ""audio/x-aiff""},
            {"".aifc"", ""audio/aiff""},
            {"".aiff"", ""audio/aiff""},
            {"".application"", ""application/x-ms-application""},
            {"".art"", ""image/x-jg""},
            {"".asd"", ""application/octet-stream""},
            {"".asf"", ""video/x-ms-asf""},
            {"".asi"", ""application/octet-stream""},
            {"".asm"", ""text/plain""},
            {"".asr"", ""video/x-ms-asf""},
            {"".asx"", ""video/x-ms-asf""},
            {"".atom"", ""application/atom+xml""},
            {"".au"", ""audio/basic""},
            {"".avi"", ""video/x-msvideo""},
            {"".axs"", ""application/olescript""},
            {"".bas"", ""text/plain""},
            {"".bcpio"", ""application/x-bcpio""},
            {"".bin"", ""application/octet-stream""},
            {"".bmp"", ""image/bmp""},
            {"".c"", ""text/plain""},
            {"".cab"", ""application/octet-stream""},
            {"".calx"", ""application/vnd.ms-office.calx""},
            {"".cat"", ""application/vnd.ms-pki.seccat""},
            {"".cdf"", ""application/x-cdf""},
            {"".chm"", ""application/octet-stream""},
            {"".class"", ""application/x-java-applet""},
            {"".clp"", ""application/x-msclip""},
            {"".cmx"", ""image/x-cmx""},
            {"".cnf"", ""text/plain""},
            {"".cod"", ""image/cis-cod""},
            {"".cpio"", ""application/x-cpio""},
            {"".cpp"", ""text/plain""},
            {"".crd"", ""application/x-mscardfile""},
            {"".crl"", ""application/pkix-crl""},
            {"".crt"", ""application/x-x509-ca-cert""},
            {"".csh"", ""application/x-csh""},
            {"".css"", ""text/css""},
            {"".csv"", ""application/octet-stream""},
            {"".cur"", ""application/octet-stream""},
            {"".dcr"", ""application/x-director""},
            {"".deploy"", ""application/octet-stream""},
            {"".der"", ""application/x-x509-ca-cert""},
            {"".dib"", ""image/bmp""},
            {"".dir"", ""application/x-director""},
            {"".disco"", ""text/xml""},
            {"".dll"", ""application/x-msdownload""},
            {"".dll.config"", ""text/xml""},
            {"".dlm"", ""text/dlm""},
            {"".doc"", ""application/msword""},
            {"".docm"", ""application/vnd.ms-word.document.macroEnabled.12""},
            {"".docx"", ""application/vnd.openxmlformats-officedocument.wordprocessingml.document""},
            {"".dot"", ""application/msword""},
            {"".dotm"", ""application/vnd.ms-word.template.macroEnabled.12""},
            {"".dotx"", ""application/vnd.openxmlformats-officedocument.wordprocessingml.template""},
            {"".dsp"", ""application/octet-stream""},
            {"".dtd"", ""text/xml""},
            {"".dvi"", ""application/x-dvi""},
            {"".dwf"", ""drawing/x-dwf""},
            {"".dwp"", ""application/octet-stream""},
            {"".dxr"", ""application/x-director""},
            {"".eml"", ""message/rfc822""},
            {"".emz"", ""application/octet-stream""},
            {"".eot"", ""application/octet-stream""},
            {"".eps"", ""application/postscript""},
            {"".etx"", ""text/x-setext""},
            {"".evy"", ""application/envoy""},
            {"".exe"", ""application/octet-stream""},
            {"".exe.config"", ""text/xml""},
            {"".fdf"", ""application/vnd.fdf""},
            {"".fif"", ""application/fractals""},
            {"".fla"", ""application/octet-stream""},
            {"".flr"", ""x-world/x-vrml""},
            {"".flv"", ""video/x-flv""},
            {"".gif"", ""image/gif""},
            {"".gtar"", ""application/x-gtar""},
            {"".gz"", ""application/x-gzip""},
            {"".h"", ""text/plain""},
            {"".hdf"", ""application/x-hdf""},
            {"".hdml"", ""text/x-hdml""},
            {"".hhc"", ""application/x-oleobject""},
            {"".hhk"", ""application/octet-stream""},
            {"".hhp"", ""application/octet-stream""},
            {"".hlp"", ""application/winhlp""},
            {"".hqx"", ""application/mac-binhex40""},
            {"".hta"", ""application/hta""},
            {"".htc"", ""text/x-component""},
            {"".htm"", ""text/html""},
            {"".html"", ""text/html""},
            {"".htt"", ""text/webviewhtml""},
            {"".hxt"", ""text/html""},
            {"".ico"", ""image/x-icon""},
            {"".ics"", ""application/octet-stream""},
            {"".ief"", ""image/ief""},
            {"".iii"", ""application/x-iphone""},
            {"".inf"", ""application/octet-stream""},
            {"".ins"", ""application/x-internet-signup""},
            {"".isp"", ""application/x-internet-signup""},
            {"".IVF"", ""video/x-ivf""},
            {"".jar"", ""application/java-archive""},
            {"".java"", ""application/octet-stream""},
            {"".jck"", ""application/liquidmotion""},
            {"".jcz"", ""application/liquidmotion""},
            {"".jfif"", ""image/pjpeg""},
            {"".jpb"", ""application/octet-stream""},
            {"".jpe"", ""image/jpeg""},
            {"".jpeg"", ""image/jpeg""},
            {"".jpg"", ""image/jpeg""},
            {"".js"", ""application/x-javascript""},
            {"".jsx"", ""text/jscript""},
            {"".latex"", ""application/x-latex""},
            {"".lit"", ""application/x-ms-reader""},
            {"".lpk"", ""application/octet-stream""},
            {"".lsf"", ""video/x-la-asf""},
            {"".lsx"", ""video/x-la-asf""},
            {"".lzh"", ""application/octet-stream""},
            {"".m13"", ""application/x-msmediaview""},
            {"".m14"", ""application/x-msmediaview""},
            {"".m1v"", ""video/mpeg""},
            {"".m3u"", ""audio/x-mpegurl""},
            {"".man"", ""application/x-troff-man""},
            {"".manifest"", ""application/x-ms-manifest""},
            {"".map"", ""text/plain""},
            {"".mdb"", ""application/x-msaccess""},
            {"".mdp"", ""application/octet-stream""},
            {"".me"", ""application/x-troff-me""},
            {"".mht"", ""message/rfc822""},
            {"".mhtml"", ""message/rfc822""},
            {"".mid"", ""audio/mid""},
            {"".midi"", ""audio/mid""},
            {"".mix"", ""application/octet-stream""},
            {"".mmf"", ""application/x-smaf""},
            {"".mno"", ""text/xml""},
            {"".mny"", ""application/x-msmoney""},
            {"".mov"", ""video/quicktime""},
            {"".movie"", ""video/x-sgi-movie""},
            {"".mp2"", ""video/mpeg""},
            {"".mp3"", ""audio/mpeg""},
            {"".mpa"", ""video/mpeg""},
            {"".mpe"", ""video/mpeg""},
            {"".mpeg"", ""video/mpeg""},
            {"".mpg"", ""video/mpeg""},
            {"".mpp"", ""application/vnd.ms-project""},
            {"".mpv2"", ""video/mpeg""},
            {"".ms"", ""application/x-troff-ms""},
            {"".msi"", ""application/octet-stream""},
            {"".mso"", ""application/octet-stream""},
            {"".mvb"", ""application/x-msmediaview""},
            {"".mvc"", ""application/x-miva-compiled""},
            {"".nc"", ""application/x-netcdf""},
            {"".nsc"", ""video/x-ms-asf""},
            {"".nws"", ""message/rfc822""},
            {"".ocx"", ""application/octet-stream""},
            {"".oda"", ""application/oda""},
            {"".odc"", ""text/x-ms-odc""},
            {"".ods"", ""application/oleobject""},
            {"".one"", ""application/onenote""},
            {"".onea"", ""application/onenote""},
            {"".onetoc"", ""application/onenote""},
            {"".onetoc2"", ""application/onenote""},
            {"".onetmp"", ""application/onenote""},
            {"".onepkg"", ""application/onenote""},
            {"".osdx"", ""application/opensearchdescription+xml""},
            {"".p10"", ""application/pkcs10""},
            {"".p12"", ""application/x-pkcs12""},
            {"".p7b"", ""application/x-pkcs7-certificates""},
            {"".p7c"", ""application/pkcs7-mime""},
            {"".p7m"", ""application/pkcs7-mime""},
            {"".p7r"", ""application/x-pkcs7-certreqresp""},
            {"".p7s"", ""application/pkcs7-signature""},
            {"".pbm"", ""image/x-portable-bitmap""},
            {"".pcx"", ""application/octet-stream""},
            {"".pcz"", ""application/octet-stream""},
            {"".pdf"", ""application/pdf""},
            {"".pfb"", ""application/octet-stream""},
            {"".pfm"", ""application/octet-stream""},
            {"".pfx"", ""application/x-pkcs12""},
            {"".pgm"", ""image/x-portable-graymap""},
            {"".pko"", ""application/vnd.ms-pki.pko""},
            {"".pma"", ""application/x-perfmon""},
            {"".pmc"", ""application/x-perfmon""},
            {"".pml"", ""application/x-perfmon""},
            {"".pmr"", ""application/x-perfmon""},
            {"".pmw"", ""application/x-perfmon""},
            {"".png"", ""image/png""},
            {"".pnm"", ""image/x-portable-anymap""},
            {"".pnz"", ""image/png""},
            {"".pot"", ""application/vnd.ms-powerpoint""},
            {"".potm"", ""application/vnd.ms-powerpoint.template.macroEnabled.12""},
            {"".potx"", ""application/vnd.openxmlformats-officedocument.presentationml.template""},
            {"".ppam"", ""application/vnd.ms-powerpoint.addin.macroEnabled.12""},
            {"".ppm"", ""image/x-portable-pixmap""},
            {"".pps"", ""application/vnd.ms-powerpoint""},
            {"".ppsm"", ""application/vnd.ms-powerpoint.slideshow.macroEnabled.12""},
            {"".ppsx"", ""application/vnd.openxmlformats-officedocument.presentationml.slideshow""},
            {"".ppt"", ""application/vnd.ms-powerpoint""},
            {"".pptm"", ""application/vnd.ms-powerpoint.presentation.macroEnabled.12""},
            {"".pptx"", ""application/vnd.openxmlformats-officedocument.presentationml.presentation""},
            {"".prf"", ""application/pics-rules""},
            {"".prm"", ""application/octet-stream""},
            {"".prx"", ""application/octet-stream""},
            {"".ps"", ""application/postscript""},
            {"".psd"", ""application/octet-stream""},
            {"".psm"", ""application/octet-stream""},
            {"".psp"", ""application/octet-stream""},
            {"".pub"", ""application/x-mspublisher""},
            {"".qt"", ""video/quicktime""},
            {"".qtl"", ""application/x-quicktimeplayer""},
            {"".qxd"", ""application/octet-stream""},
            {"".ra"", ""audio/x-pn-realaudio""},
            {"".ram"", ""audio/x-pn-realaudio""},
            {"".rar"", ""application/octet-stream""},
            {"".ras"", ""image/x-cmu-raster""},
            {"".rf"", ""image/vnd.rn-realflash""},
            {"".rgb"", ""image/x-rgb""},
            {"".rm"", ""application/vnd.rn-realmedia""},
            {"".rmi"", ""audio/mid""},
            {"".roff"", ""application/x-troff""},
            {"".rpm"", ""audio/x-pn-realaudio-plugin""},
            {"".rtf"", ""application/rtf""},
            {"".rtx"", ""text/richtext""},
            {"".scd"", ""application/x-msschedule""},
            {"".sct"", ""text/scriptlet""},
            {"".sea"", ""application/octet-stream""},
            {"".setpay"", ""application/set-payment-initiation""},
            {"".setreg"", ""application/set-registration-initiation""},
            {"".sgml"", ""text/sgml""},
            {"".sh"", ""application/x-sh""},
            {"".shar"", ""application/x-shar""},
            {"".sit"", ""application/x-stuffit""},
            {"".sldm"", ""application/vnd.ms-powerpoint.slide.macroEnabled.12""},
            {"".sldx"", ""application/vnd.openxmlformats-officedocument.presentationml.slide""},
            {"".smd"", ""audio/x-smd""},
            {"".smi"", ""application/octet-stream""},
            {"".smx"", ""audio/x-smd""},
            {"".smz"", ""audio/x-smd""},
            {"".snd"", ""audio/basic""},
            {"".snp"", ""application/octet-stream""},
            {"".spc"", ""application/x-pkcs7-certificates""},
            {"".spl"", ""application/futuresplash""},
            {"".src"", ""application/x-wais-source""},
            {"".ssm"", ""application/streamingmedia""},
            {"".sst"", ""application/vnd.ms-pki.certstore""},
            {"".stl"", ""application/vnd.ms-pki.stl""},
            {"".sv4cpio"", ""application/x-sv4cpio""},
            {"".sv4crc"", ""application/x-sv4crc""},
            {"".swf"", ""application/x-shockwave-flash""},
            {"".t"", ""application/x-troff""},
            {"".tar"", ""application/x-tar""},
            {"".tcl"", ""application/x-tcl""},
            {"".tex"", ""application/x-tex""},
            {"".texi"", ""application/x-texinfo""},
            {"".texinfo"", ""application/x-texinfo""},
            {"".tgz"", ""application/x-compressed""},
            {"".thmx"", ""application/vnd.ms-officetheme""},
            {"".thn"", ""application/octet-stream""},
            {"".tif"", ""image/tiff""},
            {"".tiff"", ""image/tiff""},
            {"".toc"", ""application/octet-stream""},
            {"".tr"", ""application/x-troff""},
            {"".trm"", ""application/x-msterminal""},
            {"".tsv"", ""text/tab-separated-values""},
            {"".ttf"", ""application/octet-stream""},
            {"".txt"", ""text/plain""},
            {"".u32"", ""application/octet-stream""},
            {"".uls"", ""text/iuls""},
            {"".ustar"", ""application/x-ustar""},
            {"".vbs"", ""text/vbscript""},
            {"".vcf"", ""text/x-vcard""},
            {"".vcs"", ""text/plain""},
            {"".vdx"", ""application/vnd.ms-visio.viewer""},
            {"".vml"", ""text/xml""},
            {"".vsd"", ""application/vnd.visio""},
            {"".vss"", ""application/vnd.visio""},
            {"".vst"", ""application/vnd.visio""},
            {"".vsto"", ""application/x-ms-vsto""},
            {"".vsw"", ""application/vnd.visio""},
            {"".vsx"", ""application/vnd.visio""},
            {"".vtx"", ""application/vnd.visio""},
            {"".wav"", ""audio/wav""},
            {"".wax"", ""audio/x-ms-wax""},
            {"".wbmp"", ""image/vnd.wap.wbmp""},
            {"".wcm"", ""application/vnd.ms-works""},
            {"".wdb"", ""application/vnd.ms-works""},
            {"".wks"", ""application/vnd.ms-works""},
            {"".wm"", ""video/x-ms-wm""},
            {"".wma"", ""audio/x-ms-wma""},
            {"".wmd"", ""application/x-ms-wmd""},
            {"".wmf"", ""application/x-msmetafile""},
            {"".wml"", ""text/vnd.wap.wml""},
            {"".wmlc"", ""application/vnd.wap.wmlc""},
            {"".wmls"", ""text/vnd.wap.wmlscript""},
            {"".wmlsc"", ""application/vnd.wap.wmlscriptc""},
            {"".wmp"", ""video/x-ms-wmp""},
            {"".wmv"", ""video/x-ms-wmv""},
            {"".wmx"", ""video/x-ms-wmx""},
            {"".wmz"", ""application/x-ms-wmz""},
            {"".wps"", ""application/vnd.ms-works""},
            {"".wri"", ""application/x-mswrite""},
            {"".wrl"", ""x-world/x-vrml""},
            {"".wrz"", ""x-world/x-vrml""},
            {"".wsdl"", ""text/xml""},
            {"".wvx"", ""video/x-ms-wvx""},
            {"".x"", ""application/directx""},
            {"".xaf"", ""x-world/x-vrml""},
            {"".xaml"", ""application/xaml+xml""},
            {"".xap"", ""application/x-silverlight-app""},
            {"".xbap"", ""application/x-ms-xbap""},
            {"".xbm"", ""image/x-xbitmap""},
            {"".xdr"", ""text/plain""},
            {"".xla"", ""application/vnd.ms-excel""},
            {"".xlam"", ""application/vnd.ms-excel.addin.macroEnabled.12""},
            {"".xlc"", ""application/vnd.ms-excel""},
            {"".xlm"", ""application/vnd.ms-excel""},
            {"".xls"", ""application/vnd.ms-excel""},
            {"".xlsb"", ""application/vnd.ms-excel.sheet.binary.macroEnabled.12""},
            {"".xlsm"", ""application/vnd.ms-excel.sheet.macroEnabled.12""},
            {"".xlsx"", ""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet""},
            {"".xlt"", ""application/vnd.ms-excel""},
            {"".xltm"", ""application/vnd.ms-excel.template.macroEnabled.12""},
            {"".xltx"", ""application/vnd.openxmlformats-officedocument.spreadsheetml.template""},
            {"".xlw"", ""application/vnd.ms-excel""},
            {"".xml"", ""text/xml""},
            {"".xof"", ""x-world/x-vrml""},
            {"".xpm"", ""image/x-xpixmap""},
            {"".xps"", ""application/vnd.ms-xpsdocument""},
            {"".xsd"", ""text/xml""},
            {"".xsf"", ""text/xml""},
            {"".xsl"", ""text/xml""},
            {"".xslt"", ""text/xml""},
            {"".xsn"", ""application/octet-stream""},
            {"".xtp"", ""application/octet-stream""},
            {"".xwd"", ""image/x-xwindowdump""},
            {"".z"", ""application/x-compress""},
            {"".zip"", ""application/x-zip-compressed""},
        };
    }
}
    #endregion MIME HELPER

";
            #endregion MimeHelper

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
            public static string FileDataCode =
@"    /// <summary>
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
            ContentType = MimeMapping.GetMimeMapping(FileName);
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
                    cs.Append(MimeHelper);//System.Web is not present in .net core, so we have to implement it
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
                else if (!param.Type.TypeName.Equals("String", StringComparison.OrdinalIgnoreCase) && param.Type.IsList == false && param.Type.IsArray == false)
                    cspar += ".ToString(" + (param.Type.TypeName == "DateTime" ? "\"M/d/yyyy h:mm:ss tt\"" : string.Empty) + ")";

                if (param.Type.IsArray || param.Type.IsDictionary)
                {
                    if (param.HasDefaultValue)
                    {
                        cs.Append("                if (").Append(param.Name).Append(" != ").Append(FormatCSDefaultValue(param)).AppendLine(")");
                        cs.AppendLine("                {");
                    }
                    if (param.Type.IsDictionary)
                    {
                        cs.Append((param.HasDefaultValue ? "    " : "")).Append("                foreach (").Append(GetCSTypeName(param.Type).Replace("Dictionary", "KeyValuePair")).Append(" _item in ").Append(param.Name).AppendLine(")");
                        cs.Append((param.HasDefaultValue ? "    " : "")).AppendLine("                {");
                        cs.Append((param.HasDefaultValue ? "    " : "")).Append("                    values.Add(\"").Append(param.Name).AppendLine("_\" + _item.Key, _item.Value);");
                        cs.Append((param.HasDefaultValue ? "    " : "")).AppendLine("                }");
                    }
                    else
                    {
                        cs.Append((param.HasDefaultValue ? "    " : "")).Append("                foreach (").Append(GetCSTypeName(param.Type).Replace("[]", string.Empty)).Append(" _item in ").Append(param.Name).AppendLine(")");
                        cs.Append((param.HasDefaultValue ? "    " : "")).AppendLine("                {");
                        cs.Append((param.HasDefaultValue ? "    " : "")).Append("                    values.Add(\"").Append(param.Name).Append("\", _item").Append((param.Type.TypeName != "String" ? ".ToString()" : string.Empty)).AppendLine(");");
                        cs.Append((param.HasDefaultValue ? "    " : "")).AppendLine("                }");
                    }
                    if (param.HasDefaultValue)
                    {
                        cs.AppendLine("                }");
                    }
                }
                else
                {
                    cs.Append("                ").Append((param.HasDefaultValue ? "if (" + param.Name + " != " + FormatCSDefaultValue(param) + ") " : "")).Append("values.Add(\"").Append(param.Name).Append("\", ").Append((param.Type.IsList && (param.Type.IsPrimitive || param.Type.IsEnum) ? "string.Join(\",\", " + cspar + ")" : cspar)).AppendLine(");");
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
                cs.Append("    /// ").AppendLine(cls.Summary);
                cs.AppendLine("    /// </summary>");
                cs.Append("    public ").Append((cls.IsEnum ? "enum " : "class ")).AppendLine(cls.Name);
                cs.AppendLine("    {");
                foreach (var fld in cls.Fields)
                {
                    cs.AppendLine("        /// <summary>");
                    cs.Append("        /// ").AppendLine(fld.Description);
                    cs.AppendLine("        /// </summary>");
                    if (cls.IsEnum)
                        cs.Append("        ").Append(fld.Name).Append(" = ").Append(((APIDocParser.EnumField)fld).Value).AppendLine(",");
                    else
                        cs.Append("        public ").Append(GetCSTypeName(fld.Type)).Append(" ").Append(fld.Name).AppendLine(";");
                    cs.AppendLine();
                }
                cs.AppendLine("    }");
                cs.AppendLine();

                return cs.ToString();
            }

            public static string BuildCodeSampleForMethod(APIDocParser.Function func, KeyValuePair<string, APIDocParser.Category> cat)
            {
                var replaceTags = new System.Collections.Specialized.StringDictionary();

                replaceTags.Add(Environment.NewLine, "</br>");
                replaceTags.Add(">", "&gt;");
                replaceTags.Add("<", "&lt;");

                StringBuilder cs = new StringBuilder();
                bool parametersToWrite = func.Parameters.Any(o => o.Name != "apikey" && !o.HasDefaultValue);

                // generate parameters definitions
                if (parametersToWrite)
                {
                    foreach (var param in func.Parameters)
                    {
                        if (param.Name == "apikey" || param.HasDefaultValue)
                            continue;

                        cs.Append(GetCSTypeName(param.Type, forParam: true)).Append(" ").Append(param.Name);
                        cs.Append(" = ").Append(param.Type.DefaultValue /*FormatCSDefaultValue(param, true)*/).AppendLine(";");
                    }
                }
                if (parametersToWrite) cs.AppendLine();

                // create return object, if exists
                if (func.ReturnType.TypeName != null)
                    cs.Append(GetCSTypeName(func.ReturnType)).Append(" result = ").Append((func.ReturnType.IsEnum ? "ApiTypes." + func.ReturnType.TypeName + "." : string.Empty)).Append(func.ReturnType.DefaultValue).AppendLine(";");

                cs.Append(
@"try
{
    ");
                if (func.ReturnType.TypeName != null) cs.Append("result = ");

                // generate method call
                cs.Append(cat.Key).Append(".").Append(func.Name).Append("(");

                if (parametersToWrite)
                {
                    foreach (var param in func.Parameters)
                    {
                        if (param.Name == "apikey" || param.HasDefaultValue) continue;
                        cs.Append(param.Name).Append(": ").Append(param.Name).Append(", ");  // TODO This makes Email/Send call empty. Should we make a special case?
                    }

                    cs.Remove(cs.Length - 2, 2);
                }

                cs.AppendLine(");");
                cs.Append(
@"}
catch (Exception ex)
{
    if (ex is ApplicationException)
        Console.WriteLine(""Server didn't accept the request: "" + ex.Message);
    else
        Console.WriteLine(""Something unexpected happened: "" + ex.Message);

    return;
}");

                return cs.ToString().LimitLength(1000).MultipleReplaceIgnoreCase(replaceTags);
            }

            #endregion
        }
    }
}