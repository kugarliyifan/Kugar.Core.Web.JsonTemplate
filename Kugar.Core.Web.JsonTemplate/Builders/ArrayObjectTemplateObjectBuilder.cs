using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Kugar.Core.ExtMethod;
using NJsonSchema;
using NJsonSchema.Annotations;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public interface IArrayBuilder<TElement> : IObjectBuilderInfo,IObjectBuilderPipe<TElement>,IDisposable
    {
        IArrayBuilder<TElement> AddProperty<TValue>(
            [Required]string propertyName,
            [Required]Func<IJsonTemplateBuilderContext<TElement>, TValue> valueFactory,
            string description = "",
            bool isNull = false,
            object example = null,
            Type? newValueType = null,
            Func<IJsonTemplateBuilderContext<TElement>, bool> ifCheckExp = null
        );

        IChildObjectBuilder<TChildModel> AddObject<TChildModel>(
            [Required]string propertyName,
            [Required]Func<IJsonTemplateBuilderContext<TElement>, TChildModel> valueFactory,
            bool isNull = false,
            string description = "",
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifNullRender = null
        );

        IArrayBuilder<TArrayNewElement> AddArrayObject<TArrayNewElement>(string propertyName,
            Func<IJsonTemplateBuilderContext<TElement>, IEnumerable<TArrayNewElement>> valueFactory,
            bool isNull = false,
            string description = ""//,
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            //Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null
        );


        IArrayBuilder<TElement> End();
    }

    internal class ArrayObjectTemplateObjectBuilder<TParentModel, TElementModel> : IArrayBuilder<TElementModel>,IObjectBuilderPipe<TElementModel>
    {
        private List<PipeActionBuilder<TElementModel>> _pipe = new List<PipeActionBuilder<TElementModel>>();
        private Func<IJsonTemplateBuilderContext<TParentModel>, IEnumerable<TElementModel>> _arrayValueFactory = null;
        private IList<PipeActionBuilder<TParentModel>> _parent = null;

        public ArrayObjectTemplateObjectBuilder(
            IObjectBuilderPipe<TParentModel> parent,
            [Required]Func<IJsonTemplateBuilderContext<TParentModel>, IEnumerable<TElementModel>> arrayValueFactory,
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver
        )
        {
            _arrayValueFactory = arrayValueFactory;
            _parent = parent.Pipe;
            this.SchemaBuilder = schemeBuilder;
            this.Generator = generator;
            this.Resolver = resolver;
        }
        



        public IArrayBuilder<TElementModel> AddProperty<TValue>(string propertyName,
            Func<IJsonTemplateBuilderContext<TElementModel>, TValue> valueFactory,
            string description = "",
            bool isNull = false,
            object example = null,
            Type? newValueType = null,
            Func<IJsonTemplateBuilderContext<TElementModel>, bool> ifCheckExp = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory != null);

            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            _pipe.Add(async (writer, context) =>
            {
                if (!(ifCheckExp?.Invoke(context) ?? true))
                {
                    return;
                }

                await writer.WritePropertyNameAsync(propertyName, context.CancellationToken);

                var value = valueFactory(context);

                if (value != null)
                {
                    await writer.WriteValueAsync(value, context.CancellationToken);
                }
                else
                {
                    await writer.WriteNullAsync(context.CancellationToken);
                }

            });

            JsonObjectType jsonType = NSwagSchemeBuilder.NetTypeToJsonObjectType(newValueType ?? typeof(TValue));


            SchemaBuilder.AddSingleProperty(propertyName, jsonType,
                description, example, isNull);

            return this;
        }

        public IChildObjectBuilder<TChildModel> AddObject<TChildModel>(string propertyName, 
            Func<IJsonTemplateBuilderContext<TElementModel>, TChildModel> valueFactory, 
            bool isNull = false,
            string description = "", 
            Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifNullRender = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory != null);

            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            //SchemaBuilder.AddObjectProperty(propertyName, description, isNull);

            _pipe.Add(async (writer, context) =>
            {
                await writer.WritePropertyNameAsync(propertyName, context.CancellationToken);
            });

            var childSchemeBuilder = SchemaBuilder.AddObjectProperty(propertyName, description, isNull);

            return (IChildObjectBuilder<TChildModel>)new ChildJsonTemplateObjectBuilder<TElementModel, TChildModel>(this,
                valueFactory,
                childSchemeBuilder,
                Generator,
                Resolver,
                true).Start();
        }

        public IArrayBuilder<TArrayNewElement> AddArrayObject<TArrayNewElement>(string propertyName, Func<IJsonTemplateBuilderContext<TElementModel>, IEnumerable<TArrayNewElement>> valueFactory, bool isNull = false,
            string description = "")
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                _pipe.Add(async (writer, model) =>
                {
                    await writer.WritePropertyNameAsync(propertyName, model.CancellationToken);
                });
            }
            else
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var s1 = SchemaBuilder.AddObjectArrayProperty(propertyName, desciption: description, nullable: isNull);

            var s = new ArrayObjectTemplateObjectBuilder<TElementModel, TArrayNewElement>(this, valueFactory, s1, Generator, Resolver);

            return s;
        }


        public IArrayBuilder<TElementModel> End()
        {
            _parent.Add(async (writer, context) =>
            {
                await writer.WriteStartArrayAsync(context.CancellationToken);

                var array = _arrayValueFactory(new JsonTemplateBuilderContext<TParentModel>(context.HttpContext,context.RootModel, context.Model,context.JsonSerializerSettings));

                if (array.HasData())
                {
                    foreach (var element in array)
                    {
                        await writer.WriteStartObjectAsync(context.CancellationToken);

                        var newContext = new JsonTemplateBuilderContext<TElementModel>(context.HttpContext, context.RootModel,element,context.JsonSerializerSettings);

                        foreach (var func in _pipe)
                        {
                            await func(writer, newContext);
                        }

                        await writer.WriteEndObjectAsync(context.CancellationToken);
                    }
                }
                
                await writer.WriteEndArrayAsync(context.CancellationToken);
            });

            return this;
        }

        public void Dispose()
        {
            this.End();
        }

        public IList<PipeActionBuilder<TElementModel>> Pipe => _pipe;

        public  NSwagSchemeBuilder SchemaBuilder { get;}

        public JsonSchemaGenerator Generator { get;}

        public JsonSchemaResolver Resolver { get;  }

        public Type ModelType { get; } = typeof(TElementModel);
    }
}