using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.BaseStruct;
using Kugar.Core.Web.JsonTemplate.Builders;
using Kugar.Core.Web.JsonTemplate.Helpers;

namespace Kugar.Core.Web.JsonTemplate.Templates
{
    /// <summary>
    /// 在输出的数据外层加多一个ResultReturn的头部,用于ReturnData为IEnumerable类型的时候使用
    /// </summary>
    /// <typeparam name="TModel">输入的ReturnData中数组内Item的类型</typeparam>
    public abstract  class WrapResultReturnArrayJsonTemplateBase<TElement> : JsonTemplateBase<IEnumerable<TElement>> //where TModel:IEnumerable<TElement>
    {
        private static readonly ResultReturnArrayFactory<IEnumerable<TElement>>
            _defaultResultFactory = (context) => SuccessResultReturn.Default;

        /// <summary>
        /// 构建内层ReturnData属性数组Item内部的参数,对builder参数不要使用using
        /// </summary>
        /// <param name="builder"></param>
        protected abstract void BuildReturnDataScheme(IArrayBuilder<TElement> builder);
        

        protected virtual IArrayBuilder<TElement> BuildWrap(IObjectBuilder<IEnumerable<TElement>> builder)
        {
            builder.Pipe.Add((writer, context) =>
            {
                context.Model = HandleList(context.Model);
                return Task.CompletedTask;
            });

            using (var b=builder.FromObject(GetResultReturn))
            {
                b.AddProperties(x=>x.IsSuccess,x=>x.Message,x=>x.ReturnCode);
            }

            return builder.AddArrayObject<TElement>("returnData", x => x.Model);
        }

        public override void BuildScheme(IObjectBuilder<IEnumerable<TElement>> builder)
        {
            using (var b = BuildWrap(builder))
            {
                BuildReturnDataScheme(b);
            }
        }

        protected virtual IResultReturn GetResultReturn(IJsonTemplateBuilderContext<IEnumerable<TElement>> context)
        {
            return _defaultResultFactory(context);
        }

        /// <summary>
        /// 对输入的数据进行整理
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        protected virtual IEnumerable<TElement> HandleList(IEnumerable<TElement> src)
        {
            return src;
        }

        public delegate ResultReturn ResultReturnArrayFactory<TModel>(IJsonTemplateBuilderContext<TModel> context);
    }
}
