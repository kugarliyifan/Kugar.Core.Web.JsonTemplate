using System;
using Fasterflect;
using Microsoft.AspNetCore.Mvc;

namespace Kugar.Core.Web.JsonTemplate
{
    public static class JsonTemplateObjectExt
    {
        public static IActionResult Json<TBuilder>(this ControllerBase controller,
            object value) where TBuilder : IJsonTemplateObject, new()
        {
            var constructorInvoker = GlobalJsonTemplateCache.GetActionResultType(typeof(TBuilder), value.GetType());
            
            var o = (IJsonTemplateActionResult) constructorInvoker(typeof(TBuilder));
            
            o.Model = value;

            return o;
        }
        
    }
}