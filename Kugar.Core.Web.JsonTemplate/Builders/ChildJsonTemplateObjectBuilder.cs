using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.JsonTemplate.Exceptions;
using Kugar.Core.Web.JsonTemplate.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            Func<IJsonTemplateBuilderContext<TArrayElement>, bool> ifCheckExp = null,
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

        public string DisplayPropertyName { get; }

        public string PropertyName { get; }

        public (string propertyName, string desc) GetMemberNameWithDesc<TValue>(
            Expression<Func<TCurrentModel, TValue>> objectPropertyExp)
        {
            var desc=ExpressionHelpers.GetMemberDescription(ExpressionHelpers.GetMemberExpr(objectPropertyExp));
            var name = ExpressionHelpers.GetExpressionPropertyName(objectPropertyExp);

            return (name, desc);
        }
        
    }
    
    

    internal class ChildJsonTemplateObjectBuilder<TParentModel, TCurrentModel> : IChildObjectBuilder<TCurrentModel>
    {
        private List<PipeActionBuilder<TCurrentModel>> _pipe = new List<PipeActionBuilder<TCurrentModel>>();
        private bool _isNewObject = false;

        private IObjectBuilderPipe<TParentModel> _parent;

        //private IList<PipeActionBuilder<TChildModel>> _changeTypeParent = null;
        private Func<IJsonTemplateBuilderContext<TParentModel>, TCurrentModel> _childObjFactory;
        //private Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> _ifNullRender = null;
        private Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> _ifCheckExp = null;
        private string _propertyName = "";
        private string _displayPropertyName = "";

        public ChildJsonTemplateObjectBuilder(
            string propertyName,
            string displayPropertyName,
            IObjectBuilderPipe<TParentModel> parent,
            Func<IJsonTemplateBuilderContext<TParentModel>, TCurrentModel> childObjFactory,
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver,
            bool isNewObject,
            Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> ifCheckExp=null //,
            //Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> ifNullRender = null
            ) //: base(schemeBuilder, generator, resolver)
        {
            _parent = parent;
            _childObjFactory = childObjFactory;
            //_ifNullRender = ifNullRender;
            _isNewObject = isNewObject;
            _ifCheckExp = ifCheckExp;
            this.SchemaBuilder = schemeBuilder;
            this.Generator = generator;
            this.Resolver = resolver;
            _propertyName = propertyName;
            _displayPropertyName = displayPropertyName;
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

            var propertyInvoke = new PropertyInvoker<TCurrentModel, TValue>()
            {
                ifCheckExp = ifCheckExp,
                ParentDisplayName = $"{_displayPropertyName}.{propertyName}",
                PropertyName = propertyName,
                valueFactory = valueFactory
            };

            _pipe.Add(propertyInvoke.Invoke);

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

            //_pipe.Add(async (writer, context) =>
            //{
            //    await writer.WritePropertyNameAsync(propertyName, context.CancellationToken);
            //});
    
            var childSchemeBuilder = SchemaBuilder.AddObjectProperty(propertyName, description, isNull);

            return new ChildJsonTemplateObjectBuilder<TCurrentModel, TNewChildModel>(
                propertyName,
                $"{_displayPropertyName}.{propertyName}",
                this,
                valueFactory,
                childSchemeBuilder,
                Generator,
                Resolver,
                isNewObject:true,
                ifCheckExp:ifCheckExp
                
                /*, ifNullRender: ifNullRender*/).Start();
        }

        public IArrayBuilder<TArrayElement> AddArrayObject<TArrayElement>(string propertyName, Func<IJsonTemplateBuilderContext<TCurrentModel>, IEnumerable<TArrayElement>> valueFactory, bool isNull = false,
            string description = "", 
            Func<IJsonTemplateBuilderContext<TArrayElement>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                _pipe.Add((writer, model) =>
                {
                    writer.WritePropertyName(propertyName);
                });
            }
            else
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var s1 = SchemaBuilder.AddObjectArrayProperty(propertyName, desciption: description, nullable: isNull);

            var s = new ArrayObjectTemplateObjectBuilder<TCurrentModel, TArrayElement>(propertyName, $"{_displayPropertyName}.{propertyName}", this, valueFactory, s1, Generator, Resolver,ifCheckExp);

            return s;
        }

        public IChildObjectBuilder<TCurrentModel> AddArrayValue<TArrayElement>(string propertyName, Func<IJsonTemplateBuilderContext<TCurrentModel>, IEnumerable<TArrayElement>> valueFactory, bool isNull = false,
            string description = "", Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            var propertyInvoke = new ArrayValueInvoker<TCurrentModel, TArrayElement>()
            {
                ifNullRender = ifNullRender,
                ParentDisplayName =_displayPropertyName ,
                PropertyName = propertyName,
                valueFactory = valueFactory
            };

            _pipe.Add(propertyInvoke.Invoke);
              
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

            //if (_isNewObject)
            //{
            //    _pipe.Add(async (writer, context) =>
            //    {
            //        if (!(_ifCheckExp?.Invoke(context)??true))
            //        {
            //            return;
            //        }
            //        await writer.WritePropertyNameAsync(_propertyName, context.CancellationToken);
            //    });
            //}
            

            return this;
        }

        public string DisplayPropertyName => _displayPropertyName;
        public string PropertyName => _propertyName;


        public void End()
        {
            //if (_isNewObject)
            //{
            //    _pipe.Add((async (writer, context) =>
            //    {
            //        await writer.WriteEndObjectAsync(context.CancellationToken);
            //    }));
            //}
            
            _parent.Pipe.Add(invoke);
            
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

        private void invoke(JsonWriter writer, IJsonTemplateBuilderContext<TParentModel> context)
        {
            TCurrentModel value;

            try
            {
                value = _childObjFactory(context);
            }
            catch (Exception e)
            {
                throw new DataFactoryException("生成数据错误", e, context);
            }
            
            var c = (JsonTemplateBuilderContext<TParentModel>)context;


            var newContext = new JsonTemplateBuilderContext<TCurrentModel>(context.HttpContext, context.RootModel, value, context.JsonSerializerSettings, c._globalTemporaryData)
            {
                //PropertyRenderChecker = context.PropertyRenderChecker
                PropertyName = _displayPropertyName
            };

            if (!(_ifCheckExp?.Invoke(newContext) ?? true))
            {
                return;
            }

            if (_isNewObject)
            {
                writer.WritePropertyName(_propertyName);
            }



            if (value != null)
            {
                if (_isNewObject)
                {
                    writer.WriteStartObject();
                }

                foreach (var builder in _pipe)
                {
                    try
                    {
                        builder(writer, newContext);
                    }
                    catch (Exception e)
                    {
                        Debugger.Break();
                        throw;
                    }
                }

                if (_isNewObject)
                {
                    writer.WriteEndObject();
                }
            }
            else
            {
                writer.WriteNull();
            }
        }
    }

    internal struct PropertyInvoker<TCurrentModel, TNewChildModel>
    {
        public string PropertyName { set; get; }

        public Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> ifCheckExp { set; get; }

        public string ParentDisplayName { set; get; }

        public Func<IJsonTemplateBuilderContext<TCurrentModel>, TNewChildModel> valueFactory { set; get; }

        public void Invoke(JsonWriter writer, IJsonTemplateBuilderContext<TCurrentModel> context)
        {
            context.PropertyName = $"{ParentDisplayName}.{PropertyName}";

            if (context.Model==null)
            {
                Debugger.Break();
                Trace.WriteLine($"正在输出:{context.PropertyName}");
                return;
            }

            if (!(ifCheckExp?.Invoke(context) ?? true))
            {
                return;
            }

            TNewChildModel value = default;

            try
            {
                value = valueFactory(context);
            }
            catch (Exception e)
            {
                throw new DataFactoryException($"数据生成错误:{PropertyName}", e, context);
            }

            try
            {
                writer.WritePropertyName(PropertyName);
                 
                if (value != null)
                {
                    writer.WriteValue(value);
                }
                else
                {
                    writer.WriteNull();
                }
            }
            catch (Exception e)
            {
                throw new OutputRenderException(context, $"输出参数错误:{PropertyName}", e);
            }
        }
    }

    internal struct ArrayValueInvoker<TCurrentModel, TArrayElement>
    {
        public string PropertyName { set; get; }

        public Func<IJsonTemplateBuilderContext<TCurrentModel>, IEnumerable<TArrayElement>> valueFactory { set; get; }

        public Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender { set; get; }

        public string ParentDisplayName { set; get; }

        public void Invoke(JsonWriter writer, IJsonTemplateBuilderContext<TCurrentModel> context)
        {
            context.PropertyName = $"{ParentDisplayName}.{PropertyName}";

            IEnumerable<TArrayElement> data = null;

            try
            {
                data = valueFactory(context);
            }
            catch (Exception e)
            {

                throw new DataFactoryException($"数据生成错误:{ParentDisplayName}", e, context);
            }

            try
            {

                if (!data.HasData() && ifNullRender != null)
                {
                    var c = new JsonTemplateBuilderContext<IEnumerable<TArrayElement>>(context.HttpContext,
                        context.RootModel, data, context.JsonSerializerSettings);

                    if (!ifNullRender(c))
                    {
                        return;
                    }
                }

                writer.WritePropertyName(PropertyName);

                writer.WriteStartArray();

                if (data.HasData())
                {
                    foreach (var value in data)
                    {
                        context.Serializer.Serialize(writer, value);
                        //await writer.WriteValueAsync(value);
                    }
                }

                writer.WriteEndArray();
            }
            catch (Exception e)
            {
                throw new OutputRenderException(context, $"数据输出错误:{context.PropertyName}", e);
            }
        }
    }
}