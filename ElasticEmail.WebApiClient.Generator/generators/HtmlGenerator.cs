using ElasticEmail;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using static ElasticEmail.generators.APIDoc;

namespace EEClientGenerator.generators
{
    public static partial class APIDoc
    {
        public static class HtmlGenerator
        {
            //Dictionary for renaming param types
            //https://msdn.microsoft.com/en-us/library/hh710232%28v=vs.94%29.aspx
            static Dictionary<string, string> paramCLRTypeToDocMap = new Dictionary<string, string>
            {
                { "String", "string" },
                { "Int32", "int" },
                { "Int64", "long" },
                { "Double", "double" },
                { "Decimal", "decimal" },
                { "Boolean", "boolean"},
                { "DateTime", "datetime"},
                { "Guid", "GUID"},
                { "FilePostRequest", "POST form-data file" },
                { "FilePutRequest", "PUT input stream" },
                { "FileRequest", "POST form-data file or input stream" },
                { "FileResponse", "Output stream containing file data" },
                { "CsvFileResponse", "Output stream containing CSV file data" },
                { "XmlFileResponse", "Output stream containing XML file data" },
                { "JsonFileResponse", "Output stream containing JSON file data" },
                { "TextResponse", "Text string" },
                { "XmlResponse", "Xml string" },
                { "HtmlResponse", "Html string" },
                { "JavascriptResponse", "Javascript string" },
                { "JsonResponse", "Json string" }
            };

            static string GetDocTypeName(APIDocParser.DataType dataType)
            {
                string typeName = dataType.TypeName;

                if (dataType.IsDictionary)
                {
                    string[] subtypes = typeName.Split(',');
                    for (int i = 0; i < 2; i++)
                    {
                        var tmpName = subtypes[i];
                        bool wasFound = paramCLRTypeToDocMap.TryGetValue(tmpName, out subtypes[i]);
                        if (!wasFound) subtypes[i] = tmpName;
                    }
                    typeName = "Repeated list of " + subtypes[0] + " keys and " + subtypes[1] + " values";
                    return typeName;
                }

                if (dataType.TypeName != null && (dataType.IsPrimitive || dataType.IsFile) && paramCLRTypeToDocMap.TryGetValue(dataType.TypeName, out typeName) == false)
                    throw new Exception("Unknown type - " + dataType.TypeName);

                if (dataType.IsList) typeName = "List of " + typeName;
                if (dataType.IsArray) typeName = "Repeated list of " + typeName;
                //if (dataType.IsDictionary) typeName = "Dictionary";
                if (dataType.IsNullable) typeName += "?";
                return typeName ?? "";
            }

            public static string Generate(APIDocParser.Project project)
            {
                StringBuilder htmlpage = new StringBuilder();

                //HEAD TAG
                htmlpage.Append(@"
        <!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd""><html xmlns=""http://www.w3.org/1999/xhtml"">
        <head><meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
        <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
        <title>Cloud Email Delivery | SMTP Relay, EC2 Email | Elastic Email | API Documentation</title>
		<script language=""javascript"" type=""text/javascript"" src=""https://ajax.googleapis.com/ajax/libs/jquery/1.11.2/jquery.min.js""></script>
        <link rel=""shortcut icon"" href=""https://elasticemail.com/_library/favicon.ico"" type=""image/x-icon"" />
        <meta name=""description"" content=""Elastic Email is the lowest cost provider of high quality email delivery service and software for your cloud application or marketing needs."" />
        <meta name=""keywords"" content=""bulk email marketing software, bulk email services, cloud email, cloud email services, free email marketing, how to send a email, mass email marketing, mass email services, mass emails, send mass email, smtp email server, smtp relay, smtp relay server, smtp relays, transactional email, 67cbb5b503449c7084d7"" />
        <script language=""javascript"" type=""text/javascript"" src=""https://ajax.googleapis.com/ajax/libs/jquery/1.11.2/jquery.min.js""></script>
        <script type=""text/javascript"" src=""//maxcdn.bootstrapcdn.com/bootstrap/3.3.4/js/bootstrap.min.js""></script>        
        <link href='https://fonts.googleapis.com/css?family=Open+Sans:400,600,600italic,700' rel='stylesheet' type='text/css'>
        <link rel=""stylesheet"" href=""https://maxcdn.bootstrapcdn.com/bootstrap/3.3.4/css/bootstrap.min.css"" type=""text/css"">
        <link rel=""stylesheet"" href=""https://maxcdn.bootstrapcdn.com/font-awesome/4.3.0/css/font-awesome.min.css"" type=""text/css"">
		<script type=""text/javascript"" src=""https://maxcdn.bootstrapcdn.com/bootstrap/3.3.4/js/bootstrap.min.js""></script>
		<script type=""text/javascript"" src=""https://cdnjs.cloudflare.com/ajax/libs/jquery.devbridge-autocomplete/1.2.24/jquery.autocomplete.min.js""></script>        
        <style>
		body {font-family: 'Open Sans', sans-serif; position: relative; background: #f6f6f6; max-width: 920px;}
		h2 {display:inline-block; word-break: break-all; font-weight: 600; margin-top: 0; font-size: 60px; color: #616161; line-height: 80px; padding-top: 80px; margin-bottom: 0px;}
		h2 small {font-weight: 400; font-size: 20px; color: #666666}
		h3 {display:inline-block; word-break: break-all; font-weight: 600; font-size: 24px; color: #424242; padding-top: 100px; margin-bottom: 5px;}
		h3 small {display: inline-block; padding-top: 5px; font-weight: 400; font-size: 16px; color: #333333; word-break: keep-all;}
		h4 {word-break: break-all; font-weight: 600; font-size: 24px; color: #333333; margin-bottom: 25px; margin-top: 60px;}
		hr {border-top: 1px solid #24cc82}
        .navbar-brand svg {width: 200px}
		code.method_path {word-break: break-all; background-color: #cfd8dc; color: #666666; padding: 10px 18px; border-radius: 7px; font-size: 16px; display: inline-block; margin-top: 30px;}
		.samp { padding: 15px; border: 1px solid #cfd8dc; border-radius: 6px; background: #eceff1; color: #999999; word-break: break-all; float: left; font-size: 14px}
		.tablecontainer {background: #eceff1; padding: 15px; font-size: 16px; border-radius: 7px; }
		thead {color: #78909c; font-weight: 700;}
		.table>thead>tr>th, .table>thead>tr>td { border-bottom: 0}
		.table>tbody>tr>td { border-top: 1px solid #cfd8dc}
		.table>tbody>tr:first-child>td { border-top: 0}
		.table-condensed>tbody>tr>td, .table-condensed>tbody>tr>th, .table-condensed>tfoot>tr>td, .table-condensed>tfoot>tr>th, .table-condensed>thead>tr>td, .table-condensed>thead>tr>th {10px 5px;}
		tbody {color: #333333; font-size: 14px;}
		th {font-size: 18px}
		#autocomplete {border-bottom: 1px solid white; width: calc(100% - 50px);;border: none;color: #bdbdbd;font-size: 16px;margin-top: 0px;outline: none !important; padding-top: 4px;display: inline-block;margin-top: 0px;margin-bottom: 0px;line-height: 29px;vertical-align: middle;}
		#autocomplete:focus, #autocomplete:hover { -webkit-transition: width 0.5s ease-in-out; -moz-transition: width 0.5s ease-in-out; -o-transition: width 0.5s ease-in-out; transition: width 0.5s ease-in-out; width: calc(100% - 50px); border-bottom: 1px solid #24cc82 }
		#searchcontainer { max-width: 920px; margin: auto; margin-top: 15px;}
		#documentation_body { position: relative; z-index: 100}
		#documentation_body p { margin: 30px 0 }
		#overview_content .tablecontainer {margin: 20px 0;}
		section[id$='_content'] {overflow: auto; margin-bottom: 100px;}
		.btn-primary {font-size: 18px; background: #1976d2; padding: 7px 15px; margin: 70px 0 0; border: 0; font-size: 16px;}    
		#site_header {position: fixed; left: 0; width: 100%; background-color: #ffffff; z-index: 1000; }
		#main_nav {width: 230px; max-width: 240px; position: fixed; top: 60px; left: 0; overflow-x: hidden; background: #f3f3f3}
		#main-nav li a, #main-nav li ul li a { color: #616161; text-decoration: none;}
		#main_nav *{background-color: transparent; color: #616161; font-size: 14px; text-decoration: none; border: 0; padding: 2px 5px;};
		#main_nav a:hover, a, a:hover {color: #24cc82}
		#main_nav .navbar-toggle {position: relative;float: right;padding: 9px 10px;margin-top: 8px;margin-right: 0;margin-bottom: 8px;background-color: transparent;background-image: none;border: 1px solid #999999;border-radius: 4px;}
		#main_nav .navbar-toggle .icon-bar {background-color: #888;display: block;width: 22px;height: 2px;border-radius: 1px;padding: 0px;}
		#api_menu {min-height: 100%; height: auto; padding: 30px 0; overflow: auto; }
		#api_menu .badge {background: #cfd8dc; color: #757575; font-size: 12px; padding: 3px 7px; font-weight: normal}
		#api_menu .active .badge {font-weight: 700; background: #24cc82; color: white;}
        #api_menu li .nav { display: none;}
		#api_menu li.active .nav { display: block;}
		#api_menu>.active>a, #api_menu>.active .active>a {font-weight: 700; color: #24cc82}
		#main_nav .category_menu a {font-size: 12px; border-left: 3px solid transparent; line-height: 16px; margin: 1px 0; text-overflow: ellipsis; overflow: hidden;}
		#main_nav .category_menu .active a  {border-left: 3px solid #24cc82;}
		#api_menu li:last-of-type ul {padding-bottom:30px !important}
        #navbar_toggle {padding: 15px; border: 1px solid #e3e3e3; margin: 5px 10px; color: #666666;}
		#start h2 { margin-bottom 95px;}
		#start p {font-size: 18px}
		#start .apidescription {font-size: 14px; padding-left: 15px}
		#start .tablecontainer { margin-bottom: 15px; margin-top: 35px; padding: 15px 20px; }
		.autocomplete-suggestions { border: 1px solid #f3f3f3; background: #FFF; overflow: auto; left: 60px !important; max-width: calc(100% - 50px); !important; cursor: pointer; padding: 5px; }
        .autocomplete-suggestion {padding: 2px;}
        .autocomplete-suggestion:hover {background: #24cc82; color: white; }
        .param_collapse {transition: .3s all; -webkit-transition: .3s all;}
        .param_collapse.collapsed i {    -webkit-transform: rotate(180deg); -ms-transform: rotate(180deg); transform: rotate(180deg);}
        #documentation_body #apichangeloglist p { margin: 0 0 0 0; line-height: 2em; }
        #apichangelog {margin-bottom: 2em;}
        #apichangeloglist article {padding: 15px; margin: 20px; border-radius: 15px; background: #eceff1;}
        #apichangeloglist li {line-height: 2em;}
        #apichangeloglist code {font-size: 100%; background: transparent; color: #333;}
        ::-webkit-scrollbar { width: 9px;}
		::-webkit-scrollbar-track { background-color: #f3f3f3; border-radius: 4px; }
		::-webkit-scrollbar-thumb { background-color: #cfd8dc; height: 60px;}
		::-webkit-scrollbar-thumb:hover { background-color: #cfd8dc;}
		::-moz-selection { /* Code for Firefox */ background: #24cc82; color: white; padding: .2em .3em;}
		::selection, mark { background: #24cc82; color: white; padding: .2em 0.3em;}
		@media screen and (min-width: 768px) and (max-width:1400px) { .container, #searchcontainer { margin-left: 250px; max-width: calc(100% - 250px); } #main_nav {background: transparent; width: auto; } #api_menu {background: #f3f3f3; width: 230px; } }
		@media screen and (max-width: 768px) { .autocomplete-suggestions {left: 60px !important } #site_header {position:fixed; top: 0; left: 0; background: white; width: 100%; height: 120px; z-index: 1000000} #documentation_body {margin: -15px; margin-top: 60px; padding: 15px; padding-top: 60px; background: #f6f6f6; overflow: auto;} h2 {font-size: 40px; color: #616161; line-height: 60px; padding-top: 140px} h3 { padding-top: 150px } #main_nav {top: 120px;}}
		</style>
        </HEAD>");

                //BODY
                htmlpage.Append(@"<body class=""container"" data-spy=""scroll"" data-target="".navbar"">");
                htmlpage.Append(@"<header id=""site_header""><button type=""button"" id=""navbar_toggle"" class=""pull-left navbar-toggle collapsed"" aria-expanded=""false"">
			                        <span class=""sr-only"">Toggle navigation</span><i class=""fa fa-bars fa-lg""></i><i class=""fa fa-arrow-left fa-lg"" style=""display:none""></i></button><div class=""navbar-header"" style=""height: 60px;""><a href=""http://elasticemail.com"" class=""navbar-brand"">
			<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 317.47 40.38'><defs><style>.cls-1{fill:#9b3534;}.cls-2{fill:#4e4e50;}</style></defs><title>ee_logo_</title><path id='znak' class='cls-1' d='M179.12 16.58h-3.3v3h3.3a5.21 5.21 0 1 1 0 10.41h-23.71a5.21 5.21 0 0 1 0-10.41h11.59v2.27l6.53-3.77-6.53-3.76v2.27h-11.59a8.21 8.21 0 0 0 0 16.41h23.71a8.21 8.21 0 1 0 0-16.42z'/><g id='Elastic'><path class='cls-2' d='M4.87 22.06h14.67v-4.03h-14.67v-11.93h15.51v-4.08h-20.38v37.74h21.22v-4.09h-16.35v-13.61zM26.82 0h4.93v39.76h-4.93zM58.52 23.13c0-5.43-2-11.09-10.3-11.09a16.82 16.82 0 0 0-8.9 2.41l1.12 3.25a13.5 13.5 0 0 1 7.06-2c5.54 0 6.16 4 6.16 6.27v.56c-10.47-.06-16.3 3.53-16.3 10.08 0 3.92 2.8 7.78 8.29 7.78a10.1 10.1 0 0 0 8.29-4h.17l.39 3.42h4.5a38.17 38.17 0 0 1-.45-6.5v-10.18zm-4.76 7.5a5.14 5.14 0 0 1-.28 1.57 6.74 6.74 0 0 1-6.55 4.54 4.36 4.36 0 0 1-4.65-4.74c0-5.26 6.1-6.22 11.48-6.1v4.7zM74.65 24c-3.53-1.34-5.15-2.35-5.15-4.59 0-2 1.62-3.7 4.54-3.7a10.55 10.55 0 0 1 5.54 1.57l1.23-3.58a13.53 13.53 0 0 0-6.67-1.7c-5.82 0-9.35 3.58-9.35 8 0 3.25 2.3 5.94 7.17 7.67 3.64 1.34 5 2.63 5 5s-1.68 4-5.26 4a12.91 12.91 0 0 1-6.5-2l-1.2 3.8a15.45 15.45 0 0 0 7.62 1.85c6.44 0 10.14-3.36 10.14-8.12 0-4.03-2.41-6.38-7.11-8.2zM93.46 4.87l-4.81 1.29v6.5h-4.2v3.75h4.2v14.78c0 3.19.5 5.6 1.9 7.06a6.83 6.83 0 0 0 5.32 2.07 12.07 12.07 0 0 0 4.37-.67l-.24-3.65a11 11 0 0 1-2.86.34c-2.74 0-3.7-1.9-3.7-5.26v-14.67h7.06v-3.75h-7.04v-7.79zM108 2a3 3 0 0 0-3.11 3 3.05 3.05 0 0 0 6.11 0 2.93 2.93 0 0 0-3-3zM105.5 12.66h4.93v27.1h-4.93zM130.48 36.4c-5.38 0-9.41-3.86-9.41-10.14 0-5.66 3.36-10.25 9.58-10.25a11.43 11.43 0 0 1 5.71 1.29l1.12-3.81a16.47 16.47 0 0 0-6.83-1.4c-8.85 0-14.56 6-14.56 14.39s5.32 13.83 13.5 13.83a18.15 18.15 0 0 0 7.78-1.57l-.84-3.7a14 14 0 0 1-6.05 1.36z'/></g><g id='Email'><path class='cls-1' d='M205.47 22.06h14.67v-4.03h-14.67v-11.93h15.51v-4.08h-20.39v37.74h21.23v-4.09h-16.35v-13.61zM257.21 12a9.25 9.25 0 0 0-6 1.9 11.17 11.17 0 0 0-3 3.53h-.11a8 8 0 0 0-7.69-5.43 9.34 9.34 0 0 0-8.51 5h-.17l-.22-4.37h-4.31c.17 2.24.22 4.54.22 7.34v19.79h4.82v-16.35a6.8 6.8 0 0 1 .39-2.41c.78-2.46 3-4.93 6.1-4.93 3.75 0 5.66 3.14 5.66 7.45v16.24h4.82v-16.76a8.13 8.13 0 0 1 .39-2.46 6.38 6.38 0 0 1 5.82-4.48c4 0 5.88 3.14 5.88 8.34v15.36h4.82v-16c-.01-9.37-5.33-11.76-8.91-11.76zM292.6 23.13c0-5.43-2-11.09-10.3-11.09a16.82 16.82 0 0 0-8.9 2.41l1.12 3.25a13.5 13.5 0 0 1 7.06-2c5.54 0 6.16 4 6.16 6.27v.56c-10.47-.06-16.3 3.53-16.3 10.08 0 3.92 2.8 7.78 8.29 7.78a10.1 10.1 0 0 0 8.29-4h.17l.39 3.42h4.48a38.17 38.17 0 0 1-.45-6.5v-10.18zm-4.76 7.5a5.14 5.14 0 0 1-.28 1.57 6.74 6.74 0 0 1-6.56 4.54 4.36 4.36 0 0 1-4.65-4.7c0-5.26 6.1-6.22 11.48-6.1v4.7zM302.46 2a3 3 0 0 0-3.08 3 3.05 3.05 0 0 0 6.1 0 2.93 2.93 0 0 0-3.02-3zM299.99 12.66h4.93v27.1h-4.93zM312.54 0h4.93v39.76h-4.93z'/></g></svg>
            </a></div>
            <div id='searchcontainer'><div class=""col-xs-12""><i class='fa fa-lg fa-search' style=""margin-right: 30px; color: #bdbdbd""></i><input type='text' placeholder='Search' name='suggestions' id='autocomplete'/></div></div>
            </header>");
                htmlpage.Append(@"<nav id=""main_nav"" class=""navbar""><ul id=""api_menu""  class=""nav"">");

                //MAIN MENU
                htmlpage.Append(@"  <li ><a class="""" href=""#start"">Overview</a></li>");
                htmlpage.Append(@"  <li ><a class="""" href=""#apichangelog"">Changelog</a></li>");
                foreach (var cat in project.Categories.OrderBy(f => f.Value.Name))
                {
                    htmlpage.Append(@"<li><a class="""" href=""#" + cat.Value.Name + @"_header"">" + cat.Value.Name + @" <span class=""badge pull-right"">" + cat.Value.Functions.Count + @"</span></a><ul class=""nav category_menu"">");

                    //METHOD MENU
                    foreach (var method in cat.Value.Functions.OrderBy(f => f.Name))
                    {
                        htmlpage.Append(@"<li><a style=""padding-left: 15px;"" href=""#" + cat.Value.Name + "_" + method.Name + @""">" + method.Name + @"</a></li>");
                    }
                    htmlpage.Append(@"</ul></li>");
                }
                htmlpage.Append(@"<li><a href=""#classes_content"" >Classes <span class=""badge pull-right"">" + project.Classes.Count + @"</span></a><ul class=""nav category_menu"" >");

                //CLASSES MENU
                foreach (var cls in project.Classes.OrderBy(f => f.Name))
                {
                    htmlpage.Append(@"<li><a style=""padding-left: 15px;"" href=""#classes_" + cls.Name + @""">" + cls.Name + @"</a></li>");
                }
                htmlpage.Append(@"</ul></li>");
                htmlpage.Append(@"</ul></nav>");

                //CONTENT
                htmlpage.Append(@"  <section id=""documentation_body""><section class="""" id=""start"">
				                <h2>API Documentation</h2> 
				                <p>Welcome to the Elastic Email API Documentation. </p>
				                <p>If you are a developer building an application we recommend using this HTTP API, which is more flexible and efficient than standard SMTP.</p>
				                <p>This API is a powerful service which allows direct access to all functionality for the Elastic Email Dashboard and additional calls that may be required for tight integration with our Email services.</p>
				                <p>Most of API requests should be sent using an HTTP <mark>GET</mark> method. If a method needs sending using an HTTP <mark>POST</mark> method, it is designated in the method description.</p>
			                    <div class=""col-xs-12"">
				                    <div class=""col-xs-12 tablecontainer""> 
				                    <table id=""overview"" class=""table table-condensed"" style=""table-layout: fixed;  word-wrap: break-word;"">
				                      <thead>
					                    <tr class=""fullpath"" ><th rowspan=""2"" class=""col-xs-2"" >Use</th><th colspan=""3"" style=""text-align:center; border-bottom:1px solid #24cc82;"">Full Path for API connection</th></tr>
					                    <tr><th class=""col-xs-4"">Base URL</th><th class=""col-xs-3"">Path</th><th class=""col-xs-3"">Parameters</th></tr>
				                      </thead>
				                      <tbody>
					                    <tr><td>Elastic Email</td><td >https://api.elasticemail.com</td><td rowspan=""2"" style=""vertical-align: middle;"">/v2/category/action</td><td rowspan=""2"" style=""vertical-align: middle;"">?param1=value1&amp;param2=value2</td></tr>
					                    <tr><td>Private Branding*</td><td>https://api.yourdomain.com</td></tr>
					                    <tr><td>Example</td><td>https://api.elasticemail.com</td><td >/v2/contact/list</td><td>?apikey=your-apikey</td></tr>
				                      </tbody>
				                    </table>
				
				                    </div>
				                    <p class=""apidescription"">* You can create a CNAME in your DNS software and turn on <a href=""https://elasticemail.com/support/user-interface/custom-branding"" style=""color:#22b272"">private branding</a> on your Account screen to brand api calls for your customers</p>
			                    </div>                
                                <p>Common API Calls:</p>
                                <ul>
                                    <li><a href=""#Email_Send"">Send an Email</a></li>
                                    <li><a href=""#Campaign_Add"">Submit a Campaign</a></li>
                                    <li><a href=""#Log_Export"">Export a Log</a></li>
                                    <li><a href=""#SMS_header"">Send an SMS</a></li>
                                </ul>
				                <p>Interface Libraries for this API have been written for several languages. Access them along with code samples here: <a href=""http://elasticemail.com/support/http-api/integration-libraries"">http://elasticemail.com/support/http-api/integration-libraries</a></p>
			                    </section>");


                htmlpage.Append(Changelog.Value);

                foreach (var cat in project.Categories.OrderBy(f => f.Value.Name))
                {
                    htmlpage.Append(@"<div id=""" + cat.Value.Name + @"_content"" class=""category_body"">");
                    htmlpage.Append(@"<h2 id=""" + cat.Value.Name + @"_header"">" + cat.Value.Name + @"<br/><small>" + cat.Value.Summary + @"</small></h2>");

                    foreach (var method in cat.Value.Functions.OrderBy(f => f.Name))
                    {
                        htmlpage.Append(@"<section><h3 class=""method_name"" id=""" + cat.Value.Name + "_" + method.Name + @""">" + method.Name + @"<br/><small>" + method.Summary + @"</small></h3><hr/>" +
                            @"<section class=""col-xs-12""><code class=""method_path"" style=""text-transform:lowercase;"">/" + cat.Value.Name + "/" + method.Name + @"</code>");
                        if (method.Parameters != null)
                        {
                            htmlpage.Append(@"<h4 data-toggle=""collapse"" class=""param_collapse collapsed"" data-target=""#" + cat.Value.Name + "_" + method.Name + @"_parameters"" style=""cursor:pointer;""><em>Parameters</em> <i class=""fa fa-chevron-up""></i></h4>");
                            htmlpage.Append(@"<div class=""col-xs-12 tablecontainer collapse"" id=""" + cat.Value.Name + "_" + method.Name + @"_parameters"">				<table class=""table  table-hover table-condensed  method_description"" style=""table-layout: fixed;  word-wrap: break-word;"" >
					<thead>
						<tr>
							<td><strong>Name</strong></td>
							<td><strong>Type</strong></td>
							<td><strong>Required</strong></td>
							<td><strong>Default</strong></td>
							<td><strong>Description</strong></td>
						</tr>
					</thead>
					<tbody>");
                            foreach (var param in method.Parameters.OrderBy(f => f.Name))
                            {
                                if (param.IsFilePutUpload || param.IsFilePostUpload)
                                    continue;

                                string clsTypeHtml = null;
                                if (param.Type.IsPrimitive || param.Type.IsFile || param.Type.IsDictionary)
                                    clsTypeHtml = GetDocTypeName(param.Type);
                                else
                                    clsTypeHtml = @"<a class=""text-default"" href='#classes_" + param.Type.TypeName + "' data-target='" + param.Type.TypeName + "'>" + GetDocTypeName(param.Type) + "</a>";

                                string defaultValue = CSGenerator.FormatCSDefaultValue(param);

                                htmlpage.Append(@"<tr><td class=""col-sm-3"" >" + param.Name + "</td><td>" + clsTypeHtml + "</td><td>" + (string.IsNullOrEmpty(defaultValue) ? "Yes" : "No") + "</td><td>" +
                                                defaultValue + "</td><td>" + param.Description + "</td></tr>");
                            }

                            if (method.Parameters.Any(f => f.IsFilePostUpload && f.IsFilePutUpload))
                            {
                                htmlpage.Append(@"<tr><td colspan=""4""><h4>Attach the file as POST multipart/form-data file upload or PUT file upload with content-disposition header</h4></td></tr>");
                            }
                            else if (method.Parameters.Any(f => f.IsFilePostUpload))
                            {
                                htmlpage.Append(@"<tr><td colspan=""4""><h4>Attach the file as POST multipart/form-data file upload</h4></td></tr>");
                            }
                            else if (method.Parameters.Any(f => f.IsFilePutUpload))
                            {
                                htmlpage.Append(@"<tr><td colspan=""4""><h4>Attach the file as PUT file upload</h4></td></tr>");
                            }
                            htmlpage.Append(@"</tbody> </table> </section>");
                        }

                        htmlpage.Append(@"<section class=""col-xs-12""><h4><em>Returns</em></h4>");
                        htmlpage.Append(@"<div class=""samp""><samp>{""success"": true, ""error"": null, ""data"": <strong>");
                        if (method.ReturnType.TypeName == null)
                        {
                            htmlpage.Append(@"""""");
                        }
                        else if (method.ReturnType.IsPrimitive || method.ReturnType.IsFile)
                        {
                            htmlpage.Append(@"{ " + GetDocTypeName(method.ReturnType) + " }");
                        }
                        else
                        {
                            htmlpage.Append(@"{ <a class=""text-default"" href='#classes_" + method.ReturnType.TypeName + "' data-target='" + method.ReturnType.TypeName + "'>" + GetDocTypeName(method.ReturnType) + "</a>" + " }");
                        }
                        htmlpage.Append(" </strong>}</samp></div></section>");

                        if (method.Example != null)
                        {
                            htmlpage.Append(@"<section class=""col-xs-12""><h4><em>Example</em> <sup class=""btn badge"" data-copy='" + cat.Value.Name + "_" + method.Name + "' style='margin-left:5px; background: #24cc82; padding: 4px 8px; font-weight: 400; color: white; height: 28px; border-radius: 14px; line-height: 17px;'><i class='fa fa-link'></i> copy link</sup></h4>");
                            htmlpage.Append(@"<div class=""samp""><samp data-copy_target='" + cat.Value.Name + "_" + method.Name + "'>" + method.Example + "</samp></div></section>");
                        }
                        htmlpage.Append(@"<section class=""col-xs-12"">
					        <a class=""btn btn-primary back_to_top"" href=""#start"">Back to top</a></section></section>");
                    }

                    htmlpage.Append(@"</div>");
                }

                //CLASSES
                htmlpage.Append(@"<div id=""classes_content"" class=""api_content col-xs-12 col-sm-8 col-md-9"">");
                htmlpage.Append(@"<h2>Classes<br/>");
                htmlpage.Append(@"<small>Classes used in Elastic Email API</small></h2>");
                foreach (var cls in project.Classes.OrderBy(f => f.Name))
                {
                    htmlpage.Append(@"<section><h3 class=""method_name"" id=""classes_" + cls.Name + @""">" + cls.Name + (cls.IsEnum ? " Enumeration" : "") + @"<br/><small>" + cls.Summary + @"</small></h3><hr/>");
                    htmlpage.Append(@"<table class=""table table-hover table-condensed returns_description"" style=""table-layout: fixed;  word-wrap: break-word;"">
                                            <thead><tr><th class=""method_name col-xl-3 col-md-4 col-sm-4"">" + (cls.IsEnum ? " Values" : "Properties") + @"</th>" + (cls.IsEnum ? "" : @"<th class=""col-xl-6 col-md-6 col-sm-6"">Description</th>") +
                        @"<th class=""col-xl-3 col-md-2 col-sm-2 "">" + (cls.IsEnum ? "Value" : "Example") + @"</th><th class=""col-xl-3 col-md-2 col-sm-2 "">" + (cls.IsEnum ? "Description" : "Type") + @"</th></tr></thead><tbody>");
                    if (cls.Fields != null)
                    {
                        foreach (var field in cls.Fields.OrderBy(f => f.Name))
                        {
                            if (cls.IsEnum)
                            {
                                htmlpage.Append(@"<tr><td class=""col-xs-2"" style=""padding-left:30px;"">" + field.Name + @"</td><td class=""col-xs-2""> " + ((APIDocParser.EnumField)field).Value + @"</td><td class=""col-xs-8"">" + field.Description + @"</td></tr>");
                            }
                            else
                            {
                                string clsTypeHtml = null;
                                if (field.Type.IsPrimitive || field.Type.IsFile || field.Type.IsDictionary)
                                {
                                    clsTypeHtml = GetDocTypeName(field.Type);
                                }
                                else
                                {
                                    clsTypeHtml = @"<a class=""text-default"" href='#classes_" + field.Type.TypeName + "' data-target='" + field.Type.TypeName + "'>" + GetDocTypeName(field.Type) + "</a>";
                                }

                                htmlpage.Append(@"<tr><td class=""col-xs-2"" style=""padding-left:30px;"">" + field.Name + @"</td><td class=""col-xs-4""> " + field.Description + @"</td><td class=""col-xs-4"">" + field.Example + @"</td><td class=""col-xs-2"">" + clsTypeHtml + @"</td></tr>");
                            }
                        }
                    }
                    htmlpage.Append("</tbody></table>");
                    htmlpage.Append(@"<div><a class=""btn btn-primary back_to_top"" href=""#start"">Back to top</a></div></section>");
                }
                htmlpage.Append(@"</div>");

                htmlpage.Append(@"</div></section>
                                <script>
                                        $(document).ready(function () {
										var $documentation = $('#documentation_body');
										var $navbarbtn = $('#navbar_toggle');
										$('#main_nav').css('height', ($(window).height() - (($(window).width() > 767) ? 60 : 120 ) + 'px'));
									    $('#api_menu').css('height', ($(window).height() - (($(window).width() > 767) ? 140 : 200 ) + 'px'));
										$('#api_menu>li>a').on('click', function(e) {
											var $menuitem = $(this);
											if ($menuitem.parent().hasClass('active')) {
												e.preventDefault();
												$('#api_menu').children('.active').removeClass('active');
											} else $menuitem.parent().addClass('active');
										});

										$navbarbtn.on('click', function () {
											if ($documentation.css('left') != '250px') {
												$documentation.stop().animate({'left': '250px'}, 600 );
												$navbarbtn.find('.fa').toggle(200)
												} else {;
												$documentation.stop().animate({'left': '0px'}, 400 );
												$navbarbtn.find('.fa').toggle(200)
												}
										});
                                        var suggestions = [");
                foreach (var cat in project.Categories.OrderBy(f => f.Value.Name))
                {
                    foreach (var method in cat.Value.Functions.OrderBy(f => f.Name))
                    {
                        htmlpage.Append(@"{ value: """ + cat.Value.Name + " " + method.Name + @""", data: {name: """ + method.Name + @""", category: """ + cat.Value.Name + @""", description: """ + method.Summary + @"""} },");
                    }
                }
                foreach (var cls in project.Classes.OrderBy(f => f.Name))
                {
                    htmlpage.Append(@"{ value: """ + cls.Name + @""", data: {name: """ + cls.Name + @""" ,category: ""classes"", description: """ + cls.Summary + @"""} },");
                }
                htmlpage.Append(@"];
										$('#autocomplete').autocomplete({
										lookup: suggestions,
										formatResult: function(suggestion, currentValue) {
										return suggestion.value + ' <small>' + suggestion.data.description + '</small>';
										},
										onSelect: function (suggestion) {
											$(document).scrollTop( $('#' + suggestion.data.category + '_' + suggestion.data.name).offset().top ); 
											}
										});
										var clickToCopy = {

											init: function (callback) {

												var elem = $('[data-copy]'),
													obj = this;

												elem.unbind('click').click( function () {

													if (obj.test()) {

														var $this = $(this),
															toCopy = $this.data('copy'),
															nodeContent = $('[data-copy_target=""' + toCopy + '""]'),
															selection = window.getSelection(),
															range = document.createRange(),
                                                            htmlcontent = $this.html();

														if (nodeContent.length > 0 ) {
															range.selectNodeContents(nodeContent[0]);
														} else {
															console.log('No data-copy_target attr!!');
														}

														selection.removeAllRanges();
														selection.addRange(range);
													    $this.html('<i class=""fa fa-check""></i> copied!');
														    setTimeout(function() {
															$this.html(htmlcontent);
														}, 5000);

														try {
															document.execCommand('copy');
															if (callback) callback();
														} catch (err) {
															console.log(err)
														};

													}

												});

											},
											test: function () {
												return !!window.getSelection && !!document.execCommand;
											}
										}
										if (clickToCopy.test()){
											clickToCopy.init();
										}		
                                    });
                                <!-- Hotjar Tracking Code for http://api.elasticemail.com/public/help -->
                                    (function(h,o,t,j,a,r){
                                        h.hj=h.hj||function(){(h.hj.q=h.hj.q||[]).push(arguments)};
                                        h._hjSettings={hjid:188952,hjsv:5};
                                        a=o.getElementsByTagName('head')[0];
                                        r=o.createElement('script');r.async=1;
                                        r.src=t+h._hjSettings.hjid+j+h._hjSettings.hjsv;
                                        a.appendChild(r);
                                    })(window,document,'//static.hotjar.com/c/hotjar-','.js?sv=');
                                </script>
                                </body></html>");

                return htmlpage.ToString();
            }


            public static string ApiLicense =
@"The MIT License (MIT)

Copyright (c) 2016-2017 Elastic Email, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.";


            #region Changelog

            public class ChangeLog
            {
                [JsonProperty(PropertyName = "latest_version")]
                public string LatestVersion { get; set; }

                [JsonProperty(PropertyName = "release_date")]
                public DateTime ReleaseDate { get; set; }

                [JsonProperty(PropertyName = "changelog")]
                public Entry[] Entries { get; set; }

                public class Entry
                {
                    public string Version { get; set; }
                    public DateTime Date { get; set; }
                    public string Changes { get; set; }

                    [JsonProperty(PropertyName = "affected")]
                    public Affected[] AffectedParts { get; set; }

                    public class Affected
                    {
                        [JsonProperty(PropertyName = "class_name")]
                        public string ClassName { get; set; }

                        [JsonProperty(PropertyName = "method_name")]
                        public string MethodName { get; set; }

                        [JsonProperty(PropertyName = "parameter_name")]
                        public string ParameterName { get; set; }

                        public WhatAffected What { get; set; }

                        public HowAffected How { get; set; }

                        public enum WhatAffected
                        {
                            Class,
                            HelperClass,
                            Method,
                            Parameter
                        }
                        public enum HowAffected
                        {
                            Add,
                            Modify,
                            Delete
                        }
                    }
                }
            }

            private static Lazy<string> Changelog = new Lazy<string>(LoadChangelog);

            private static string LoadChangelog()
            {
                ChangeLog changelog = null;

                using (StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("ElasticEmailAPI.v2.docs.Changelog.json")))
                {
                    string json = reader.ReadToEnd();
                    changelog = JsonConvert.DeserializeObject<ChangeLog>(json);
                }

                StringBuilder log = new StringBuilder(128);

                log.AppendLine("<div id='apichangelog'>");
                log.AppendLine("<h2>Recent updates <small>Current API version: " + changelog.Entries.First().Version + "</small></h2>");
                log.AppendLine("<p style=\"font - size: 18px; line - height: 1.5em;\">We at Elastic Email improve our system daily by releasing new features, eliminating bugs, and bringing you fresh documentation. Have a look and catch up with the latest changes.</p>");
                log.AppendLine("<div id='apichangeloglist' style='max-height: 800px; overflow-y: auto;'>");

                List<string> months = new List<string>();
                ChangeLog.Entry change = new ChangeLog.Entry();

                for (int i = 0; i < changelog.Entries.Length; i++)
                {
                    change = changelog.Entries[i];
                    string month = change.Date.ToString("MMMM yyyy");
                    if (!(months.Contains(month)))
                    {
                        log.AppendLine("<h3 style='display:block;padding-bottom: 10px; border-bottom: 1px solid #24cc82; padding-top: 30px'>" + month + "</h3>");
                        months.Add(month);
                    }

                    log.AppendLine("<article>");
                    //log.AppendLine("<li>Version: " + change.Version + "</li>");
                    log.AppendLine("<p><b>" + change.Date.ToShortDateString() + "</b></p>");
                    log.AppendLine("<p>" + change.Changes + "</p>");
                    if (change.AffectedParts != null) { log.AppendLine("<a href='#readmore_" + i + "' data-toggle='collapse' aria-expanded='false' aria-controls='readmore_" + i + "'>Read more</a></p>"); }

                    if (change.AffectedParts != null)
                    {
                        log.AppendLine("<div class='collapse' id='readmore_" + i + "'>");
                        log.AppendLine("<ul class='changelist'>");

                        foreach (var affected in change.AffectedParts)
                        {
                            log.Append("<li>");

                            switch (affected.What)
                            {
                                case ChangeLog.Entry.Affected.WhatAffected.Parameter:
                                    if (affected.How == ChangeLog.Entry.Affected.HowAffected.Delete)
                                        log.AppendLine("[REMOVED] parameter <code>" + affected.ParameterName + "</code> from <code>" + affected.ClassName + "/" + affected.MethodName + " </code>");
                                    else
                                        // added or modified - link exists
                                        log.Append((affected.How == ChangeLog.Entry.Affected.HowAffected.Add ? "[ADDED]" : "[CHANGED]") + " parameter <code>" + affected.ParameterName + @"</code>" + (affected.How == ChangeLog.Entry.Affected.HowAffected.Add ? "to" : "in") + @" method <code><a class=""text-default"" href='#" + affected.ClassName + "_" + affected.MethodName + "' data-target='#" + affected.ClassName + "_" + affected.MethodName + "'>" + affected.ClassName + "/" + affected.MethodName + "</a></code>");
                                    break;
                                case ChangeLog.Entry.Affected.WhatAffected.Method:
                                    if (affected.How == ChangeLog.Entry.Affected.HowAffected.Delete)
                                        log.AppendLine("[REMOVED] method <code>" + affected.ClassName + "/" + affected.MethodName + "</code>");
                                    else
                                        log.Append((affected.How == ChangeLog.Entry.Affected.HowAffected.Add ? "[ADDED]" : "[CHANGED]") + @" method <code><a class=""text-default"" href='#" + affected.ClassName + "_" + affected.MethodName + "' data-target='#" + affected.ClassName + "_" + affected.MethodName + "'>" + affected.ClassName + "/" + affected.MethodName + "</a></code>");
                                    break;
                                case ChangeLog.Entry.Affected.WhatAffected.Class:
                                    if (affected.How == ChangeLog.Entry.Affected.HowAffected.Delete)
                                        log.AppendLine("[REMOVED] class <code>" + affected.ClassName + "</code>");
                                    else
                                        log.Append((affected.How == ChangeLog.Entry.Affected.HowAffected.Add ? "[ADDED]" : "[CHANGED]") + @" class <code><a class=""text-default"" href='#" + affected.ClassName + "_header" + "' data-target='#" + affected.ClassName + "_header" + "'>" + affected.ClassName + "</a></code>");
                                    break;
                                case ChangeLog.Entry.Affected.WhatAffected.HelperClass:
                                    if (affected.How == ChangeLog.Entry.Affected.HowAffected.Delete)
                                        log.AppendLine("[REMOVED] class <code>" + affected.ClassName + "</code>");
                                    else
                                        log.Append((affected.How == ChangeLog.Entry.Affected.HowAffected.Add ? "[ADDED]" : "[CHANGED]") + @" class <code><a class=""text-default"" href='#classes_" + affected.ClassName + "' data-target='#classes_" + affected.ClassName + "'>" + affected.ClassName + "</a></code>");
                                    break;
                            }

                            log.AppendLine("</li>");
                        }

                        log.AppendLine("</ul></div>");
                    }
                    log.AppendLine("</article>");
                }
                log.AppendLine("</div></div>");
                return log.ToString();
            }

            #endregion

        }
    }
}
