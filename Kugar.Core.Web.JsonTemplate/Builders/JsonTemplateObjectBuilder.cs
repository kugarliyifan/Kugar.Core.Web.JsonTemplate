using System;
using System.Collections.Generic;
using System.Diagnostics;
using Kugar.Core.ExtMethod;
using Kugar.Core.Log;
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

    public interface IObjectBuilder<TModel> : /*IDisposable,*/ IObjectBuilderInfo,IObjectBuilderPipe<TModel>
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

        IChildObjectBuilder<TChildModel> AddObject<TChildModel>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TChildModel> valueFactory,
            bool isNull = false,
            string description = ""//,
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifNullRender = null
        );

        IArrayBuilder<TArrayElement> AddArrayObject<TArrayElement>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TArrayElement>> valueFactory,
            bool isNull = false,
            string description = ""//,
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
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

    }


    internal class JsonTemplateObjectBuilder<TModel> : IObjectBuilder<TModel>,IObjectBuilderPipe<TModel>
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

            _pipe.Add(async (writer, context) =>
            {
                if (!(ifCheckExp?.Invoke(context)??true))
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

        /// <summary>
        /// 添加一个object属性
        /// </summary>
        /// <typeparam name="TChildModel">子对象的类型</typeparam>
        /// <param name="propertyName">新增的属性名</param>
        /// <param name="valueFactory">获取值的方法</param>
        /// <param name="isNull">是否允许为null</param>
        /// <param name="description">备注</param>
        /// <returns></returns>
        public IChildObjectBuilder<TChildModel> AddObject<TChildModel>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TChildModel> valueFactory,
            bool isNull = false,
            string description = ""
            )
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

            return (IChildObjectBuilder<TChildModel>)new ChildJsonTemplateObjectBuilder<TModel, TChildModel>(this,
                valueFactory,
                childSchemeBuilder,
                Generator,
                Resolver,
                isNewObject: true//,
                //ifCheckExp:ifCheckExp,
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
        /// <param name="ifNullRender">如果值为null,是否继续调用输出,为true时,继续调用各种参数回调,,为false时,直接输出null</param>
        /// <returns></returns>
        public IArrayBuilder<TArrayElement> AddArrayObject<TArrayElement>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TArrayElement>> valueFactory,
            bool isNull = false,
            string description = ""//,
            //Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null
            )
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

            var s = new ArrayObjectTemplateObjectBuilder<TModel, TArrayElement>(this, valueFactory, s1, Generator, Resolver);

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

            _pipe.Add(async (writer, context) =>
            {
                var data = valueFactory(context);
                
                
                await writer.WritePropertyNameAsync(propertyName, context.CancellationToken);
            
                await writer.WriteStartArrayAsync(context.CancellationToken);

                if (data.HasData())
                {
                    foreach (var value in data)
                    {
                        await writer.WriteValueAsync(value,context.CancellationToken);
                    }
                } 

                await writer.WriteEndArrayAsync(context.CancellationToken);
                
            });

            SchemaBuilder.AddValueArray(propertyName,NSwagSchemeBuilder.NetTypeToJsonObjectType(typeof(TArrayElement)), description, isNull);

            return this;
        }


        public virtual IObjectBuilder<TModel> Start()
        {
            _pipe.Add(async (writer, context) =>
            {
                await writer.WriteStartObjectAsync(context.CancellationToken);
            });

            return this;
        }

        public virtual void End()
        {
            _pipe.Add(async (writer, context) =>
            {
                await writer.WriteEndObjectAsync(context.CancellationToken);
            });
            
        }

        public virtual IList<PipeActionBuilder<TModel>> Pipe => _pipe;
        
        public NSwagSchemeBuilder SchemaBuilder { get; set; }

        public JsonSchemaGenerator Generator { get; set; }

        public JsonSchemaResolver Resolver { get; set; }
        public Type ModelType => typeof(TModel);

    }
}
