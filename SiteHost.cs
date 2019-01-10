using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SiteHost
{   
    
    public static class MagicOnionMiddlewareExtensions
    {
        public static IApplicationBuilder UseMagicOnionSwagger(this IApplicationBuilder app, SwaggerOptions options)
        {
            return app.UseMiddleware<HostMiddleWare>(options);
        }
        
    }
    
    public class SwaggerDocument
    {
        public readonly string swagger = "2.0";

        public Info info;

        public string host;

        public string basePath;

        public IList<string> schemes;

        public IList<string> consumes;

        public IList<string> produces;

        public IDictionary<string, PathItem> paths;

        public IDictionary<string, Schema> definitions;

        public IDictionary<string, Parameter> parameters;

        public IDictionary<string, Response> responses;

        public IDictionary<string, SecurityScheme> securityDefinitions;

        public IList<IDictionary<string, IEnumerable<string>>> security;

        public IList<Tag> tags;

        public ExternalDocs externalDocs;

        public Dictionary<string, object> vendorExtensions = new Dictionary<string, object>();
    }

    public class Info
    {
        public string version;

        public string title;

        public string description;

        public string termsOfService;

        public Contact contact;

        public License license;

        public Dictionary<string, object> vendorExtensions = new Dictionary<string, object>();
    }

    public class Contact
    {
        public string name;

        public string url;

        public string email;
    }

    public class License
    {
        public string name;

        public string url;
    }

    public class PathItem
    {
        [JsonProperty("$ref")]
        public string @ref;

        public Operation get;

        public Operation put;

        public Operation post;

        public Operation delete;

        public Operation options;

        public Operation head;

        public Operation patch;

        public IList<Parameter> parameters;

        public Dictionary<string, object> vendorExtensions = new Dictionary<string, object>();
    }

    public class Operation
    {
        public IList<string> tags;

        public string summary;

        public string description;

        public ExternalDocs externalDocs;

        public string operationId;

        public IList<string> consumes;

        public IList<string> produces;

        public IList<Parameter> parameters;

        public IDictionary<string, Response> responses;

        public IList<string> schemes;

        public bool? deprecated;

        public IList<IDictionary<string, IEnumerable<string>>> security;

        public Dictionary<string, object> vendorExtensions = new Dictionary<string, object>();
    }

    public class Tag
    {
        public string name;

        public string description;

        public ExternalDocs externalDocs;

        public Dictionary<string, object> vendorExtensions = new Dictionary<string, object>();
    }

    public class ExternalDocs
    {
        public string description;

        public string url;
    }

    public class Parameter : PartialSchema
    {
        [JsonProperty("$ref")]
        public string @ref;

        public string name;

        public string @in;

        public string description;

        public bool? required;

        public Schema schema;
    }

    public class Schema
    {
        [JsonProperty("$ref")]
        public string @ref;

        public string format;

        public string title;

        public string description;

        public object @default;

        public int? multipleOf;

        public int? maximum;

        public bool? exclusiveMaximum;

        public int? minimum;

        public bool? exclusiveMinimum;

        public int? maxLength;

        public int? minLength;

        public string pattern;

        public int? maxItems;

        public int? minItems;

        public bool? uniqueItems;

        public int? maxProperties;

        public int? minProperties;

        public IList<string> required;

        public IList<object> @enum;

        public string type;

        public Schema items;

        public IList<Schema> allOf;

        public IDictionary<string, Schema> properties;

        public Schema additionalProperties;

        public string discriminator;

        public bool? readOnly;

        public Xml xml;

        public ExternalDocs externalDocs;

        public object example;

        public Dictionary<string, object> vendorExtensions = new Dictionary<string, object>();
    }

    public class PartialSchema
    {
        public string type;

        public string format;

        public PartialSchema items;

        public string collectionFormat;

        public object @default;

        public int? maximum;

        public bool? exclusiveMaximum;

        public int? minimum;

        public bool? exclusiveMinimum;

        public int? maxLength;

        public int? minLength;

        public string pattern;

        public int? maxItems;

        public int? minItems;

        public bool? uniqueItems;

        public IList<object> @enum;

        public int? multipleOf;

        public Dictionary<string, object> vendorExtensions = new Dictionary<string, object>();
    }

    public class Response
    {
        public string description;

        public Schema schema;

        public IDictionary<string, Header> headers;

        public object examples;

        public Dictionary<string, object> vendorExtensions = new Dictionary<string, object>();
    }

    public class Header : PartialSchema
    {
        public string description;
    }

    public class Xml
    {
        public string name;

        public string @namespace;

        public string prefix;

        public bool? attribute;

        public bool? wrapped;
    }

    public class SecurityScheme
    {
        public string type;

        public string description;

        public string name;

        public string @in;

        public string flow;

        public string authorizationUrl;

        public string tokenUrl;

        public IDictionary<string, string> scopes;

        public Dictionary<string, object> vendorExtensions = new Dictionary<string, object>();
    }
    
    public class SwaggerOptions
    {
        public string ApiBasePath { get; private set; }
        public Info Info { get; set; }

        /// <summary>
        /// (FilePath, LoadedEmbeddedBytes) => CustomBytes)
        /// </summary>
        public Func<string, byte[], byte[]> ResolveCustomResource { get; set; }
        public Func<HttpContext, string> CustomHost { get; set; }
        public string XmlDocumentPath { get; set; }
        public string JsonName { get; set; }
        public string[] ForceSchemas { get; set; }

        public SwaggerOptions(string title, string description, string apiBasePath)
        {
            ApiBasePath = apiBasePath;
            JsonName = "swagger.json";
            Info = new Info { description = description, title = title };
            ForceSchemas = new string[0];
        }
    }
    
    public class HostMiddleWare
    {
        static readonly Task EmptyTask = Task.FromResult(0);

        readonly RequestDelegate next;
        
        readonly SwaggerOptions options;

        public HostMiddleWare(RequestDelegate next, SwaggerOptions options)
        {
            this.next = next;
            
            this.options = options;
        }
        
        public Task Invoke(HttpContext httpContext)
        {
            // reference embedded resouces
            const string prefix = "siteHost.Site.";
            
            var path = httpContext.Request.Path.Value.Trim('/');
            if (path == "") path = "index.html";
            
            var filePath = prefix + path.Replace("/",".");
            var mimeType = GetMimeType(filePath);

            Console.WriteLine(filePath);

            var files = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            var siteHostASM = typeof(HostMiddleWare).GetTypeInfo().Assembly;

            using (var stream = siteHostASM.GetManifestResourceStream(filePath))
            {
                if (options.ResolveCustomResource == null)
                {
                    if (stream == null)
                    {
                        // not found, standard request.
                        return next(httpContext);
                    }

                    httpContext.Response.Headers["Content-Type"] = new[] { mimeType };
                    httpContext.Response.StatusCode = 200;
                    var response = httpContext.Response.Body;
                    stream.CopyTo(response);
                    
                }
                else
                {
                    byte[] bytes;
                    if (stream == null)
                    {
                        bytes = options.ResolveCustomResource(path, null);
                    }
                    else
                    {
                        using (var ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            bytes = options.ResolveCustomResource(path, ms.ToArray());
                        }
                    }

                    if (bytes == null)
                    {
                        // not found, standard request.
                        return next(httpContext);
                    }

                    httpContext.Response.Headers["Content-Type"] = new[] { mimeType };
                    httpContext.Response.StatusCode = 200;
                    
                    var response = httpContext.Response.Body;
                    response.Write(bytes, 0, bytes.Length);
                }

            }
            
            httpContext.Response.StatusCode = 200;
            return EmptyTask;
        }

        static string GetMimeType(string path)
        {
            var extension = path.Split('.').Last();

            switch (extension)
            {
                case "css":
                    return "text/css";
                case "js":
                    return "text/javascript";
                case "json":
                    return "application/json";
                case "gif":
                    return "image/gif";
                case "png":
                    return "image/png";
                case "eot":
                    return "application/vnd.ms-fontobject";
                case "woff":
                    return "application/font-woff";
                case "woff2":
                    return "application/font-woff2";
                case "otf":
                    return "application/font-sfnt";
                case "ttf":
                    return "application/font-sfnt";
                case "svg":
                    return "image/svg+xml";
                case "ico":
                    return "image/x-icon";
                default:
                    return "text/html";
            }

        }
    }
}
