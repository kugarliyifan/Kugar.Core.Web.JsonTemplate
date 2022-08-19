using System;
using System.Collections.Generic;
using Kugar.Core.Web.JsonTemplate.Builders;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate.Compatibility
{
    public class IChildObjectBuilder<TModel> : SameRootTemplateBuilder<TModel>
    {
        public IChildObjectBuilder(string propertyName, string displayPropertyName, IObjectBuilderPipe<TModel, TModel> parent, NSwagSchemeBuilder schemeBuilder, JsonSchemaGenerator generator, JsonSchemaResolver resolver, bool isNewObject, Func<IJsonTemplateBuilderContext<TModel, TModel>, bool> ifCheckExp = null) : base(propertyName, displayPropertyName, parent, schemeBuilder, generator, resolver, isNewObject, ifCheckExp)
        {
        }
    }

    public class IArrayBuilder<TModel> :SameRootArrayBuilder<TModel>, ISameRootArrayBuilder<TModel>
    {
        public IArrayBuilder(string properyName, string displayPropertyName, IObjectBuilderPipe<IEnumerable<TModel>, IEnumerable<TModel>> parent, NSwagSchemeBuilder schemeBuilder, JsonSchemaGenerator generator, JsonSchemaResolver resolver, Func<IJsonTemplateBuilderContext<IEnumerable<TModel>, TModel>, bool> ifCheckExp = null) : base(properyName, displayPropertyName, parent, schemeBuilder, generator, resolver, ifCheckExp)
        {
        }
    }
}
