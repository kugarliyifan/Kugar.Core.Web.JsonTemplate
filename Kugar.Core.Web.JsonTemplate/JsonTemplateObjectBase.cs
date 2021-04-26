using System;
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
        /// <summary>
        /// 构建属性的输出,builder一定不要使用using
        /// </summary>
        /// <param name="builder">属性构建器,,一定不要对builder使用using</param>
        public abstract void BuildScheme(IObjectBuilder<TModel,TModel> builder);

        /// <summary>
        /// 当前的模型类型
        /// </summary>
        public Type ModelType => typeof(TModel);
    }
}