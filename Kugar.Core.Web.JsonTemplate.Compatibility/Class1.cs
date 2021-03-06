using System;
using Kugar.Core.Web.JsonTemplate.Builders;

namespace Kugar.Core.Web.JsonTemplate.Compatibility
{
    public abstract class StaticJsonBuilder<TModel>:JsonTemplateBase<TModel>
    {
        public override void BuildScheme(IObjectBuilder<TModel> builder)
        {
            throw new NotImplementedException();
        }

        public abstract void BuildSchema();
    }
}
