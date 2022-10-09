using Kugar.Core.BaseStruct;
using Kugar.Core.Web.JsonTemplate.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kugar.Core.Web.JsonTemplate.Templates
{
    /// <summary>
    /// 输出
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public abstract class WrapResultReturnTemplateBase<TModel> : JsonTemplateBase<TModel>
    {
        private static readonly ResultReturnFactory<TModel>
            _defaultResultFactory = (context) => SuccessResultReturn.Default;

        protected WrapResultReturnTemplateBase()
        {
            this.ResultFactory = _defaultResultFactory;
        }

        public override void BuildScheme(RootObjectTemplateBuilder<TModel> builder)
        {
            using (var f = FromObject(builder, GetResultReturn))
            {
                f.AddProperty("isSuccess", x => x.Model.IsSuccess, description: "本次操作是否成功")
                    .AddProperty("message", x => x.Model.Message, description: "本次操作的操作结果文本")
                    .AddProperty("returnCode", x => 0, description: "操作结果代码");
            }

            BuildReturnDataScheme(builder);
        }

        /// <summary>
        /// 构建内层ReturnData属性内部的参数,对builder参数不要使用using
        /// </summary>
        /// <param name="builder"></param>
        protected abstract void BuildReturnDataScheme(RootObjectTemplateBuilder<TModel> builder);

        /// <summary>
        /// 用于控制输出的外层ResultReturn的属性
        /// </summary>
        protected virtual ResultReturnFactory<TModel> ResultFactory { get; }


        protected virtual ResultReturn GetResultReturn(IJsonTemplateBuilderContext<TModel, TModel> context)
        {
            return _defaultResultFactory(context);
        }

        public delegate ResultReturn ResultReturnFactory<TModelData>(IJsonTemplateBuilderContext<TModel, TModelData> context);
    }
}
