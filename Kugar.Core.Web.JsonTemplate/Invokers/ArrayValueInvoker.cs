using System;
using System.Collections.Generic;
using System.Diagnostics;
using Kugar.Core.ExtMethod;
using Kugar.Core.Log;
using Kugar.Core.Web.JsonTemplate.Exceptions;
using Newtonsoft.Json;

namespace Kugar.Core.Web.JsonTemplate.Invokers
{
    /// <summary>
    /// 值类型数组执行器
    /// </summary>
    /// <typeparam name="TCurrentModel"></typeparam>
    /// <typeparam name="TArrayElement"></typeparam>
    public class ArrayValueInvoker<TRootModel, TCurrentModel, TArrayElement>
    {
        private string _builderTypeName = "";
        private Type _type = null;

        public string PropertyName { set; get; }

        public Func<IJsonTemplateBuilderContext<TRootModel, TCurrentModel>, IEnumerable<TArrayElement>> valueFactory { set; get; }

        public Func<IJsonTemplateBuilderContext<TRootModel, IEnumerable<TArrayElement>>, bool> ifNullRender { set; get; }

        public string ParentDisplayName { set; get; }

        public Type BuilderTemplate
        {
            set
            {
                _builderTypeName = value.GetType().Name;
                _type = value;
            }
            get
            {
                return _type;
            }
        }

        public void Invoke(JsonWriter writer, IJsonTemplateBuilderContext<TRootModel, TCurrentModel> context)
        {
            context.PropertyName = $"{ParentDisplayName}.{PropertyName}";

            IEnumerable<TArrayElement> data = null;

            if (GlobalSettings.IsRenderTrace)
            {
                Debug.WriteLine($"{_builderTypeName}|Property=开始输出{context.PropertyName}");
            }

            try
            {
                data = valueFactory(context);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{_builderTypeName}|Array=数据生成错误:{ParentDisplayName} \n{JsonConvert.SerializeObject(e)}");
                LoggerManager.Default.Error($"{_builderTypeName}|Array=输出参数错误:{ParentDisplayName}", e);
                throw new DataFactoryException($"数据生成错误:{ParentDisplayName}", e, context);
            }

            try
            {

                if (!data.HasData() && ifNullRender != null)
                {
                    var c = new JsonTemplateBuilderContext<TRootModel, IEnumerable<TArrayElement>>(context.HttpContext,
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
                        if (value==null)
                        {
                            continue;
                        }

                        context.Serializer.Serialize(writer, value);
                        //await writer.WriteValueAsync(value);
                    }
                }

                writer.WriteEndArray();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{_builderTypeName}|Array=数据生成错误:{PropertyName} \n{JsonConvert.SerializeObject(e)}");
                LoggerManager.Default.Error($"{_builderTypeName}|Array=输出参数错误:{PropertyName}", e);
                throw new OutputRenderException(context, $"数据输出错误:{context.PropertyName}", e);
            }
        }
    }
}