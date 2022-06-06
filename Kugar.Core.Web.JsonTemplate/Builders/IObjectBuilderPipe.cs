using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public interface IObjectBuilderPipe<TModel>
    {
        IList<PipeActionBuilder<TModel>> Pipe { get; }

        public void WriteProperty(JsonWriter writer,object value, IJsonTemplateBuilderContext context)
        {
            context.Serializer.Serialize(writer,value);
        }
    }


    public interface ITemplateBuilder<TModel> : IObjectBuilderPipe<TModel>, IObjectBuilderInfo{}

    public class TemplateBuilderBase<TModel> : ITemplateBuilder<TModel>
    {
        public IList<PipeActionBuilder<TModel>> Pipe { get; }
        public NSwagSchemeBuilder SchemaBuilder { get; }
        public JsonSchemaGenerator Generator { get; }
        public JsonSchemaResolver Resolver { get; }
        public Type ModelType { get; }
    }
}