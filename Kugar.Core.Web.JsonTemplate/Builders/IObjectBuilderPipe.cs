using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.Xml.Linq;
using Kugar.Core.Web.JsonTemplate.Helpers;
using Newtonsoft.Json;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public interface IObjectBuilderPipe<TRootModel, TModel>
    {
        IList<PipeActionBuilder<TRootModel, TModel>> Pipe { get; }

        public void WriteProperty(JsonWriter writer, object value, IJsonTemplateBuilderContext context)
        {
            context.Serializer.Serialize(writer, value);
        }
    }
}