﻿using System;
using Kugar.Core.Web.JsonTemplate.Builders;

namespace Kugar.Core.Web.JsonTemplate
{
    public interface IJsonTemplateObject 
    { 
        Type ModelType { get; }
    }

    /// <summary>
    /// 用于构建输出模板的基类
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public abstract class JsonTemplateBase<TModel> :IJsonTemplateObject
    {
        public abstract void BuildScheme(IObjectBuilder<TModel> builder);

        public Type ModelType => typeof(TModel);
    }
}