using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.JsonTemplate.Builders;

namespace Kugar.Core.Web.JsonTemplate.Helpers
{
    /// <summary>
    /// Kugare.Core中一些通用的类的输出
    /// </summary>
    public static class CoreStructExtMethod
    {
        public static ITemplateBuilder<TModel> FromReturnResult<TModel>(this ITemplateBuilder<TModel> source,Func<IJsonTemplateBuilderContext<TModel>, bool> resultFactory)
        {
            source.AddProperty("isSuccess", resultFactory,"本次操作是否成功")
                .AddProperty("message", x => string.Empty,"操作结果文本")
                .AddProperty("returnCode", x => 0, "返回的执行结果代码", example: 0)
                ;

            return source.AddObject("returnData", x => x.Model);
        }

        public static ITemplateBuilder<TModel> FromReturnResult<TModel>(this ITemplateBuilder<TModel> source,
            Func<IJsonTemplateBuilderContext<TModel>, (bool isSuccess, string message)> resultFactory)
        {
            using (var f = source.FromObject(resultFactory))
            {
                f.AddProperty("isSuccess", x => x.Model.isSuccess, description: "本次操作是否成功")
                    .AddProperty("message", x => x.Model.message, description: "本次操作的操作结果文本")
                    .AddProperty("returnCode", x =>0, description: "操作结果代码");

                //f.AddProperties(x => x.isSuccess, x => x.message)
                //    .AddProperty("returnCode", x => 0, "返回的执行结果代码", example: 0)
                //    ;
            }

            return source.AddObject("returnData", x => x.Model);
        }

        public static ITemplateBuilder<TModel> FromReturnResult<TModel>(
            this ITemplateBuilder<TModel> source,
            Func<IJsonTemplateBuilderContext<TModel>, (bool isSuccess, int returnCode, string message)> resultFactory)
        {
            using (var f = source.FromObject(resultFactory))
            {
                f.AddProperty("isSuccess", x => x.Model.isSuccess, description: "本次操作是否成功")
                    .AddProperty("message", x => x.Model.message, description: "本次操作的操作结果文本")
                    .AddProperty("returnCode", x => x.Model.returnCode, description: "操作结果代码");
                
                //f.AddProperties(x => x.isSuccess, x => x.message, x => x.returnCode);
            }

            return source.AddObject("returnData", x => x.Model);
        }

        public static ITemplateBuilder<TModel> FromReturnResult<TModel>(
            this ITemplateBuilder<TModel> source,
            Func<IJsonTemplateBuilderContext<TModel>, ResultReturn> resultFactory)
        {
            using (var f = source.FromObject(resultFactory))
            {
                f.AddProperty("isSuccess", x => x.Model.IsSuccess, description: "本次操作是否成功")
                    .AddProperty("message", x => x.Model.Message.IfEmptyOrWhileSpace(x.Model.Error?.Message ?? ""), description: "本次操作的操作结果文本")
                    .AddProperty("returnCode", x => x.Model.ReturnCode, description: "操作结果代码");

                //f.AddProperties(x => x.IsSuccess, x => x.ReturnCode)
                //    .AddProperty("message", x => x.Model.Message.IfEmptyOrWhileSpace(x.Model.Error?.Message ?? ""), description: "结果文本消息");
            }

            return source.AddObject("returnData", x => x.Model,description:"输出的实际结果");
        }


        public static ITemplateBuilder<TNewElement> FromReturnArrayResult<TElement,TNewElement>(
            this ITemplateBuilder<IEnumerable<TElement>> source,
            Func<IJsonTemplateBuilderContext<IEnumerable<TElement>>, ResultReturn> resultFactory,
            Func<IJsonTemplateBuilderContext<IEnumerable<TElement>>, IEnumerable<TNewElement>> whereFunc ) 
        {
            using (var f = source.FromObject(resultFactory))
            {
                f.AddProperty("isSuccess", x => x.Model.IsSuccess, description: "本次操作是否成功")
                    .AddProperty("message", x => x.Model.Message.IfEmptyOrWhileSpace(x.Model.Error?.Message ?? ""), description: "本次操作的操作结果文本")
                    .AddProperty("returnCode", x => x.Model.ReturnCode, description: "操作结果代码");

                //f.AddProperties(x => x.IsSuccess, x => x.ReturnCode)
                //    .AddProperty("message", x => x.Model.Message.IfEmptyOrWhileSpace(x.Model.Error?.Message ?? ""), description: "结果文本消息");
            }

            return source.AddArrayObject<TNewElement>("returnData", x => whereFunc?.Invoke(x)??null, description: "输出的实际结果");
        }

        public static IArrayBuilder<TModel, TElement> FromPagedList<TModel, TElement>(this ITemplateBuilder<TModel> builder,
            [NotNull] Func<IJsonTemplateBuilderContext<TModel>, IPagedList<TElement>> valueFactory
            )
        {
            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            using (var b=builder.FromObject(valueFactory))
            {
                b.AddProperties(x => x.PageCount, x => x.PageSize, x => x.PageIndex, x => x.TotalCount);
            }
            
                
            return builder.AddArrayObject("Data", x => valueFactory(x).GetData(), description: "数据内容");
        } 
    }
}
