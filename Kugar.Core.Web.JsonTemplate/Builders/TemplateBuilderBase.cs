using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Kugar.Core.ExtMethod;
using Kugar.Core.Log;
using Kugar.Core.Web.JsonTemplate.Exceptions;
using Kugar.Core.Web.JsonTemplate.Invokers;
using Newtonsoft.Json;
using NJsonSchema;
using NJsonSchema.Generation;
using YamlDotNet.Core.Tokens;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public interface ITemplateBuilder<TRootModel, TModel> : IObjectBuilderPipe<TRootModel, TModel>, IObjectBuilderInfo, IDisposable
    {
        ITemplateBuilder<TRootModel, TModel> AddProperty<TValue>(string propertyName,
            Func<IJsonTemplateBuilderContext<TRootModel, TModel>, TValue> valueFactory,
            string description = "",
            bool? isNull = null,
            object example = null,
            Type newValueType = null,
            Func<IJsonTemplateBuilderContext<TRootModel, TModel>, bool> ifCheckExp = null,bool isRawValue=false,JsonObjectType? jsonType=null);

        ITemplateBuilder<TRootModel, TChildModel> AddObject<TChildModel>(string propertyName,
            Func<IJsonTemplateBuilderContext<TRootModel, TModel>, TChildModel> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TRootModel, TChildModel>, bool> ifCheck = null);

        ITemplateBuilder<TRootModel, TModel> AddArrayValue<TValue>(string propertyName,
            Func<IJsonTemplateBuilderContext<TRootModel, TModel>, IEnumerable<TValue>> valueFactory, bool isNull = false,
            string description = "", Func<IJsonTemplateBuilderContext<TRootModel, IEnumerable<TValue>>, bool> ifCheckExp = null);

        IArrayBuilder<TRootModel, TModel, TArrayNewElement> AddArrayObject<TArrayNewElement>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TRootModel, TModel>, IEnumerable<TArrayNewElement>> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TRootModel, TArrayNewElement>, bool> ifCheckExp = null);

    }

    public abstract class TemplateBuilderBase<TRootModel, TParentModel, TModel> : ITemplateBuilder<TRootModel, TModel>
    {
        protected TemplateBuilderBase(
            string displayPropertyName,
            IObjectBuilderPipe<TRootModel, TParentModel> parent,
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver,
            Func<IJsonTemplateBuilderContext<TRootModel, TModel>, bool> ifCheckExp = null
        )
        {
            DisplayPropertyName = displayPropertyName;

            SchemaBuilder = schemeBuilder;

            Generator = generator;

            Resolver = resolver;

            Parent = parent;

            ModelType = typeof(TModel);

            IfCheckExp = ifCheckExp;
        }

        protected virtual string DisplayPropertyName { get; }

        /// <summary>
        /// 添加一个单值类型或值类型数组的属性,
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="valueFactory"></param>
        /// <param name="description"></param>
        /// <param name="isNull"></param>
        /// <param name="example"></param>
        /// <param name="newValueType"></param>
        /// <param name="ifCheckExp"></param>
        /// <param name="isRawValue"></param>
        /// <param name="jsonType"></param>
        /// <returns></returns>
        /// <exception cref="DataFactoryException"></exception>
        public virtual ITemplateBuilder<TRootModel, TModel> AddProperty<TValue>(string propertyName,
            Func<IJsonTemplateBuilderContext<TRootModel, TModel>, TValue> valueFactory,
            string description = "",
            bool? isNull = null,
            object example = null,
            Type newValueType = null,
            Func<IJsonTemplateBuilderContext<TRootModel, TModel>, bool> ifCheckExp = null,bool isRawValue=false,JsonObjectType? jsonType=null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory != null);

            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            newValueType ??= typeof(TValue);

            var propertyInvoke = new PropertyInvoker<TRootModel, TModel, TValue>()
            {
                BuilderTemplate = this.GetType(),
                ifCheckExp = ifCheckExp,
                ParentDisplayName = DisplayPropertyName,
                PropertyName = propertyName,
                valueFactory = valueFactory,
                IsRawRender = isRawValue
            };

            Pipe.Add(propertyInvoke.Invoke);

            if (jsonType==null)
            {
                jsonType =typeof(TValue)==typeof(string) && isRawValue?JsonObjectType.Object:NSwagSchemeBuilder.NetTypeToJsonObjectType(newValueType);    
            }

            if (typeof(TValue).IsIEnumerable())
            {
                var type = typeof(TValue);

                if (type.IsGenericType)
                {
                    var itemType = type.GenericTypeArguments[0];

                    if (!itemType.IsPrimitive &&  itemType!=typeof(string))
                    {
                        throw new DataFactoryException($"由于使用了AddProperty函数,因此{propertyName}返回的值必须是值类型或值类型数组,不允许是对象数组");
                    }
                }
                

                
            }

            if (!isNull.HasValue)
            {
                if ( (newValueType.IsGenericType && 
                    newValueType.GetGenericTypeDefinition() == typeof(Nullable<>)) 
                    )
                {
                    isNull = true;
                }
                else
                {
                    isNull = false;
                }

            }

            SchemaBuilder.AddSingleProperty(propertyName, jsonType.Value,
                description, example, isNull??false);

            return this;
        }

        /// <summary>
        /// 添加一个新的对象
        /// </summary>
        /// <typeparam name="TChildModel"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="valueFactory"></param>
        /// <param name="isNull"></param>
        /// <param name="description"></param>
        /// <param name="ifCheck"></param>
        /// <returns></returns>
        public virtual ITemplateBuilder<TRootModel, TChildModel> AddObject<TChildModel>(string propertyName,
            Func<IJsonTemplateBuilderContext<TRootModel, TModel>, TChildModel> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TRootModel, TChildModel>, bool> ifCheck = null)
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

            return new ChildJsonTemplateObjectBuilder<TRootModel, TModel,TChildModel>(
                propertyName,
                $"{DisplayPropertyName}.{propertyName}",
                this,
                valueFactory,
                childSchemeBuilder,
                Generator,
                Resolver,
                true, ifCheck).Start();
        }

        /// <summary>
        /// 添加一个对象数组
        /// </summary>
        /// <typeparam name="TArrayNewElement"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="valueFactory"></param>
        /// <param name="isNull"></param>
        /// <param name="description"></param>
        /// <param name="ifCheckExp"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual IArrayBuilder<TRootModel, TModel,TArrayNewElement> AddArrayObject<TArrayNewElement>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TRootModel, TModel>, IEnumerable<TArrayNewElement>> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TRootModel, TArrayNewElement>, bool> ifCheckExp = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                Pipe.Add((writer, model) =>
                {
                    writer.WritePropertyName(propertyName);
                });
            }
            else
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var s1 = SchemaBuilder.AddObjectArrayProperty(propertyName, desciption: description, nullable: isNull);

            var s = new ArrayObjectTemplateObjectBuilder<TRootModel, TModel, TArrayNewElement>(propertyName, $"{DisplayPropertyName}.{propertyName}", this, valueFactory, s1, Generator, Resolver, ifCheckExp);

            return s;
        }

        [Obsolete("请使用AddProperty代替")]
        public virtual ITemplateBuilder<TRootModel, TModel> AddArrayValue<TValue>(string propertyName, Func<IJsonTemplateBuilderContext<TRootModel, TModel>, IEnumerable<TValue>> valueFactory, bool isNull = false,
            string description = "", Func<IJsonTemplateBuilderContext<TRootModel, IEnumerable<TValue>>, bool> ifNullRender = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            var propertyInvoke = new ArrayValueInvoker<TRootModel, TModel, TValue>()
            {
                ifNullRender = ifNullRender,
                ParentDisplayName = DisplayPropertyName,
                PropertyName = propertyName,
                valueFactory = valueFactory
            };

            Pipe.Add(propertyInvoke.Invoke);

            JsonObjectType jsonType = NSwagSchemeBuilder.NetTypeToJsonObjectType(typeof(TValue));


            var s1 = SchemaBuilder.AddValueArray(propertyName, jsonType, desciption: description, nullable: isNull);

            return this;
        }

        protected virtual void InvokePipe(JsonWriter writer, JsonTemplateBuilderContext<TRootModel, TModel> context)
        {
            foreach (var func in this.Pipe)
            {
                try
                {
                    func(writer, context);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"{this.GetType().Name}|输出中 :{context.PropertyName}\n{JsonConvert.SerializeObject(e)}");
                    LoggerManager.Default.Error($"{this.GetType().Name}|输出中 :",e);
                    throw;
                }

            }
        }

        protected virtual IJsonTemplateBuilderContext<TRootModel, T> CreateBuilderContext<T>(
            string propertyName,
            T model,
            IJsonTemplateBuilderContext<TRootModel, TModel> parentContext
        )
        {
            var newContext = new JsonTemplateBuilderContext<TRootModel, T>
            (parentContext.HttpContext,
                parentContext.RootModel,
                model,
                parentContext.JsonSerializerSettings)
            {
                //PropertyRenderChecker = context.PropertyRenderChecker
                PropertyName = propertyName,
                _globalTemporaryData = new Lazy<TemplateData>(parentContext.GlobalTemporaryData)
            };

            return newContext;
        }

        public virtual IList<PipeActionBuilder<TRootModel, TModel>> Pipe { get; } = new List<PipeActionBuilder<TRootModel, TModel>>();

        public virtual NSwagSchemeBuilder SchemaBuilder { get; }
        public virtual JsonSchemaGenerator Generator { get; }
        public virtual JsonSchemaResolver Resolver { get; }
        public virtual Type ModelType { get; }

        public virtual IObjectBuilderPipe<TRootModel, TParentModel> Parent { set; get; }

        public virtual Func<IJsonTemplateBuilderContext<TRootModel, TModel>, bool> IfCheckExp { get; }

        public abstract void Dispose();
    }
}