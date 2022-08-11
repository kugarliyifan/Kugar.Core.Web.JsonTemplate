using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public class ObjectTemplateBuilder<TParentModel, TModel>:TemplateBuilderBase<TParentModel, TModel>
    {
        public ObjectTemplateBuilder(string displayPropertyName, 
            IObjectBuilderPipe<TParentModel> parent, 
            NSwagSchemeBuilder schemeBuilder, 
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver, 
            Func<IJsonTemplateBuilderContext<TModel>, bool> ifCheckExp = null
            ) : base(displayPropertyName, parent, schemeBuilder, generator, resolver, ifCheckExp)
        {
        }

        public virtual ITemplateBuilder<TModel> Start()
        {
            this.Pipe.Add((writer, context) =>
            {
                writer.WriteStartObject();
            });

            return this;
        }

        public virtual void End()
        {
            this.Pipe.Add((writer, context) =>
            {
                writer.WriteEndObject();
            });

        }
    }
}

