﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

                var model = (TModel) Model;
                 
                var modelContext = new JsonTemplateBuilderContext<TModel>(context.HttpContext,model, model,jsonSettings);

                foreach (var pipe in objectBuilder.Pipe)
                {
                    try
                    {
                        await pipe(jsonWriter,modelContext );
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    
                }

                await jsonWriter.FlushAsync(context.HttpContext.RequestAborted);
            }

        }
    }
}