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
    public interface IChildObjectBuilder<TCurrentModel> : IObjectBuilderInfo,IObjectBuilderPipe<TCurrentModel>,IDisposable
    {
        IChildObjectBuilder<TCurrentModel> AddProperty<TValue>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TCurrentModel>, TValue> valueFactory,
            string description = "",
            bool isNull = false,
            object example = null,
            Type? newValueType = null,
            Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> ifCheckExp = null
        );

        IChildObjectBuilder<TNewChildModel> AddObject<TNewChildModel>(string propertyName,
            Func<IJsonTemplateBuilderContext<TCurrentModel>, TNewChildModel> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TNewChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<TNewChildModel>, bool> ifNullRender = null
        );

        IArrayBuilder<TArrayElement> AddArrayObject<TArrayElement>(string propertyName,
            Func<IJsonTemplateBuilderContext<TCurrentModel>, IEnumerable<TArrayElement>> valueFactory,
            bool isNull = false,
            string description = "",
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null
        );

        IChildObjectBuilder<TCurrentModel>  AddArrayValue<TArrayElement>(string propertyName,
            Func<IJsonTemplateBuilderContext<TCurrentModel>, IEnumerable<TArrayElement>> valueFactory,
            bool isNull = false,
            string description = "",
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null
        );

        IChildObjectBuilder<TCurrentModel> Start();
        
    }
    
    

    internal class ChildJsonTemplateObjectBuilder<TParentModel, TCurrentModel> : IChildObjectBuilder<TCurrentModel>
    {
        private List<PipeActionBuilder<TCurrentModel>> _pipe = new List<PipeActionBuilder<TCurrentModel>>();
        private bool _isNewObject = false;

        private IObjectBuilderPipe<TParentModel> _parent;

        //private IList<PipeActionBuilder<TChildModel>> _changeTypeParent = null;
        private Func<IJsonTemplateBuilderContext<TParentModel>, TCurrentModel> _childObjFactory;
        //private Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> _ifNullRender = null;
        //private Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> _ifCheckExp = null;

        public ChildJsonTemplateObjectBuilder(IObjectBuilderPipe<TParentModel> parent,
            Func<IJsonTemplateBuilderContext<TParentModel>, TCurrentModel> childObjFactory,
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

        public IChildObjectBuilder<TCurrentModel> AddProperty<TValue>(string propertyName, 
            Func<IJsonTemplateBuilderContext<TCurrentModel>, TValue> valueFactory, 
            string description = "",
            bool isNull = false, 
            object example = null, 
            Type? newValueType = null, 
            Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> ifCheckExp = null)
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

        public IChildObjectBuilder<TNewChildModel> AddObject<TNewChildModel>(string propertyName, 
            Func<IJsonTemplateBuilderContext<TCurrentModel>, TNewChildModel> valueFactory, 
            bool isNull = false,
            string description = "", 
            Func<IJsonTemplateBuilderContext<TNewChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<TNewChildModel>, bool> ifNullRender = null)
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

            return new ChildJsonTemplateObjectBuilder<TCurrentModel, TNewChildModel>(this,
                valueFactory,
                childSchemeBuilder,
                Generator,
                Resolver,
                isNewObject:true/*, ifNullRender: ifNullRender*/).Start();
        }

        public IArrayBuilder<TArrayElement> AddArrayObject<TArrayElement>(string propertyName, Func<IJsonTemplateBuilderContext<TCurrentModel>, IEnumerable<TArrayElement>> valueFactory, bool isNull = false,
            string description = "", Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null)
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

            var s = new ArrayObjectTemplateObjectBuilder<TCurrentModel, TArrayElement>(this, valueFactory, s1, Generator, Resolver);

            return s;
        }

        public IChildObjectBuilder<TCurrentModel> AddArrayValue<TArrayElement>(string propertyName, Func<IJsonTemplateBuilderContext<TCurrentModel>, IEnumerable<TArrayElement>> valueFactory, bool isNull = false,
            string description = "", Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null)
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

        public IChildObjectBuilder<TCurrentModel> Start()
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
                
                var c=(JsonTemplateBuilderContext<TParentModel>)context;

                var newContext = new JsonTemplateBuilderContext<TCurrentModel>(context.HttpContext,context.RootModel, value,context.JsonSerializerSettings,c._globalTemporaryData)
                {
                    PropertyRenderChecker = context.PropertyRenderChecker
                };

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
        public IList<PipeActionBuilder<TCurrentModel>> Pipe => _pipe;
        public void Dispose()
        {
            End();
        }
    }
}