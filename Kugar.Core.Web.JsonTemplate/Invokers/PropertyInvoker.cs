using System;
using System.Collections;
using System.Diagnostics;
using Kugar.Core.ExtMethod;
using Kugar.Core.Log;
using Kugar.Core.Web.JsonTemplate.Exceptions;
using Newtonsoft.Json;

namespace Kugar.Core.Web.JsonTemplate.Invokers
{
    /// <summary>
    /// 属性执行器
    /// </summary>
    /// <typeparam name="TCurrentModel"></typeparam>
    /// <typeparam name="TNewChildModel"></typeparam>
    /// <typeparam name="TRootModel">根节点类型</typeparam>
    public class PropertyInvoker<TRootModel, TCurrentModel, TNewChildModel>
    {
        private string _builderTypeName ="";
        private Type _type = null;

        public string PropertyName { set; get; }

        public Func<IJsonTemplateBuilderContext<TRootModel, TCurrentModel>, bool> ifCheckExp { set; get; }

        public string ParentDisplayName { set; get; }

        public bool IsRawRender { set; get; }

        public Func<IJsonTemplateBuilderContext<TRootModel, TCurrentModel>, TNewChildModel> valueFactory { set; get; }

        public Type BuilderTemplate
        {
            set
            {
                _builderTypeName=value.GetType().Name;
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

            if (context.Model==null)
            { 
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
                if (GlobalSettings.IsRenderTrace)
                {
                    Debug.WriteLine($"{_builderTypeName}|Property=数据生成错误:{PropertyName} \n{JsonConvert.SerializeObject(e)}");    
                }
                
                LoggerManager.Default.Error($"{_builderTypeName}|Property=数据生成错误:{PropertyName}", e);
                throw new DataFactoryException($"数据生成错误:{PropertyName}", e, context);
            }

            try
            {
                //if (value is IEnumerable)
                //{
                //    throw new DataFactoryException($"由于使用了AddProperty函数,因此{PropertyName}返回的值必须是值类型,不允许是数组");
                //}

                writer.WritePropertyName(PropertyName);

                if (GlobalSettings.IsRenderTrace)
                {
                    Debug.WriteLine($"{_builderTypeName}|Property=正在输出属性:{PropertyName}={(value?.ToStringEx() ?? "null")}");
                }

                if (value != null)
                {
                    if (IsRawRender && value is string  s)
                    {
                        if (!string.IsNullOrEmpty(s))
                        { 
                            writer.WriteRawValue(s);
                            //writer.WriteRaw(s);    
                            //writer.WriteEnd();
                        }
                        else
                        {
                            writer.WriteNull();
                        }
                    }
                    else
                    {
                        if (!(value is string)  && value is IEnumerable arr)
                        {
                            //Debugger.Break();
                            writer.WriteStartArray();

                            foreach (var item in arr)
                            {
                                writer.WriteValue(item);
                            }

                            writer.WriteEndArray();
                        }
                        else
                        {
                            writer.WriteValue(value);
                        }
                         
                    }
                    
                }
                else
                {
                    writer.WriteNull();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{_builderTypeName}|Property=正在输出属性:{PropertyName}={(value?.ToStringEx() ?? "null")} Errors:\n{JsonConvert.SerializeObject(e)}");

                LoggerManager.Default.Error($"{_builderTypeName}|Property=输出参数错误:{PropertyName}", e);

                throw new OutputRenderException(context, $"{_builderTypeName}=输出参数错误:{PropertyName}", e);

                
            }
        }
    }
}