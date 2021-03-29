using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Kugar.Core.Web.JsonTemplate
{
    public interface IJsonTemplateBuilderContext<out TModel>
    {
        /// <summary>
        /// 当前请求的HttpContext
        /// </summary>
        Microsoft.AspNetCore.Http.HttpContext HttpContext { get; }

        /// <summary>
        /// 本次输出的临时数据
        /// </summary>
        Dictionary<string, object> TemporaryData { get; }

        /// <summary>
        /// 传入的Model数据
        /// </summary>
        TModel Model { get; }

        /// <summary>
        /// Request上的RequestAborted通知
        /// </summary>
        CancellationToken CancellationToken { get; }

        JsonSerializerSettings JsonSerializerSettings { get; }
        
        ILogger Logger { get; }
    }

    public interface IJsonArrayTemplateBuilderContext<out TModel>:IJsonTemplateBuilderContext<TModel>
    {
        /// <summary>
        /// 单次循环中的临时数据
        /// </summary>
        Dictionary<string,object> LoopTemporaryData { get; }
    }

    internal class JsonTemplateBuilderContext<TModel> : IJsonTemplateBuilderContext<TModel>
    {
        private Lazy<Dictionary<string, object>> _temporaryData = new Lazy<Dictionary<string, object>>();
        private Lazy<ILogger> _loggerFactory = null;

        public JsonTemplateBuilderContext(HttpContext context, TModel model,JsonSerializerSettings settings)
        {
            HttpContext = context;
            Model = model;
            JsonSerializerSettings = settings;
            _loggerFactory = new Lazy<ILogger>(getLogger);
            //CancellationToken = context.RequestAborted;
        }

        public HttpContext HttpContext { get; }

        public Dictionary<string, object> TemporaryData => _temporaryData.Value;

        public TModel Model { get; }

        public CancellationToken CancellationToken => HttpContext.RequestAborted;

        public JsonSerializerSettings JsonSerializerSettings { get; }

        public ILogger Logger => _loggerFactory.Value;
            

        private ILogger getLogger()=>((ILoggerFactory) HttpContext.RequestServices.GetService(typeof(ILoggerFactory))).CreateLogger(
            "jsontemplate");

    }

    internal class JsonArrayTemplateBuilderContext<TModel> : JsonTemplateBuilderContext<TModel>,
        IJsonArrayTemplateBuilderContext<TModel>
    {
        private Lazy<Dictionary<string, object>> _loopTemporaryData = new Lazy<Dictionary<string, object>>();


        public JsonArrayTemplateBuilderContext(HttpContext context, TModel model,JsonSerializerSettings settings) : base(context, model,settings)
        {
        }

        public Dictionary<string, object> LoopTemporaryData => _loopTemporaryData.Value;
    }
}