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
        public abstract void BuildScheme(IObjectBuilder<TModel> builder);

        /// <summary>
        /// 当前的模型类型
        /// </summary>
        public Type ModelType => typeof(TModel);

        /// <summary>
        /// 检查是否添加该属性,,返回true,输出该属性,,false为不输出该属性
        /// </summary>
        /// <param name="model"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected virtual bool PropertyRenderCheck(IJsonTemplateBuilderContext model, string propertyName)
        {
            return true;
        }
    }
}