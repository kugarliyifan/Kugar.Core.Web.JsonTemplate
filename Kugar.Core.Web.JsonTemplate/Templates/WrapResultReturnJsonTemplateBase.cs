using System;
using System.Collections.Generic;
using System.Text;
using Kugar.Core.BaseStruct;
using Kugar.Core.Web.JsonTemplate.Builders;
using Kugar.Core.Web.JsonTemplate.Helpers;

namespace Kugar.Core.Web.JsonTemplate.Templates
{
    /// <summary>
    /// 在输出的数据外层加多一个ResultReturn的头部,用于在ReturnData为非数组类型的时候使用
    /// </summary>
    /// <typeparam name="TModel">输入的ReturnData类型</typeparam>
    public abstract class WrapResultReturnJsonTemplateBase<TModel> : JsonTemplateBase<TModel>
    {
        private static readonly ResultReturnFactory<TModel>
            _defaultResultFactory = (context) => SuccessResultReturn.Default;

        protected WrapResultReturnJsonTemplateBase()
        {
            this.ResultFactory = _defaultResultFactory;
        }

        public override void BuildScheme(IObjectBuilder<TModel,TModel> builder)
        {
            using (var b = BuildWrap(builder))
            {
                BuildReturnDataScheme(b);
            }
        }

        /// <summary>
        /// 构建内层ReturnData属性内部的参数,对builder参数不要使用using
        /// </summary>
        /// <param name="builder"></param>
        protected abstract void BuildReturnDataScheme(IChildObjectBuilder<TModel,TModel> builder);

        /// <summary>
        /// 用于控制输出的外层ResultReturn的属性
        /// </summary>
        protected virtual ResultReturnFactory<TModel> ResultFactory{get;}

        protected virtual IChildObjectBuilder<TModel,TModel> BuildWrap(IObjectBuilder<TModel,TModel> builder)
        {
            return builder.FromReturnResult(context=>(ResultFactory??_defaultResultFactory).Invoke(context));
        }

        public delegate ResultReturn ResultReturnFactory<in TModel>(IJsonTemplateBuilderContext<TModel,TModel> context);
    }

    
}
