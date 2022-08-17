using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kugar.Core.Web.JsonTemplate
{
    public delegate void PipeActionBuilder<TRootModel,TModel>(JsonWriter writer, IJsonTemplateBuilderContext<TRootModel, TModel> context);
}