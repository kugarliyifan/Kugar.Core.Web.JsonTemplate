using System;
using System.Linq.Expressions;
using Kugar.Core.Web.JsonTemplate.Exceptions;

namespace Kugar.Core.Web.JsonTemplate.Helpers
{
    public class PropertyExpInvoker<TModel, TValue>
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

        public TValue Invoke(IJsonTemplateBuilderContext<TModel> context)
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
                return _invoke(context.Model);
            }
            catch (Exception e)
            {
                throw new OutputRenderException(context, $"输出{displayName}出现错误", e)
                {
                    DisplayPropertyPath = displayName
                };
            }
            
        }
    }
}