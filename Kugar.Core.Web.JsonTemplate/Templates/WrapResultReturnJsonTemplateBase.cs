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

        public override void BuildScheme(RootObjectTemplateBuilder<TModel> builder)
        {
            using (var b = BuildWrap(builder))
            {
                BuildReturnDataScheme((SameRootTemplateBuilder<TModel>)b);
            }
        }

        /// <summary>
        /// 构建内层ReturnData属性内部的参数,对builder参数不要使用using
        /// </summary>
        /// <param name="builder"></param>
        protected abstract void BuildReturnDataScheme(SameRootTemplateBuilder<TModel> builder);

        /// <summary>
        /// 用于控制输出的外层ResultReturn的属性
        /// </summary>
        protected virtual ResultReturnFactory<TModel> ResultFactory{get;}

        protected virtual SameRootTemplateBuilder<TModel> BuildWrap(RootObjectTemplateBuilder<TModel> builder) 
        {
            using (var f=FromObject(builder,GetResultReturn))
            {
                f.AddProperty("isSuccess", x =>x.Model.IsSuccess, description: "本次操作是否成功")
                    .AddProperty("message", x => x.Model.Message, description: "本次操作的操作结果文本")
                    .AddProperty("returnCode", x => 0, description: "操作结果代码");
            }
            
            return builder.AddObject("returnData", x => x.Model); 
        }

        protected virtual ResultReturn GetResultReturn(IJsonTemplateBuilderContext<TModel, TModel> context)
        {
            return _defaultResultFactory(context);
        }

        public delegate ResultReturn ResultReturnFactory<TModelData>(IJsonTemplateBuilderContext<TModel, TModelData> context);
    }

    
}
