using System.Collections.Generic;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public interface IObjectBuilderPipe<TModel>
    {
        IList<PipeActionBuilder<TModel>> Pipe { get; }
    }
}