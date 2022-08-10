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
    public interface IChildObjectBuilder<TCurrentModel> : ITemplateBuilder<TCurrentModel>,IDisposable
    {
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
            Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifCheckExp = null
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
    
    public class ChildJsonTemplateObjectBuilder<TParentModel, TCurrentModel> : TemplateBuilderBase<TParentModel,TCurrentModel>,IDisposable
    {
        private bool _isNewObject = false;
        
        private Func<IJsonTemplateBuilderContext<TParentModel>, TCurrentModel> _childObjFactory;
        private string _propertyName = "";

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
            ) : base(displayPropertyName,parent,schemeBuilder, generator, resolver, ifCheckExp)
        {
            _childObjFactory = childObjFactory;
            _isNewObject = isNewObject;
            _propertyName = propertyName;
        }
        
        public ITemplateBuilder<TCurrentModel> Start()
        {
            return this;
        }
        
        public string PropertyName => _propertyName;
        
        public void End()
        {
            Parent.Pipe.Add(invoke);
        }
        
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
                PropertyName = DisplayPropertyName
            };

            if (!(IfCheckExp?.Invoke(newContext) ?? true))
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

                base.InvokePipe(writer,newContext);
                
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

    public struct PropertyInvoker<TCurrentModel, TNewChildModel>
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

    public struct ArrayValueInvoker<TCurrentModel, TArrayElement>
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