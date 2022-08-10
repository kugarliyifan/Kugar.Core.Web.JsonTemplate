using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public interface IObjectBuilderPipe<TModel>
    {
        IList<PipeActionBuilder<TModel>> Pipe { get; }

        public void WriteProperty(JsonWriter writer, object value, IJsonTemplateBuilderContext context)
        {
            context.Serializer.Serialize(writer, value);
        }
    }


    public interface ITemplateBuilder<TModel> : IObjectBuilderPipe<TModel>, IObjectBuilderInfo
    {
        ITemplateBuilder<TModel> AddProperty<TValue>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TValue> valueFactory,
            string description = "",
            bool isNull = false,
            object example = null,
            Type newValueType = null,
            Func<IJsonTemplateBuilderContext<TModel>, bool> ifCheckExp = null);

        IObjectBuilder<TChildModel> AddObject<TChildModel>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TChildModel> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheck = null);

    }

    public abstract class TemplateBuilderBase<TParentModel, TModel> : ITemplateBuilder<TModel>
    {
        protected TemplateBuilderBase(
            string displayPropertyName,
            IObjectBuilderPipe<TParentModel> parent,
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver,
            Func<IJsonTemplateBuilderContext<TModel>, bool> ifCheckExp = null
        )
        {
            DisplayPropertyName = displayPropertyName;

            this.SchemaBuilder = schemeBuilder;

            this.Generator = generator;

            this.Resolver = resolver;

            this.Parent = parent;

            ModelType = typeof(TModel);

            IfCheckExp = ifCheckExp;
        }

        protected virtual string DisplayPropertyName { get; }

        public virtual ITemplateBuilder<TModel> AddProperty<TValue>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TValue> valueFactory,
            string description = "",
            bool isNull = false,
            object example = null,
            Type? newValueType = null,
            Func<IJsonTemplateBuilderContext<TModel>, bool> ifCheckExp = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory != null);

            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            var propertyInvoke = new PropertyInvoker<TModel, TValue>()
            {
                ifCheckExp = ifCheckExp,
                ParentDisplayName = DisplayPropertyName,
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
                $"{DisplayPropertyName}.{propertyName}",
                this,
                valueFactory,
                childSchemeBuilder,
                Generator,
                Resolver,
                true, ifCheck).Start();
        }

        public virtual IArrayBuilder<TArrayNewElement> AddArrayObject<TArrayNewElement>(
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

            var s = new ArrayObjectTemplateObjectBuilder<TModel, TArrayNewElement>(propertyName, $"{DisplayPropertyName}.{propertyName}", this, valueFactory, s1, Generator, Resolver, ifCheckExp);

            return s;
        }

        public virtual ITemplateBuilder<TModel> AddArrayValue<TValue>(string propertyName, Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TValue>> valueFactory, bool isNull = false,
            string description = "", Func<IJsonTemplateBuilderContext<IEnumerable<TValue>>, bool> ifNullRender = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            var propertyInvoke = new ArrayValueInvoker<TModel, TValue>()
            {
                ifNullRender = ifNullRender,
                ParentDisplayName = DisplayPropertyName,
                PropertyName = propertyName,
                valueFactory = valueFactory
            };

            Pipe.Add(propertyInvoke.Invoke);

            JsonObjectType jsonType = NSwagSchemeBuilder.NetTypeToJsonObjectType(typeof(TValue));


            var s1 = SchemaBuilder.AddValueArray(propertyName, jsonType, desciption: description, nullable: isNull);

            return this;
        }

        protected virtual void InvokePipe(JsonWriter writer, JsonTemplateBuilderContext<TModel> context)
        {
            foreach (var func in this.Pipe)
            {
                try
                {
                    func(writer, context);
                }
                catch (Exception e)
                {
                    Debugger.Break();
                    throw;
                }

            }
        }

        protected virtual IJsonTemplateBuilderContext<T> CreateBuilderContext<T>(
            string propertyName,
            T model,
            IJsonTemplateBuilderContext<TModel> parentContext
            )
        {
            var newContext = new JsonTemplateBuilderContext<T>
                (parentContext.HttpContext,
                    parentContext.RootModel,
                    model,
                    parentContext.JsonSerializerSettings)
            {
                //PropertyRenderChecker = context.PropertyRenderChecker
                PropertyName = propertyName,
                _globalTemporaryData = new Lazy<TemplateData>(parentContext.GlobalTemporaryData)
            };

            return newContext;
        }

        public virtual IList<PipeActionBuilder<TModel>> Pipe { get; } = new List<PipeActionBuilder<TModel>>();

        public virtual NSwagSchemeBuilder SchemaBuilder { get; }
        public virtual JsonSchemaGenerator Generator { get; }
        public virtual JsonSchemaResolver Resolver { get; }
        public virtual Type ModelType { get; }

        public virtual IObjectBuilderPipe<TParentModel> Parent { set; get; }

        public virtual Func<IJsonTemplateBuilderContext<TModel>, bool> IfCheckExp { get; }
    }
}