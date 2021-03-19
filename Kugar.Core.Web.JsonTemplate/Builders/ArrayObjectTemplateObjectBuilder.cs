﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public interface IArrayBuilder<out TElement> : IDisposable
    {
        IArrayBuilder<TElement> AddProperty<TValue>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TElement>, TValue> valueFactory,
            string description = "",
            bool isNull = false,
            object example = null,
            Type? newValueType = null,
            Func<IJsonTemplateBuilderContext<TElement>, bool> ifCheckExp = null
        );

        IChildObjectBuilder<TChildModel> AddObject<TChildModel>(string propertyName,
            Func<IJsonTemplateBuilderContext<TElement>, TChildModel> valueFactory,
            bool isNull = false,
            string description = "",
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifNullRender = null
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
            Func<IJsonTemplateBuilderContext<TParentModel>, IEnumerable<TElementModel>> arrayValueFactory,
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
                true,
                ifNullRender).Start();
        }



        public IArrayBuilder<TElementModel> End()
        {
            _parent.Add(async (writer, context) =>
            {
                await writer.WriteStartArrayAsync(context.CancellationToken);

                var array = _arrayValueFactory(new JsonTemplateBuilderContext<TParentModel>(context.HttpContext, context.Model));

                foreach (var element in array)
                {
                    await writer.WriteStartObjectAsync(context.CancellationToken);

                    var newContext = new JsonTemplateBuilderContext<TElementModel>(context.HttpContext, element);

                    foreach (var func in _pipe)
                    {
                        await func(writer, newContext);
                    }

                    await writer.WriteEndObjectAsync(context.CancellationToken);
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

        protected internal NSwagSchemeBuilder SchemaBuilder { get; set; }

        protected JsonSchemaGenerator Generator { get; set; }

        protected JsonSchemaResolver Resolver { get; set; }
    }
}