using System;
using Microsoft.AspNetCore.Mvc;

namespace Kugar.Core.Web.JsonTemplate.Helpers
{
    public static class JsonTemplateObjectExt
    {
        public static IActionResult Json<TBuilder>(this ControllerBase controller,
            object value) where TBuilder : IJsonTemplateObject, new()
        {
            if (value==null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var constructorInvoker = GlobalJsonTemplateCache.GetActionResultType(typeof(TBuilder), value.GetType());
            
            var o = (IJsonTemplateActionResult) constructorInvoker(typeof(TBuilder));
            
            o.Model = value;

            return o;
        }
        
    }
}