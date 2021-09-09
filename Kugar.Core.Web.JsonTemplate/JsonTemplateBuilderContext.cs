﻿using System;
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
        TemplateData ScopeTemporaryData { get; }
        

        /// <summary>
        /// 单个Request输出的范围内的临时数据
        /// </summary>
        TemplateData  GlobalTemporaryData { get; }

       

        /// <summary>
        /// Request上的RequestAborted通知
        /// </summary>
        CancellationToken CancellationToken { get; }

        JsonSerializerSettings JsonSerializerSettings { get; }

        JsonSerializer Serializer { get; }
        
        ILogger Logger { get; }
    }

    public interface IJsonTemplateBuilderContext<TModel>:IJsonTemplateBuilderContext
    {
        /// <summary>
        /// 传入的Model数据
        /// </summary>
        TModel Model { get; set; }

        dynamic RootModel { get; }

        //Func<IJsonTemplateBuilderContext, string, bool> PropertyRenderChecker { set; get; }
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
        private Lazy<TemplateData> _temporaryData = new Lazy<TemplateData>();
        internal Lazy<TemplateData> _globalTemporaryData = null;
        private Lazy<ILogger> _loggerFactory = null;

        public JsonTemplateBuilderContext(Microsoft.AspNetCore.Http.HttpContext context,dynamic rootModel, TModel model,JsonSerializerSettings settings,Lazy<TemplateData> globalTemporaryDataFactory=null)
        {
            HttpContext = context;
            Model = model;
            JsonSerializerSettings = settings;
            _loggerFactory = new Lazy<ILogger>(getLogger);
            _globalTemporaryData = globalTemporaryDataFactory ?? new Lazy<TemplateData>();
            RootModel = rootModel;
            Serializer= JsonSerializer.Create(JsonSerializerSettings);
            //GlobalScopeData = new ExpandoObject();
            //CancellationToken = context.RequestAborted;
        }

        public Microsoft.AspNetCore.Http.HttpContext HttpContext { get; }

        public TemplateData ScopeTemporaryData => _temporaryData.Value;

        public TemplateData GlobalTemporaryData => _globalTemporaryData.Value;

        public TModel Model { get; set; }

        public dynamic RootModel  { get; }

        //public Func<IJsonTemplateBuilderContext, string, bool> PropertyRenderChecker { get; set; }

        public CancellationToken CancellationToken => HttpContext.RequestAborted;

        public JsonSerializerSettings JsonSerializerSettings { get; }
        public JsonSerializer Serializer { get; }

        public ILogger Logger => _loggerFactory.Value;
            

        private ILogger getLogger()=>((ILoggerFactory) HttpContext.RequestServices.GetService(typeof(ILoggerFactory))).CreateLogger(
            "jsontemplate");

    }

    public class TemplateData
    {
        private Dictionary<string, object> _data = new Dictionary<string, object>();

        public TValue Get<TValue>(string key)
        {
            if (_data.TryGetValue(key,out var data))
            {
                return (TValue)data;
            }
            else
            {
                return default;
            }
        }

        public TemplateData Set<TValue>(string key, TValue value)
        {
            if (_data.ContainsKey(key))
            {
                _data[key] = value;
            }
            else
            {
                _data.Add(key,value);
            }

            return this;
        }

        public void Remove(string key)
        {
            _data.Remove(key);  
        }
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