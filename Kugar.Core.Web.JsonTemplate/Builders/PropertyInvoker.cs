using System;
using System.Diagnostics;
using Kugar.Core.Web.JsonTemplate.Exceptions;
using Newtonsoft.Json;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
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
}