using System;
using System.Collections.Generic;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.JsonTemplate.Exceptions;
using Newtonsoft.Json;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public struct ArrayValueInvoker<TCurrentModel, TArrayElement>
    {
        public string PropertyName { set; get; }

        public Func<IJsonTemplateBuilderContext<TCurrentModel>, IEnumerable<TArrayElement>> valueFactory { set; get; }

        public Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender { set; get; }

        public string ParentDisplayName { set; get; }

        public void Invoke(JsonWriter writer, IJsonTemplateBuilderContext<TCurrentModel> context)
        {
            context.PropertyName = $"{ParentDisplayName}.{PropertyName}";

            IEnumerable<TArrayElement> data = null;

            try
            {
                data = valueFactory(context);
            }
            catch (Exception e)
            {

                throw new DataFactoryException($"数据生成错误:{ParentDisplayName}", e, context);
            }

            try
            {

                if (!data.HasData() && ifNullRender != null)
                {
                    var c = new JsonTemplateBuilderContext<IEnumerable<TArrayElement>>(context.HttpContext,
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
                        context.Serializer.Serialize(writer, value);
                        //await writer.WriteValueAsync(value);
                    }
                }

                writer.WriteEndArray();
            }
            catch (Exception e)
            {
                throw new OutputRenderException(context, $"数据输出错误:{context.PropertyName}", e);
            }
        }
    }
}