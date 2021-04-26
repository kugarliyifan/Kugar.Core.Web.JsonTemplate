using System;
using System.Collections.Generic;
using System.Diagnostics;
using Kugar.Core.ExtMethod;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public interface IChildObjectBuilder<TRootModel, TCurrentModel> : IObjectBuilderInfo,IObjectBuilderPipe<TRootModel,TCurrentModel>,IDisposable
    {
        IChildObjectBuilder<TRootModel,TCurrentModel> AddProperty<TValue>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TRootModel,TCurrentModel>, TValue> valueFactory,
            string description = "",
            bool isNull = false,
            object example = null,
            Type? newValueType = null,
            Func<IJsonTemplateBuilderContext<TRootModel,TCurrentModel>, bool> ifCheckExp = null
        );

        IChildObjectBuilder<TRootModel,TNewChildModel> AddObject<TNewChildModel>(string propertyName,
            Func<IJsonTemplateBuilderContext<TRootModel,TCurrentModel>, TNewChildModel> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TRootModel,TNewChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<TRootModel,TNewChildModel>, bool> ifNullRender = null
        );

        IArrayBuilder<TRootModel,TArrayElement> AddArrayObject<TArrayElement>(string propertyName,
            Func<IJsonTemplateBuilderContext<TRootModel,TCurrentModel>, IEnumerable<TArrayElement>> valueFactory,
            bool isNull = false,
            string description = "",
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<TRootModel,IEnumerable<TArrayElement>>, bool> ifNullRender = null
        );

        IChildObjectBuilder<TRootModel,TCurrentModel>  AddArrayValue<TArrayElement>(string propertyName,
            Func<IJsonTemplateBuilderContext<TRootModel,TCurrentModel>, IEnumerable<TArrayElement>> valueFactory,
            bool isNull = false,
            string description = "",
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<TRootModel,IEnumerable<TArrayElement>>, bool> ifNullRender = null
        );

        IChildObjectBuilder<TRootModel,TCurrentModel> Start();
        
    }

    public interface IChildObjectBuilder<TModel>:IChildObjectBuilder<TModel,TModel>{}
    

    internal class ChildJsonTemplateObjectBuilder<TRootModel,TParentModel, TCurrentModel> : IChildObjectBuilder<TRootModel,TCurrentModel>
    {
        private List<PipeActionBuilder<TRootModel,TCurrentModel>> _pipe = new List<PipeActionBuilder<TRootModel,TCurrentModel>>();
        private bool _isNewObject = false;

        private IObjectBuilderPipe<TRootModel,TParentModel> _parent;

        //private IList<PipeActionBuilder<TChildModel>> _changeTypeParent = null;
        private Func<IJsonTemplateBuilderContext<TRootModel,TParentModel>, TCurrentModel> _childObjFactory;
        //private Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> _ifNullRender = null;
        //private Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> _ifCheckExp = null;

        public ChildJsonTemplateObjectBuilder(IObjectBuilderPipe<TRootModel,TParentModel> parent,
            Func<IJsonTemplateBuilderContext<TRootModel,TParentModel>, TCurrentModel> childObjFactory,
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver,
            bool isNewObject)
            //Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> ifCheckExp=null,
            //Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> ifNullRender = null) //: base(schemeBuilder, generator, resolver)
        {
            _parent = parent;
            _childObjFactory = childObjFactory;
            //_ifNullRender = ifNullRender;
            _isNewObject = isNewObject;
            //_ifCheckExp = ifCheckExp;
            this.SchemaBuilder = schemeBuilder;
            this.Generator = generator;
            this.Resolver = resolver;
        }

        public IChildObjectBuilder<TRootModel,TCurrentModel> AddProperty<TValue>(string propertyName, 
            Func<IJsonTemplateBuilderContext<TRootModel,TCurrentModel>, TValue> valueFactory, 
            string description = "",
            bool isNull = false, 
            object example = null, 
            Type? newValueType = null, 
            Func<IJsonTemplateBuilderContext<TRootModel,TCurrentModel>, bool> ifCheckExp = null)
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
                
                try
                {
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
                }
                catch (Exception e)
                {
                    context.Logger?.Log(LogLevel.Error,e,$"输出参数错误:{propertyName}");
                    throw;
                }

                

            });

            JsonObjectType jsonType = NSwagSchemeBuilder.NetTypeToJsonObjectType(newValueType ?? typeof(TValue));

            SchemaBuilder.AddSingleProperty(propertyName, jsonType,
                description, example, isNull);

            return this;
        }

        public IChildObjectBuilder<TRootModel,TNewChildModel> AddObject<TNewChildModel>(string propertyName, 
            Func<IJsonTemplateBuilderContext<TRootModel,TCurrentModel>, TNewChildModel> valueFactory, 
            bool isNull = false,
            string description = "", 
            Func<IJsonTemplateBuilderContext<TRootModel,TNewChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<TRootModel,TNewChildModel>, bool> ifNullRender = null)
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

            return new ChildJsonTemplateObjectBuilder<TRootModel,TCurrentModel, TNewChildModel>(this,
                valueFactory,
                childSchemeBuilder,
                Generator,
                Resolver,
                isNewObject:true/*, ifNullRender: ifNullRender*/).Start();
        }

        public IArrayBuilder<TRootModel,TArrayElement> AddArrayObject<TArrayElement>(string propertyName, Func<IJsonTemplateBuilderContext<TRootModel,TCurrentModel>, IEnumerable<TArrayElement>> valueFactory, bool isNull = false,
            string description = "", Func<IJsonTemplateBuilderContext<TRootModel,IEnumerable<TArrayElement>>, bool> ifNullRender = null)
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

            var s = new ArrayObjectTemplateObjectBuilder<TRootModel,TCurrentModel, TArrayElement>(this, valueFactory, s1, Generator, Resolver);

            return s;
        }

        public IChildObjectBuilder<TRootModel,TCurrentModel> AddArrayValue<TArrayElement>(string propertyName, Func<IJsonTemplateBuilderContext<TRootModel,TCurrentModel>, IEnumerable<TArrayElement>> valueFactory, bool isNull = false,
            string description = "", Func<IJsonTemplateBuilderContext<TRootModel,IEnumerable<TArrayElement>>, bool> ifNullRender = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            _pipe.Add(async (writer, context) =>
            {
                await writer.WritePropertyNameAsync(propertyName, context.CancellationToken);

                var data = valueFactory(context);
                
                await writer.WriteStartArrayAsync();

                if (data.HasData())
                {
                    foreach (var value in data)
                    {
                        await writer.WriteValueAsync(value);
                    }
                }

                await writer.WriteEndArrayAsync();
            });

            SchemaBuilder.AddValueArray(propertyName,NSwagSchemeBuilder.NetTypeToJsonObjectType(typeof(TArrayElement)), description, isNull);

            return this;
        }

        public IChildObjectBuilder<TRootModel,TCurrentModel> Start()
        {
            //if (_isNewObject)
            //{
            //    _pipe.Add((async (writer, context) =>
            //    {
            //        await writer.WriteStartObjectAsync();
            //    }));
            //}

            return this;
        }

        public void End()
        {
            //if (_isNewObject)
            //{
            //    _pipe.Add((async (writer, context) =>
            //    {
            //        await writer.WriteEndObjectAsync(context.CancellationToken);
            //    }));
            //}
            
            _parent.Pipe.Add(async (writer, context) =>
            {
                var value = _childObjFactory(context);

                if (_isNewObject)
                {
                    await writer.WriteStartObjectAsync(context.CancellationToken);
                }
                
                var c=(JsonTemplateBuilderContext<TRootModel,TParentModel>)context;

                var newContext = new JsonTemplateBuilderContext<TRootModel,TCurrentModel>(context.HttpContext,context.RootModel, value,context.JsonSerializerSettings,c._globalTemporaryData);

                foreach (var builder in _pipe)
                {
                    await builder(writer, newContext);
                }

                if (_isNewObject)
                {
                    await writer.WriteEndObjectAsync(context.CancellationToken);
                }
            });
            
        }

        public NSwagSchemeBuilder SchemaBuilder { get; }
        public JsonSchemaGenerator Generator { get; }
        public JsonSchemaResolver Resolver { get; }
        public Type ModelType => typeof(TCurrentModel);
        public IList<PipeActionBuilder<TRootModel,TCurrentModel>> Pipe => _pipe;
        public void Dispose()
        {
            End();
        }
    }
}