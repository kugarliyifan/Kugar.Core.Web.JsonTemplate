using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.JsonTemplate.Helpers;
using Microsoft.Extensions.Options;
using NJsonSchema;
using NJsonSchema.Annotations;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public interface IArrayBuilder<TElement> : ITemplateBuilder<TElement>,IDisposable
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

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TArrayNewElement"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="valueFactory"></param>
        /// <param name="isNull"></param>
        /// <param name="description"></param>
        /// <param name="ifCheckExp">对数组中每个数据项进行检查,如果返回false,则不输出该数据项</param>
        /// <returns></returns>
        IArrayBuilder<TArrayNewElement> AddArrayObject<TArrayNewElement>(string propertyName,
            Func<IJsonTemplateBuilderContext<TElement>, IEnumerable<TArrayNewElement>> valueFactory,
            bool isNull = false,
            string description = "" ,
            Func<IJsonTemplateBuilderContext<TArrayNewElement>, bool> ifCheckExp = null
            //Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null
        );

        IArrayBuilder<TElement> AddArrayValue<TValue>(string propertyName,
            Func<IJsonTemplateBuilderContext<TElement>, IEnumerable<TValue>> valueFactory,
            bool isNull = false,
            string description = "" ,
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<IEnumerable<TValue>>, bool> ifNullRender = null
        );

        public (string propertyName, string desc) GetMemberNameWithDesc<TValue>(
            Expression<Func<TElement, TValue>> objectPropertyExp)
        {
            var desc=ExpressionHelpers.GetMemberDescription(ExpressionHelpers.GetMemberExpr(objectPropertyExp));
            var name = ExpressionHelpers.GetExpressionPropertyName(objectPropertyExp);

            return (name, desc);
        }

        IArrayBuilder<TElement> End();

        public string DisplayPropertyName { get; }


        public string PropertyName { get; }
    }

    public class ArrayObjectTemplateObjectBuilder<TParentModel, TElementModel> : IArrayBuilder<TElementModel> 
    {
        private List<PipeActionBuilder<TElementModel>> _pipe = new List<PipeActionBuilder<TElementModel>>();
        private Func<IJsonTemplateBuilderContext<TParentModel>, IEnumerable<TElementModel>> _arrayValueFactory = null;
        private IList<PipeActionBuilder<TParentModel>> _parent = null;
        private Func<IJsonTemplateBuilderContext<TElementModel>, bool> _ifCheckExp = null;
        private string _propertyName = "";
        private string _displayPropertyName = "";

        public ArrayObjectTemplateObjectBuilder(
            string properyName,
            string displayPropertyName,
            IObjectBuilderPipe<TParentModel> parent,
            [Required]Func<IJsonTemplateBuilderContext<TParentModel>, IEnumerable<TElementModel>> arrayValueFactory,
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver,
            Func<IJsonTemplateBuilderContext<TElementModel>, bool> ifCheckExp = null
        )
        {
            _arrayValueFactory = arrayValueFactory;
            _parent = parent.Pipe;
            this.SchemaBuilder = schemeBuilder;
            this.Generator = generator;
            this.Resolver = resolver;
            _ifCheckExp = ifCheckExp;
            _propertyName = properyName;
            _displayPropertyName = displayPropertyName;
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

            var propertyInvoke = new PropertyInvoker<TElementModel, TValue>()
            {
                ifCheckExp = ifCheckExp,
                ParentDisplayName = _displayPropertyName,
                PropertyName = propertyName,
                valueFactory = valueFactory
            };

            _pipe.Add(propertyInvoke.Invoke);

            //_pipe.Add((writer, context) =>
            //{
            //    //if (!context.PropertyRenderChecker(context,propertyName))
            //    //{
            //    //    return;
            //    //}

            //    try
            //    {
            //        context.PropertyName = $"{_displayPropertyName}.{propertyName}";

            //        if (!(ifCheckExp?.Invoke(context) ?? true))
            //        {
            //            return;
            //        }

            //        writer.WritePropertyName(propertyName);

            //        var value = valueFactory(context);

            //        context.Serializer.Serialize(writer, value);
            //    }
            //    catch (Exception e)
            //    {
                    
            //        throw;
            //    }
                
            //    //if (value != null)
            //    //{
            //    //    await writer.WriteValueAsync(value, context.CancellationToken);
            //    //}
            //    //else
            //    //{
            //    //    await writer.WriteNullAsync(context.CancellationToken);
            //    //}

            //});

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

            //_pipe.Add(async (writer, context) =>
            //{
            //    await writer.WritePropertyNameAsync(propertyName, context.CancellationToken);
            //});

            var childSchemeBuilder = SchemaBuilder.AddObjectProperty(propertyName, description, isNull);

            return (IChildObjectBuilder<TChildModel>)new ChildJsonTemplateObjectBuilder<TElementModel, TChildModel>(
                propertyName,
                $"{_displayPropertyName}.{propertyName}",
                this,
                valueFactory,
                childSchemeBuilder,
                Generator,
                Resolver,
                true).Start();
        }

        public IArrayBuilder<TArrayNewElement> AddArrayObject<TArrayNewElement>(
            string propertyName, 
            Func<IJsonTemplateBuilderContext<TElementModel>, IEnumerable<TArrayNewElement>> valueFactory, 
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TArrayNewElement>, bool> ifCheckExp = null)
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

            var s = new ArrayObjectTemplateObjectBuilder<TElementModel, TArrayNewElement>(propertyName, $"{_displayPropertyName}.{propertyName}", this, valueFactory, s1, Generator, Resolver,ifCheckExp);

            return s;
        }

        public IArrayBuilder<TElementModel> AddArrayValue<TValue>(string propertyName, Func<IJsonTemplateBuilderContext<TElementModel>, IEnumerable<TValue>> valueFactory, bool isNull = false,
            string description = "", Func<IJsonTemplateBuilderContext<IEnumerable<TValue>>, bool> ifNullRender = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            var propertyInvoke = new ArrayValueInvoker<TElementModel, TValue>()
            {
                ifNullRender = ifNullRender,
                ParentDisplayName = _displayPropertyName,
                PropertyName = propertyName,
                valueFactory = valueFactory
            };

            _pipe.Add(propertyInvoke.Invoke);

            JsonObjectType jsonType = NSwagSchemeBuilder.NetTypeToJsonObjectType(typeof(TValue));


            var s1 = SchemaBuilder.AddValueArray(propertyName,jsonType, desciption: description, nullable: isNull);
            
            return this;
        }


        public IArrayBuilder<TElementModel> End()
        {
            _parent.Add((writer, context) =>
            {
                var option =
                    (IOptions<JsonTemplateOption>)context.HttpContext.RequestServices.GetService(
                        typeof(IOptions<JsonTemplateOption>));



                writer.WriteStartArray();

                var array = _arrayValueFactory(new JsonTemplateBuilderContext<TParentModel>(context.HttpContext, context.RootModel, context.Model, context.JsonSerializerSettings)
                {

                    PropertyName = _propertyName
                }
                );

                if (array?.HasData()??false)
                {
                    foreach (var element in array)
                    {
                        var newContext = new JsonTemplateBuilderContext<TElementModel>(context.HttpContext, context.RootModel,element,context.JsonSerializerSettings){
                            //PropertyRenderChecker = context.PropertyRenderChecker
                            PropertyName = _propertyName
                        };

                        if (!(_ifCheckExp?.Invoke(newContext)??true))
                        {
                            continue;
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
                                Debugger.Break();
                                throw;
                            }
                            
                        }

                        writer.WriteEndObject();
                    }
                }
                
                writer.WriteEndArray();
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

        public string DisplayPropertyName => _displayPropertyName;

        public string PropertyName => _propertyName;

        public Type ModelType { get; } = typeof(TElementModel);
    }
}