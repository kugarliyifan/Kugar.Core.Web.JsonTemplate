using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kugar.Core.Web.JsonTemplate
{
    public delegate Task PipeActionBuilder<in TModel>(JsonWriter writer, IJsonTemplateBuilderContext<TModel> context);
}