using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kugar.Core.Web.JsonTemplate
{
    public delegate Task PipeActionBuilder<TModel>(JsonWriter writer, IJsonTemplateBuilderContext<TModel> context);
}