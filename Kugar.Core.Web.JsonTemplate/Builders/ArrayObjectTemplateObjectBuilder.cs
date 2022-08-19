using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using Kugar.Core.ExtMethod;
using Kugar.Core.Log;
using Kugar.Core.Web.JsonTemplate.Helpers;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NJsonSchema;
using NJsonSchema.Annotations;
using NJsonSchema.Generation;
using YamlDotNet.Core.Tokens;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public interface IRootArrayBuilder<TParentModel, TElement> :IArrayBuilder<TParentModel, TParentModel, TElement>{}

    public interface IArrayBuilder<TRootModel, TParentModel, TElement> : ITemplateBuilder<TRootModel, TElement>,IDisposable
    {
        public (string propertyName, string desc) GetMemberNameWithDesc<TValue>(
            Expression<Func<TElement, TValue>> objectPropertyExp)
        {
            var desc=ExpressionHelpers.GetMemberDescription(ExpressionHelpers.GetMemberExpr(objectPropertyExp));
            var name = ExpressionHelpers.GetExpressionPropertyName(objectPropertyExp);

            return (name, desc);
        }

        IArrayBuilder<TRootModel, TParentModel, TElement> End();
        
    }

    public class ArrayObjectTemplateObjectBuilder<TRootModel, TParentModel, TElementModel> : 
        TemplateBuilderBase<TRootModel, TParentModel,TElementModel>, IArrayBuilder<TRootModel, TParentModel, TElementModel> 
    {
        private List<PipeActionBuilder<TRootModel, TElementModel>> _pipe = new List<PipeActionBuilder<TRootModel, TElementModel>>();
        private Func<IJsonTemplateBuilderContext<TRootModel, TParentModel>, IEnumerable<TElementModel>> _arrayValueFactory = null;
        private IList<PipeActionBuilder<TRootModel, TParentModel>> _parent = null;
        private Func<IJsonTemplateBuilderContext<TRootModel, TElementModel>, bool> _ifCheckExp = null;
        private string _propertyName = "";
        private string _displayPropertyName = "";

        public ArrayObjectTemplateObjectBuilder(
            string properyName,
            string displayPropertyName,
            IObjectBuilderPipe<TRootModel, TParentModel> parent,
            [Required]Func<IJsonTemplateBuilderContext<TRootModel, TParentModel>, IEnumerable<TElementModel>> arrayValueFactory,
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver,
            Func<IJsonTemplateBuilderContext<TRootModel, TElementModel>, bool> ifCheckExp = null
        ) : base(properyName,parent, schemeBuilder, generator, resolver, ifCheckExp)
        {
            _arrayValueFactory = arrayValueFactory;
            _parent = parent.Pipe;
            _ifCheckExp = ifCheckExp;
            _propertyName = properyName;
            _displayPropertyName = displayPropertyName;
        }

        public override IList<PipeActionBuilder<TRootModel, TElementModel>> Pipe => _pipe;

        public virtual IArrayBuilder<TRootModel, TParentModel, TElementModel> End()
        {
            _parent.Add((writer, context) =>
            {
                //var option =
                //    (IOptions<JsonTemplateOption>)context.HttpContext.RequestServices.GetService(
                //        typeof(IOptions<JsonTemplateOption>));


                if (GlobalSettings.IsRenderTrace)
                {
                    Debug.WriteLine($"{this.GetType().Name}|Property:{context.PropertyName}=开始创建数组", "JsonTemplate");
                }

                writer.WriteStartArray();

                var array = _arrayValueFactory(new JsonTemplateBuilderContext<TRootModel, TParentModel>(context.HttpContext, (TRootModel)context.RootModel, context.Model, context.JsonSerializerSettings)
                {

                    PropertyName = _propertyName
                }
                );

                if (array?.HasData()??false)
                {
                    var index = 0;

                    try
                    {
                        foreach (var element in array)
                        {
                            var newContext = new JsonTemplateBuilderContext<TRootModel, TElementModel>(context.HttpContext, context.RootModel, element, context.JsonSerializerSettings)
                            {
                                //PropertyRenderChecker = context.PropertyRenderChecker
                                PropertyName = _propertyName
                            };

                            if (!(_ifCheckExp?.Invoke(newContext) ?? true))
                            {
                                continue;
                            }

                            if (GlobalSettings.IsRenderTrace)
                            {
                                Debug.WriteLine($"{this.GetType().Name}|Property:{context.PropertyName}=输出属性值|{_propertyName}:index={index}", "JsonTemplate");
                            }

                            writer.WriteStartObject();

                            foreach (var func in _pipe)
                            {
                                try
                                {
                                    func(writer, newContext);
                                }
                                catch (Exception e)
                                {
                                    Debug.WriteLine($"{this.GetType().Name}执行输出错误:{JsonConvert.SerializeObject(e)}", "JsonTemplate");
                                    LoggerManager.Default.Error($"JsonTemplate:{this.GetType().Name}执行输出错误", e);
                                    throw;
                                }

                            }

                            writer.WriteEndObject();

                            index++;
                        }
                    }
                    catch (Exception e)
                    {
                        LoggerManager.Default.Error($"{this.GetType().Name}|{_propertyName}执行输出错误:index={index}",e);
                        throw;
                    }

                    
                }

                if (GlobalSettings.IsRenderTrace)
                {
                    Debug.WriteLine($"{this.GetType().Name}|Property:{context.PropertyName}=结束创建数组", "JsonTemplate");
                }

                writer.WriteEndArray();
            });

            return this;
        }

        public override void Dispose()
        {
            this.End();
        }
    }
}