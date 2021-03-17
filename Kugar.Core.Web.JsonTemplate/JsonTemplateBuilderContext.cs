using System;
using System.Collections.Generic;
using System.Threading;

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
    }

    internal class JsonTemplateBuilderContext<TModel> : IJsonTemplateBuilderContext<TModel>
    {
        private Lazy<Dictionary<string, object>> _temporaryData = new Lazy<Dictionary<string, object>>();

        public JsonTemplateBuilderContext(Microsoft.AspNetCore.Http.HttpContext context, TModel model)
        {
            HttpContext = context;
            Model = model;
            //CancellationToken = context.RequestAborted;
        }

        public Microsoft.AspNetCore.Http.HttpContext HttpContext { get; }

        public Dictionary<string, object> TemporaryData => _temporaryData.Value;

        public TModel Model { get; }

        public CancellationToken CancellationToken => HttpContext.RequestAborted;
    }
}