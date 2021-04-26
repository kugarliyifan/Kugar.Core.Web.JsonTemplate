using System.Collections.Generic;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public interface IObjectBuilderPipe<TRootModel,TModel>
    {
        IList<PipeActionBuilder<TRootModel,TModel>> Pipe { get; }
    }
}