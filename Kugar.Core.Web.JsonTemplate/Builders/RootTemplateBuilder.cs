using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public class RootTemplateBuilder<TModel> : ITemplateBuilder<TModel>
    {
        public RootTemplateBuilder(
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver)
        {
            this.Generator = generator;
            this.SchemaBuilder = schemeBuilder;
            this.Resolver = resolver;

        }

        public ITemplateBuilder<TModel> Start()
        {
            Pipe.Add((writer, context) =>
            {
                writer.WriteStartObject();
            });

            return this;
        }

        public void End()
        {
            Pipe.Add((writer, context) =>
            {
                writer.WriteEndObject();
            });

        }

        public IList<PipeActionBuilder<TModel>> Pipe { get; } = new List<PipeActionBuilder<TModel>>();

        public NSwagSchemeBuilder SchemaBuilder { get; }

        public JsonSchemaGenerator Generator { get; }

        public JsonSchemaResolver Resolver { get; }

        public Type ModelType { get; } = typeof(TModel);

        public ITemplateBuilder<TModel> AddProperty<TValue>(string propertyName, Func<IJsonTemplateBuilderContext<TModel>, TValue> valueFactory, string description = "",
            bool isNull = false, object example = null, Type newValueType = null, Func<IJsonTemplateBuilderContext<TModel>, bool> ifCheckExp = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory != null);

            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            var propertyInvoke = new PropertyInvoker<TModel, TValue>()
            {
                ifCheckExp = ifCheckExp,
                ParentDisplayName = propertyName,
                PropertyName = propertyName,
                valueFactory = valueFactory
            };

            Pipe.Add(propertyInvoke.Invoke);

            JsonObjectType jsonType = NSwagSchemeBuilder.NetTypeToJsonObjectType(newValueType ?? typeof(TValue));


            SchemaBuilder.AddSingleProperty(propertyName, jsonType,
                description, example, isNull);

            return this;
        }

        public virtual ITemplateBuilder<TChildModel> AddObject<TChildModel>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TChildModel> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheck = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory != null);

            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            //SchemaBuilder.AddObjectProperty(propertyName, description, isNull);

            //_pipe.Add(async (writer, context) =>
            //{
            //    await writer.WritePropertyNameAsync(propertyName, context.CancellationToken);
            //});

            var childSchemeBuilder = SchemaBuilder.AddObjectProperty(propertyName, description, isNull);

            return new ChildJsonTemplateObjectBuilder<TModel, TChildModel>(
                propertyName,
                propertyName,
                this,
                valueFactory,
                childSchemeBuilder,
                Generator,
                Resolver,
                true, ifCheck).Start();
        }

        public virtual IArrayBuilder<TModel, TArrayNewElement> AddArrayObject<TArrayNewElement>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TArrayNewElement>> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TArrayNewElement>, bool> ifCheckExp = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                Pipe.Add((writer, model) =>
                {
                    writer.WritePropertyName(propertyName);
                });
            }
            else
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var s1 = SchemaBuilder.AddObjectArrayProperty(propertyName, desciption: description, nullable: isNull);

            var s = new ArrayObjectTemplateObjectBuilder<TModel, TArrayNewElement>(propertyName, propertyName, this, valueFactory, s1, Generator, Resolver, ifCheckExp);

            return s;
        }

        public virtual ITemplateBuilder<TModel> AddArrayValue<TValue>(string propertyName, Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TValue>> valueFactory, bool isNull = false,
            string description = "", Func<IJsonTemplateBuilderContext<IEnumerable<TValue>>, bool> ifNullRender = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            var propertyInvoke = new ArrayValueInvoker<TModel, TValue>()
            {
                ifNullRender = ifNullRender,
                ParentDisplayName = propertyName,
                PropertyName = propertyName,
                valueFactory = valueFactory
            };

            Pipe.Add(propertyInvoke.Invoke);

            JsonObjectType jsonType = NSwagSchemeBuilder.NetTypeToJsonObjectType(typeof(TValue));


            var s1 = SchemaBuilder.AddValueArray(propertyName, jsonType, desciption: description, nullable: isNull);

            return this;
        }

        public void Dispose()
        {
            this.End();
        }
    }
}
