using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kugar.Core.Web.JsonTemplate
{
    public interface IJsonTemplateActionResult : IActionResult
    {
        public object Model { set; get; }
        
    }

    public interface IJsonTemplateActionResult<TBuilder, in TModel> : IJsonTemplateActionResult
    {

    }

    internal class JsonTemplateActionResult<TBuilder,TModel> : IJsonTemplateActionResult<TBuilder,TModel> where TBuilder : JsonTemplateBase<TModel>, new()
    {
        private Type _builderType = null;

        public JsonTemplateActionResult(Type builderType)
        {
            _builderType = builderType;
        }
        
        public object Model { get; set; }
        
        public async Task ExecuteResultAsync(ActionContext context)
        {
            //Debugger.Break();

            context.HttpContext.Response.ContentType = "application/json";

            JsonSerializerSettings jsonSettings = null;

            var jsonOpt = (IOptionsSnapshot<MvcNewtonsoftJsonOptions>)context.HttpContext.RequestServices.GetService(typeof(IOptions<MvcNewtonsoftJsonOptions>));

            if (jsonOpt?.Value != null)
            {
                jsonSettings = jsonOpt.Value.SerializerSettings;
            }
            else
            {
                jsonSettings = JsonConvert.DefaultSettings?.Invoke();
            }
            
            using (var textWriter = new StreamWriter(context.HttpContext.Response.Body))
            using (var jsonWriter = new JsonTextWriter(textWriter))
            {
                jsonWriter.Formatting = jsonSettings?.Formatting??Formatting.None;
                jsonWriter.DateFormatString = jsonSettings?.DateFormatString??"yyyy-MM-dd HH:mm:ss";
                jsonWriter.DateFormatHandling = jsonSettings?.DateFormatHandling?? DateFormatHandling.IsoDateFormat;
                jsonWriter.Culture = jsonSettings?.Culture??CultureInfo.CurrentUICulture;
                jsonWriter.DateTimeZoneHandling = jsonSettings?.DateTimeZoneHandling??DateTimeZoneHandling.Local;
                jsonWriter.FloatFormatHandling = jsonSettings?.FloatFormatHandling??FloatFormatHandling.String;
                
                var objectBuilder=GlobalJsonTemplateCache.GetTemplate<TBuilder, TModel>();

                TModel model;

                try
                {
                    model = (TModel) Model;
                }
                catch (Exception e)
                {
                    var logger = (ILogger) context.HttpContext.RequestServices.GetService(typeof(ILogger));
                    logger.Log(LogLevel.Error,"格式转换出错",e);

                    await Task.FromException(new Exception("格式转换出错", e));

                    return;

                    //throw new Exception("格式转换出错", e);
                }
                
                if (model==null)
                {
                    await Task.FromException(new ArgumentNullException(nameof(model),
                        $"model转换为{typeof(TModel).Name}失败,请检查传入的model的类型"));
                    return;
                    //throw new ArgumentNullException(nameof(model), $"model转换为{typeof(TModel).Name}失败,请检查传入的model的类型");
                }

                var modelContext = new JsonTemplateBuilderContext<TModel>(context.HttpContext,model, model,jsonSettings);
                //modelContext.PropertyRenderChecker = ((JsonTemplateBase<TModel>) objectBuilder).PropertyRenderCheck;

                foreach (var pipe in objectBuilder.Pipe)
                {
                    try
                    {
                        pipe(jsonWriter,modelContext );
                    }
                    catch (Exception e)
                    {
                        var logger = (ILogger) context.HttpContext.RequestServices.GetService(typeof(ILogger));
                        logger.Log(LogLevel.Error,"piple函数执行出错",e);

                        context.HttpContext.Response.StatusCode = 500;
                        

                        await Task.FromException(e);
                     
                        //throw;
                    }
                    
                }

                await jsonWriter.FlushAsync(context.HttpContext.RequestAborted);
            }

        }
    }
}