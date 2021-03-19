using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Kugar.Core.BaseStruct;
using Kugar.Core.Web.JsonTemplate.Builders;
using Kugar.Core.Web.JsonTemplate.Helpers;

namespace Kugar.Core.Web.JsonTemplate.Templates
{
    /// <summary>
    /// 在输出的数据外层加多一个ResultReturn的头部,用于ReturnData为IEnumerable类型的时候使用
    /// </summary>
    /// <typeparam name="TModel">输入的ReturnData中数组内Item的类型</typeparam>
    public abstract  class WrapResultReturnArrayJsonTemplateBase<TModel> : JsonTemplateBase<IEnumerable<TModel>>
    {
        private static readonly ResultReturnArrayFactory<TModel>
            _defaultResultFactory = (context) => SuccessResultReturn.Default;

        protected WrapResultReturnArrayJsonTemplateBase()
        {
            this.ResultFactory = _defaultResultFactory;
        }

        /// <summary>
        /// 构建内层ReturnData属性数组Item内部的参数
        /// </summary>
        /// <param name="builder"></param>
        protected abstract void BuildReturnDataScheme(IArrayBuilder<TModel> builder);

        /// <summary>
        /// 用于控制输出的外层ResultReturn的属性
        /// </summary>
        protected virtual ResultReturnArrayFactory<TModel> ResultFactory{get;}

        protected virtual IArrayBuilder<TModel> BuildWrap(IObjectBuilder<IEnumerable<TModel>> builder)
        {
            using (var b=builder.FromObject(context => (ResultFactory ?? _defaultResultFactory).Invoke(context)))
            {
                b.AddProperties(x=>x.IsSuccess,x=>x.Message,x=>x.ReturnCode);
            }

            return builder.AddArrayObject("returnData", x => x.Model);
        }

        public override void BuildScheme(IObjectBuilder<IEnumerable<TModel>> builder)
        {
            using (var b = BuildWrap(builder))
            {
                BuildReturnDataScheme(b);
            }
        }

        public delegate ResultReturn ResultReturnArrayFactory<in TElement>(IJsonTemplateBuilderContext<IEnumerable<TElement>> context);
    }
}
