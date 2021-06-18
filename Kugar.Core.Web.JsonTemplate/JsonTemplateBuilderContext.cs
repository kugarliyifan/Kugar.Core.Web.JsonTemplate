using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Kugar.Core.Web.JsonTemplate
{
    public interface IJsonTemplateBuilderContext
    {
        /// <summary>
        /// 当前请求的HttpContext
        /// </summary>
        Microsoft.AspNetCore.Http.HttpContext HttpContext { get; }

        /// <summary>
        /// 本次输出范围内的临时数据,ChildObject则为子对象整个输出的范围内,,如果是ObjecrArray则为整个数组对象的输出范围内
        /// </summary>
        Dictionary<string, object> ScopeTemporaryData { get; }
        

        /// <summary>
        /// 单个Request输出的范围内的临时数据
        /// </summary>
        Dictionary<string, object>  GlobalTemporaryData { get; }

       

        /// <summary>
        /// Request上的RequestAborted通知
        /// </summary>
        CancellationToken CancellationToken { get; }

        JsonSerializerSettings JsonSerializerSettings { get; }
        
        ILogger Logger { get; }
    }

    public interface IJsonTemplateBuilderContext<TModel>:IJsonTemplateBuilderContext
    {
        /// <summary>
        /// 传入的Model数据
        /// </summary>
        TModel Model { get; set; }

        dynamic RootModel { get; }
    }

    public interface IJsonArrayTemplateBuilderContext<TModel>:IJsonTemplateBuilderContext<TModel>
    {
        /// <summary>
        /// 在ArrayObject单次循环中的临时数据
        /// </summary>
        Dictionary<string,object> LoopItemTemporaryData { get; }
    }

    internal class JsonTemplateBuilderContext<TModel> : IJsonTemplateBuilderContext<TModel>
    {
        private Lazy<Dictionary<string, object>> _temporaryData = new Lazy<Dictionary<string, object>>();
        internal Lazy<Dictionary<string, object>> _globalTemporaryData = null;
        private Lazy<ILogger> _loggerFactory = null;

        public JsonTemplateBuilderContext(HttpContext context,dynamic rootModel, TModel model,JsonSerializerSettings settings,Lazy<Dictionary<string,object>> globalTemporaryDataFactory=null)
        {
            HttpContext = context;
            Model = model;
            JsonSerializerSettings = settings;
            _loggerFactory = new Lazy<ILogger>(getLogger);
            _globalTemporaryData = globalTemporaryDataFactory ?? new Lazy<Dictionary<string, object>>();
            RootModel = rootModel;
            //GlobalScopeData = new ExpandoObject();
            //CancellationToken = context.RequestAborted;
        }

        public HttpContext HttpContext { get; }

        public Dictionary<string, object> ScopeTemporaryData => _temporaryData.Value;

        public Dictionary<string, object> GlobalTemporaryData => _globalTemporaryData.Value;

        public TModel Model { get; set; }

        public dynamic RootModel  { get; }

        public CancellationToken CancellationToken => HttpContext.RequestAborted;

        public JsonSerializerSettings JsonSerializerSettings { get; }

        public ILogger Logger => _loggerFactory.Value;
            

        private ILogger getLogger()=>((ILoggerFactory) HttpContext.RequestServices.GetService(typeof(ILoggerFactory))).CreateLogger(
            "jsontemplate");

    }

    //internal class JsonArrayTemplateBuilderContext<TModel> : JsonTemplateBuilderContext<TModel>,
    //    IJsonArrayTemplateBuilderContext<TModel>
    //{
    //    private Lazy<Dictionary<string, object>> _loopTemporaryData = new Lazy<Dictionary<string, object>>();


    //    public JsonArrayTemplateBuilderContext(HttpContext context, TModel model,JsonSerializerSettings settings) : base(context, model,settings)
    //    {
    //    }

    //    public Dictionary<string, object> LoopItemTemporaryData => _loopTemporaryData.Value;
    //}
}