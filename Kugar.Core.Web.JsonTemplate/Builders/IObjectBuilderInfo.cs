using System;
using System.Linq.Expressions;
using Kugar.Core.Web.JsonTemplate.Helpers;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public interface IObjectBuilderInfo
    {

        NSwagSchemeBuilder SchemaBuilder { get; }

        JsonSchemaGenerator Generator { get; }

        JsonSchemaResolver Resolver { get; }

        Type ModelType { get; }

        
    }
}