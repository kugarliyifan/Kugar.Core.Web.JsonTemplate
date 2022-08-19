using System;
using System.Diagnostics;
using System.Linq.Expressions;
using Kugar.Core.ExtMethod;
using Kugar.Core.Log;
using Kugar.Core.Web.JsonTemplate.Exceptions;
using Kugar.Core.Web.JsonTemplate.Helpers;
using Newtonsoft.Json;
using YamlDotNet.Core.Tokens;

namespace Kugar.Core.Web.JsonTemplate.Invokers
{
    /// <summary>
    /// 对象属性表达式执行器
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class PropertyExpInvoker<TRootModel, TModel, TValue>
    {
        private Func<TModel, TValue> _invoke = null;

        public string NewPropertyName { set; get; }

        public Expression<Func<TModel, TValue>> objectPropertyExp { set; get; }

        public PropertyExpInvoker(string newPropertyName, Expression<Func<TModel, TValue>> prop)
        {
            if (prop == null)
            {
                throw new ArgumentNullException(nameof(prop));
            }

            if (string.IsNullOrEmpty(newPropertyName))
            {
                NewPropertyName = ExpressionHelpers.GetExpressionPropertyName(prop);
            }

            objectPropertyExp = prop;
            

            _invoke = prop.Compile();

            
        }

        public TValue Invoke(IJsonTemplateBuilderContext<TRootModel, TModel> context)
        {
            var displayName = $"{context.PropertyName}.{NewPropertyName}";

            context.PropertyName = displayName;

            if (context.Model == null)
            {
                throw new OutputRenderException(context, "传入的Model为null")
                {
                    DisplayPropertyPath = displayName
                };
            }

            try
            {
                var value= _invoke(context.Model);

                if (GlobalSettings.IsRenderTrace)
                {
                    Debug.WriteLine($"{this.GetType().Name}|Property:{context.PropertyName}=输出属性值{value?.ToStringEx()??"空字符串"}", "JsonTemplate");
                }

                return value;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{this.GetType().Name}|Property:{context.PropertyName}=输出错误 \n,{JsonConvert.SerializeObject(e)}", "JsonTemplate");
                LoggerManager.Default.Error($"{this.GetType().Name}|Property:{context.PropertyName}",e);
                throw new OutputRenderException(context, $"输出{displayName}出现错误", e)
                {
                    DisplayPropertyPath = displayName
                };
            }
            
        }
    }
}