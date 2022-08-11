using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
    public interface IChildObjectBuilder<TParentModel, TCurrentModel> : ITemplateBuilder<TCurrentModel>,IDisposable
    {


        TemplateBuilderBase<TParentModel, TCurrentModel> Start();

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
    
    public class ChildJsonTemplateObjectBuilder<TParentModel, TCurrentModel> : TemplateBuilderBase<TParentModel, TCurrentModel> 
    {
        private bool _isNewObject = false;
        
        private Func<IJsonTemplateBuilderContext<TParentModel>, TCurrentModel> _childObjFactory;
        private string _propertyName = "";
        private Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> _ifCheckExp = null;

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
            _ifCheckExp = ifCheckExp;
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
        
        public override void Dispose()
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



}