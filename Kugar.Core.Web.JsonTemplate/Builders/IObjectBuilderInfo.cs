using System;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public interface IObjectBuilderInfo
    {

        public NSwagSchemeBuilder SchemaBuilder { get; }

        public JsonSchemaGenerator Generator { get; }

        public JsonSchemaResolver Resolver { get; }

        public Type ModelType { get; }
    }
}