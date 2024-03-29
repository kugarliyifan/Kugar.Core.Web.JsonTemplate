﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Kugar.Core.ExtMethod;
using Kugar.Core.Log;
using Kugar.Core.Web.JsonTemplate.Helpers;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using NJsonSchema.Generation;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    //public interface IObjectBuilderWithEnd<TModel>
    //{
    //    IObjectBuilder<TModel> End();
    //}

    public interface IObjectBuilder<TModel> : IDisposable, ITemplateBuilder<TModel>
    {
        IObjectBuilder<TModel> AddProperty<TValue>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TValue> valueFactory,
            string description = "",
            bool isNull = false,
            object example = null,
            Type? newValueType = null,
            Func<IJsonTemplateBuilderContext<TModel>, bool> ifCheckExp = null
        );

        IObjectBuilder<TChildModel> AddObject<TChildModel>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TChildModel> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifNullRender = null
        );

        IArrayBuilder<TArrayElement> AddArrayObject<TArrayElement>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TArrayElement>> valueFactory,
            bool isNull = false,
            string description = "" ,
            Func<IJsonTemplateBuilderContext<TArrayElement>, bool> ifCheckExp = null //,
            //Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null
        );

        IObjectBuilder<TModel> AddArrayValue<TArrayElement>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TArrayElement>> valueFactory,
            bool isNull = false,
            string description = ""//,
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            //Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null
        );

        IObjectBuilder<TModel> Start();

        IList<PipeActionBuilder<TModel>> Pipe { get; }

        public (string propertyName, string desc) GetMemberNameWithDesc<TValue>(
            Expression<Func<TModel, TValue>> objectPropertyExp)
        {
            var desc=ExpressionHelpers.GetMemberDescription(ExpressionHelpers.GetMemberExpr(objectPropertyExp));
            var name = ExpressionHelpers.GetExpressionPropertyName(objectPropertyExp);

            return (name, desc);
        }

        public (string propertyName, string desc) GetMemberNameWithDesc<TValue>(
            Expression<Func<IJsonTemplateBuilderContext<TModel>, TValue>> objectPropertyExp)
        {
            var desc=ExpressionHelpers.GetMemberDescription(ExpressionHelpers.GetMemberExpr(objectPropertyExp));
            var name = ExpressionHelpers.GetExpressionPropertyName(objectPropertyExp);

            return (name, desc);
        }
    }

    public delegate bool IfCheckCallback<TModel>(IJsonTemplateBuilderContext<TModel> context, string propertyName);

    internal class JsonTemplateObjectBuilder<TModel> : IObjectBuilder<TModel> 
    {
        private List<PipeActionBuilder<TModel>> _pipe = new List<PipeActionBuilder<TModel>>();

        public JsonTemplateObjectBuilder(
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver
        )
        {
            this.SchemaBuilder = schemeBuilder;
            this.Generator = generator;
            this.Resolver = resolver;
        }


        /// <summary>
        /// 添加单个属性
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="propertyName">属性名</param>
        /// <param name="valueFactory">获取值的函数</param>
        /// <param name="description">属性备注</param>
        /// <param name="isNull">是否允许为空</param>
        /// <param name="example">示例</param>
        /// <param name="ifCheckExp">运行时,检查是否要输出该属性</param>
        /// <returns></returns>
        public IObjectBuilder<TModel> AddProperty<TValue>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TValue> valueFactory,
            string description = "",
            bool isNull = false,
            object example = null,
            Type? newValueType = null,
            Func<IJsonTemplateBuilderContext<TModel>, bool> ifCheckExp = null
            )
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory != null);

            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            var propertyInvoke = new PropertyInvoker<TModel, TValue>()
            {
                ifCheckExp = ifCheckExp,
                ParentDisplayName = "",
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

            //    context.PropertyName = propertyName;


            //    if (!(ifCheckExp?.Invoke(context)??true))
            //    {
            //        return;
            //    }

            //    try
            //    {
            //        writer.WritePropertyName(propertyName);

            //        var value = valueFactory(context);
                    
            //        context.Serializer.Serialize(writer,value);

            //        //if (value != null)
            //        //{
            //        //    //await w context.JsonSerializerSettings.Converters
            //        //    await writer.WriteValueAsync(value, context.CancellationToken);
            //        //}
            //        //else
            //        //{
            //        //    await writer.WriteNullAsync(context.CancellationToken);
            //        //}
            //    }
            //    catch (Exception e)
            //    {
            //        context.Logger?.Log(LogLevel.Error,e,$"输出参数错误:{propertyName}");
            //        throw;
            //    }
                
            //});

            JsonObjectType jsonType = NSwagSchemeBuilder.NetTypeToJsonObjectType(newValueType ?? typeof(TValue));

            SchemaBuilder.AddSingleProperty(propertyName, jsonType,
                description, example, isNull);

            return this;
        }
         

        ITemplateBuilder<TModel> ITemplateBuilder<TModel>.AddProperty<TValue>(string propertyName, Func<IJsonTemplateBuilderContext<TModel>, TValue> valueFactory, string description,
            bool isNull, object example, Type newValueType, Func<IJsonTemplateBuilderContext<TModel>, bool> ifCheckExp)
        {
            return AddProperty(propertyName, valueFactory, description, isNull, example, newValueType, ifCheckExp);
        }

        /// <summary>
        /// 添加一个object属性
        /// </summary>
        /// <typeparam name="TChildModel">子对象的类型</typeparam>
        /// <param name="propertyName">新增的属性名</param>
        /// <param name="valueFactory">获取值的方法</param>
        /// <param name="isNull">是否允许为null</param>
        /// <param name="description">备注</param>
        /// <returns></returns>
        public IObjectBuilder<TChildModel> AddObject<TChildModel>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TChildModel> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null 
            //Func<IJsonTemplateBuilderContext<TModel>, bool> ifCheckExp = null
            )
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);
            

            var childSchemeBuilder = SchemaBuilder.AddObjectProperty(propertyName, description, isNull);

            return (IObjectBuilder<TChildModel>)new ChildJsonTemplateObjectBuilder<TModel, TChildModel>(
                propertyName,
                 propertyName ,
                this,
                valueFactory,
                childSchemeBuilder,
                Generator,
                Resolver,
                isNewObject: true ,
                ifCheckExp:ifCheckExp
                /*ifNullRender:ifNullRender*/).Start();
        }

        /// <summary>
        /// 新增一个数组对象属性
        /// </summary>
        /// <typeparam name="TArrayElement">数组中每个对象的类型</typeparam>
        /// <param name="propertyName">新增的属性名</param>
        /// <param name="valueFactory">获取值的方法</param>
        /// <param name="isNull">是否允许为null</param>
        /// <param name="description">备注</param>
        /// <param name="ifCheckExp">对数组中每个数据项进行检查,如果返回false,则不输出该数据项</param>
        /// <returns></returns>
        public IArrayBuilder<TArrayElement> AddArrayObject<TArrayElement>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TArrayElement>> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TArrayElement>, bool> ifCheckExp = null 
            //Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null
            )
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

            var s = new ArrayObjectTemplateObjectBuilder<TModel, TArrayElement>(propertyName, propertyName, this, valueFactory, s1, Generator, Resolver,ifCheckExp:ifCheckExp);

            return s;
        }

        /// <summary>
        /// 新增一个非对象数组的属性,如int[],string[] 等
        /// </summary>
        /// <typeparam name="TArrayElement">数组中每个值的类型</typeparam>
        /// <param name="propertyName">新增的属性名</param>
        /// <param name="valueFactory">获取值的方法</param>
        /// <param name="isNull">是否允许为null</param>
        /// <param name="description">备注</param>
        /// <param name="ifNullRender">如果值为null,是否输出该属性,为true时,输出该属性,并且值为null,,为false时,不输出该属性,直接跳过</param>
        /// <returns></returns>
        public IObjectBuilder<TModel> AddArrayValue<TArrayElement>(
            string propertyName, 
            Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TArrayElement>> valueFactory, 
            bool isNull = false,
            string description = ""//, 
            //Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null
            )
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            _pipe.Add((writer, context) =>
            {
                try
                {
                    context.PropertyName = propertyName;

                    var data = valueFactory(context);


                    writer.WritePropertyName(propertyName);

                    writer.WriteStartArray();

                    if (data.HasData())
                    {
                        foreach (var value in data)
                        {
                            if (context.CancellationToken.IsCancellationRequested)
                                break;

                            context.Serializer.Serialize(writer, value);
                            //await writer.WriteValueAsync(value,context.CancellationToken);
                        }
                    }

                    writer.WriteEndArray();
                }
                catch (Exception e)
                {
                    context.Logger?.Log(LogLevel.Error, e, $"输出参数错误:{propertyName}");
                    throw;
                }


                
            });

            SchemaBuilder.AddValueArray(propertyName,NSwagSchemeBuilder.NetTypeToJsonObjectType(typeof(TArrayElement)), description, isNull);

            return this;
        }


        public virtual IObjectBuilder<TModel> Start()
        {
            _pipe.Add((writer, context) =>
            {
                writer.WriteStartObject();
            });

            return this;
        }

        public virtual void End()
        {
            _pipe.Add((writer, context) =>
            {
                writer.WriteEndObject();
            });
            
        }

        public virtual IList<PipeActionBuilder<TModel>> Pipe => _pipe;
        
        public NSwagSchemeBuilder SchemaBuilder { get; set; }

        public JsonSchemaGenerator Generator { get; set; }

        public JsonSchemaResolver Resolver { get; set; }
        public Type ModelType => typeof(TModel);

        public void Dispose()
        {
            End();
        }
    }
}
