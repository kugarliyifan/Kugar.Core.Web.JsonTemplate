using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kugar.Core.Web.JsonTemplate
{
    public delegate void PipeActionBuilder<TModel>(JsonWriter writer, IJsonTemplateBuilderContext<TModel> context);
}