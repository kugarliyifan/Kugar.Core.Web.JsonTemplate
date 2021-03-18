using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate
{
    public interface IObjectBuilderInfo
    {

        public NSwagSchemeBuilder SchemaBuilder { get; }

        public JsonSchemaGenerator Generator { get; }

        public JsonSchemaResolver Resolver { get; }

        public Type ModelType { get; }
    }

    //public interface IObjectBuilderWithEnd<TModel>
    //{
    //    IObjectBuilder<TModel> End();
    //}

    public interface IObjectBuilder<TModel> : IDisposable, IObjectBuilderInfo,IObjectBuilderPipe<TModel>
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
            string description = "",
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifNullRender = null
        );

        IArrayBuilder<TArrayElement> AddArrayObject<TArrayElement>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TArrayElement>> valueFactory,
            bool isNull = false,
            string description = "",
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null
        );

        IObjectBuilder<TModel> AddArrayValue<TArrayElement>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TArrayElement>> valueFactory,
            bool isNull = false,
            string description = "",
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null
        );

        IObjectBuilder<TModel> Start();

        IList<PipeActionBuilder<TModel>> Pipe { get; }

    }

    public class ObjectProperty<TModel>
    {
        public string PropertyName { set; get; }
        public Func<IJsonTemplateBuilderContext<TModel>, object> ValueFactory{ set; get; }
        public string Description { set; get; }
        public bool IsNull { set; get; }
        public object Example { set; get; }
        public Type? NewValueType { set; get; }

        public Func<IJsonTemplateBuilderContext<TModel>, bool> IfCheckExp{ set; get; }
    }

    public interface IChildObjectBuilder< TCurrentModel> : IObjectBuilderInfo,IObjectBuilderPipe<TCurrentModel>,IDisposable
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
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
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

    public interface IObjectBuilderPipe<TModel>
    {
        IList<PipeActionBuilder<TModel>> Pipe { get; }
    }

    public interface IArrayBuilder<TElement> : IDisposable
    {
        IArrayBuilder<TElement> AddProperty<TValue>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TElement>, TValue> valueFactory,
            string description = "",
            bool isNull = false,
            object example = null,
            Type? newValueType = null,
            Func<IJsonTemplateBuilderContext<TElement>, bool> ifCheckExp = null
        );

        IArrayBuilder<TElement> End();
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
                if (!(ifCheckExp?.Invoke(context) ?? true))
                {
                    return;
                }
                
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

            });

            JsonObjectType jsonType = NSwagSchemeBuilder.NetTypeToJsonObjectType(newValueType ?? typeof(TValue));

            SchemaBuilder.AddSingleProperty(propertyName, jsonType,
                description, example, isNull);

            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TChildModel"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="valueFactory"></param>
        /// <param name="isNull"></param>
        /// <param name="description"></param>
        /// <param name="ifNullRender">如果值为null,是否继续调用输出,为true时,继续调用各种参数回调,,为false时,直接输出null</param>
        /// <returns></returns>
        public IChildObjectBuilder<TChildModel> AddObject<TChildModel>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TChildModel> valueFactory,
            bool isNull = false,
            string description = "",
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifNullRender = null
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
                true,
                ifNullRender).Start();
        }

        public IArrayBuilder<TArrayElement> AddArrayObject<TArrayElement>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TArrayElement>> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null)
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

        public IObjectBuilder<TModel> AddArrayValue<TArrayElement>(string propertyName, Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TArrayElement>> valueFactory, bool isNull = false,
            string description = "", Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            _pipe.Add(async (writer, context) =>
            {
                await writer.WritePropertyNameAsync(propertyName, context.CancellationToken);

                var data = valueFactory(context);

                await writer.WriteStartArrayAsync(context.CancellationToken);

                foreach (var value in data)
                {
                    await writer.WriteValueAsync(value,context.CancellationToken);
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

        public void Dispose()
        {
            End();
        }
    }

    internal class ChildJsonTemplateObjectBuilder<TParentModel, TCurrentModel> : IChildObjectBuilder<TCurrentModel>
    {
        private List<PipeActionBuilder<TCurrentModel>> _pipe = new List<PipeActionBuilder<TCurrentModel>>();
        private bool _isNewObject = false;

        private IObjectBuilderPipe<TParentModel> _parent;

        //private IList<PipeActionBuilder<TChildModel>> _changeTypeParent = null;
        private Func<IJsonTemplateBuilderContext<TParentModel>, TCurrentModel> _childObjFactory;
        private Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> _ifNullRender = null;

        public ChildJsonTemplateObjectBuilder(IObjectBuilderPipe<TParentModel> parent,
            Func<IJsonTemplateBuilderContext<TParentModel>, TCurrentModel> childObjFactory,
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver,
            bool isNewObject,
            Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> ifNullRender = null) //: base(schemeBuilder, generator, resolver)
        {
            _parent = parent;
            _childObjFactory = childObjFactory;
            _ifNullRender = ifNullRender;
            _isNewObject = isNewObject;
            this.SchemaBuilder = schemeBuilder;
            this.Generator = generator;
            this.Resolver = resolver;
        }

        public IChildObjectBuilder<TCurrentModel> AddProperty<TValue>(string propertyName, Func<IJsonTemplateBuilderContext<TCurrentModel>, TValue> valueFactory, string description = "",
            bool isNull = false, object example = null, Type? newValueType = null, Func<IJsonTemplateBuilderContext<TCurrentModel>, bool> ifCheckExp = null)
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

                Console.WriteLine("输出属性:" + propertyName);

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
                true,ifNullRender).Start();
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

                foreach (var value in data)
                {
                    await writer.WriteValueAsync(value);
                }

                await writer.WriteEndArrayAsync();
            });

            SchemaBuilder.AddValueArray(propertyName,NSwagSchemeBuilder.NetTypeToJsonObjectType(typeof(TArrayElement)), description, isNull);

            return this;
        }

        public IChildObjectBuilder<TCurrentModel> Start()
        {
            if (_isNewObject)
            {
                _pipe.Add((async (writer, context) => await writer.WriteStartObjectAsync()));
            }

            return this;
        }

        public void End()
        {
            if (_isNewObject)
            {
                _pipe.Add((async (writer, context) => await writer.WriteEndObjectAsync(context.CancellationToken)));
            }
            
            _parent.Pipe.Add(async (writer, context) =>
            {
                var value = _childObjFactory(context);

                var newContext = new JsonTemplateBuilderContext<TCurrentModel>(context.HttpContext, value);

                if (value == null && _ifNullRender != null)
                {
                    if (_ifNullRender(newContext))
                    {
                        foreach (var builder in _pipe)
                        {
                            await builder(writer, newContext);
                        }
                    }
                    else
                    {
                        await writer.WriteNullAsync(context.CancellationToken);
                    }
                }
                else
                {
                    foreach (var builder in _pipe)
                    {
                        await builder(writer, newContext);
                    }
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

    internal class ArrayObjectTemplateObjectBuilder<TParentModel, TElementModel> : IArrayBuilder<TElementModel>
    {
        private List<PipeActionBuilder<TElementModel>> _pipe = new List<PipeActionBuilder<TElementModel>>();
        private Func<IJsonTemplateBuilderContext<TParentModel>, IEnumerable<TElementModel>> _arrayValueFactory = null;
        private IList<PipeActionBuilder<TParentModel>> _parent = null;

        public ArrayObjectTemplateObjectBuilder(
            IObjectBuilderPipe<TParentModel> parent,
            Func<IJsonTemplateBuilderContext<TParentModel>, IEnumerable<TElementModel>> arrayValueFactory,
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver
        )
        {
            _arrayValueFactory = arrayValueFactory;
            _parent = parent.Pipe;
            this.SchemaBuilder = schemeBuilder;
            this.Generator = generator;
            this.Resolver = resolver;
        }
        

        public void Dispose()
        {
            this.End();
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

            _pipe.Add(async (writer, context) =>
            {
                if (!(ifCheckExp?.Invoke(context) ?? true))
                {
                    return;
                }

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

            });

            JsonObjectType jsonType = NSwagSchemeBuilder.NetTypeToJsonObjectType(newValueType ?? typeof(TValue));


            SchemaBuilder.AddSingleProperty(propertyName, jsonType,
                description, example, isNull);

            return this;
        }

        public IArrayBuilder<TElementModel> End()
        {
            _parent.Add(async (writer, context) =>
            {
                await writer.WriteStartArrayAsync(context.CancellationToken);

                var array = _arrayValueFactory(new JsonTemplateBuilderContext<TParentModel>(context.HttpContext, context.Model));

                foreach (var element in array)
                {
                    await writer.WriteStartObjectAsync(context.CancellationToken);

                    var newContext = new JsonTemplateBuilderContext<TElementModel>(context.HttpContext, element);

                    foreach (var func in _pipe)
                    {
                        await func(writer, newContext);
                    }

                    await writer.WriteEndObjectAsync(context.CancellationToken);
                }

                await writer.WriteEndArrayAsync(context.CancellationToken);
            });

            return this;
        }

        public IList<PipeActionBuilder<TElementModel>> Pipe => _pipe;

        protected internal NSwagSchemeBuilder SchemaBuilder { get; set; }

        protected JsonSchemaGenerator Generator { get; set; }

        protected JsonSchemaResolver Resolver { get; set; }
    }


}
