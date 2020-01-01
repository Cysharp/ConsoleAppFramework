// This definition is borrowed from Swashbuckle.
#nullable disable annotations
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MicroBatchFramework.WebHosting.Swagger.Schemas
{
    public class SwaggerDocument
    {
        public string swagger { get; set; } = "2.0";

        public Info info { get; set; }

        public string host { get; set; }

        public string basePath { get; set; }

        public IList<string> schemes { get; set; }

        public IList<string> consumes { get; set; }

        public IList<string> produces { get; set; }

        public IDictionary<string, PathItem> paths { get; set; }

        public IDictionary<string, Schema> definitions { get; set; }

        public IDictionary<string, Parameter> parameters { get; set; }

        public IDictionary<string, Response> responses { get; set; }

        public IDictionary<string, SecurityScheme> securityDefinitions { get; set; }

        public IList<IDictionary<string, IEnumerable<string>>> security { get; set; }

        public IList<Tag> tags { get; set; }

        public ExternalDocs externalDocs { get; set; }

        // public Dictionary<string, object> vendorExtensions { get; set; } = new Dictionary<string, object>();
    }

    public class Info
    {
        public string version { get; set; }

        public string title { get; set; }

        public string description { get; set; }

        public string termsOfService { get; set; }

        public Contact contact { get; set; }

        public License license { get; set; }

        // public Dictionary<string, object> vendorExtensions { get; set; } = new Dictionary<string, object>();
    }

    public class Contact
    {
        public string name { get; set; }

        public string url { get; set; }

        public string email { get; set; }
    }

    public class License
    {
        public string name { get; set; }

        public string url { get; set; }
    }

    public class PathItem
    {
        [JsonPropertyName("$ref")]
        public string @ref { get; set; }

        public Operation get { get; set; }

        public Operation put { get; set; }

        public Operation post { get; set; }

        public Operation delete { get; set; }

        public Operation options { get; set; }

        public Operation head { get; set; }

        public Operation patch { get; set; }

        public IList<Parameter> parameters { get; set; }

        // public Dictionary<string, object> vendorExtensions { get; set; } = new Dictionary<string, object>();
    }

    public class Operation
    {
        public IList<string> tags { get; set; }

        public string summary { get; set; }

        public string description { get; set; }

        public ExternalDocs externalDocs { get; set; }

        public string operationId { get; set; }

        public IList<string> consumes { get; set; }

        public IList<string> produces { get; set; }

        public IList<Parameter> parameters { get; set; }

        public IDictionary<string, Response> responses { get; set; }

        public IList<string> schemes { get; set; }

        public bool? deprecated { get; set; }

        public IList<IDictionary<string, IEnumerable<string>>> security { get; set; }

        // public Dictionary<string, object> vendorExtensions { get; set; } = new Dictionary<string, object>();
    }

    public class Tag
    {
        public string name { get; set; }

        public string description { get; set; }

        public ExternalDocs externalDocs { get; set; }

        // public Dictionary<string, object> vendorExtensions { get; set; } = new Dictionary<string, object>();
    }

    public class ExternalDocs
    {
        public string description { get; set; }

        public string url { get; set; }
    }

    public class Parameter : PartialSchema
    {
        [JsonPropertyName("$ref")]
        public string @ref { get; set; }

        public string name { get; set; }

        public string @in { get; set; }

        public string description { get; set; }

        public bool? required { get; set; }

        public Schema schema { get; set; }
    }

    public class Schema
    {
        [JsonPropertyName("$ref")]
        public string @ref { get; set; }

        public string format { get; set; }

        public string title { get; set; }

        public string description { get; set; }

        public object @default { get; set; }

        public int? multipleOf { get; set; }

        public int? maximum { get; set; }

        public bool? exclusiveMaximum { get; set; }

        public int? minimum { get; set; }

        public bool? exclusiveMinimum { get; set; }

        public int? maxLength { get; set; }

        public int? minLength { get; set; }

        public string pattern { get; set; }

        public int? maxItems { get; set; }

        public int? minItems { get; set; }

        public bool? uniqueItems { get; set; }

        public int? maxProperties { get; set; }

        public int? minProperties { get; set; }

        public IList<string> required { get; set; }

        public IList<object> @enum { get; set; }

        public string type { get; set; }

        public Schema items { get; set; }

        public IList<Schema> allOf { get; set; }

        public IDictionary<string, Schema> properties { get; set; }

        public Schema additionalProperties { get; set; }

        public string discriminator { get; set; }

        public bool? readOnly { get; set; }

        public Xml xml { get; set; }

        public ExternalDocs externalDocs { get; set; }

        public object example { get; set; }

        // public Dictionary<string, object> vendorExtensions { get; set; } = new Dictionary<string, object>();
    }

    public class PartialSchema
    {
        public string type { get; set; }

        public string format { get; set; }

        public PartialSchema items { get; set; }

        public string collectionFormat { get; set; }

        public object @default { get; set; }

        public int? maximum { get; set; }

        public bool? exclusiveMaximum { get; set; }

        public int? minimum { get; set; }

        public bool? exclusiveMinimum { get; set; }

        public int? maxLength { get; set; }

        public int? minLength { get; set; }

        public string pattern { get; set; }

        public int? maxItems { get; set; }

        public int? minItems { get; set; }

        public bool? uniqueItems { get; set; }

        public IList<object> @enum { get; set; }

        public int? multipleOf { get; set; }

        // public Dictionary<string, object> vendorExtensions { get; set; } = new Dictionary<string, object>();
    }

    public class Response
    {
        public string description { get; set; }

        public Schema schema { get; set; }

        public IDictionary<string, Header> headers { get; set; }

        public object examples { get; set; }

        // public Dictionary<string, object> vendorExtensions { get; set; } = new Dictionary<string, object>();
    }

    public class Header : PartialSchema
    {
        public string description { get; set; }
    }

    public class Xml
    {
        public string name { get; set; }

        public string @namespace { get; set; }

        public string prefix { get; set; }

        public bool? attribute { get; set; }

        public bool? wrapped { get; set; }
    }

    public class SecurityScheme
    {
        public string type { get; set; }

        public string description { get; set; }

        public string name { get; set; }

        public string @in { get; set; }

        public string flow { get; set; }

        public string authorizationUrl { get; set; }

        public string tokenUrl { get; set; }

        public IDictionary<string, string> scopes { get; set; }

        // public Dictionary<string, object> vendorExtensions { get; set; } = new Dictionary<string, object>();
    }
}