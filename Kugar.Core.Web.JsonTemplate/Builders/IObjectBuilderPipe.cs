using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
}