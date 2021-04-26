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
        public static IChildObjectBuilder<TRootModel,TModel> FromReturnResult<TRootModel,TModel>(this IObjectBuilder<TRootModel,TModel> source,Func<IJsonTemplateBuilderContext<TRootModel,TModel>, bool> resultFactory)
        {
            source.AddProperty("isSuccess", resultFactory,"本次操作是否成功")
                .AddProperty("message", x => string.Empty,"操作结果文本")
                .AddProperty("returnCode", x => 0, "返回的执行结果代码", example: 0)
                ;

            return source.AddObject("returnData", x => x.Model);
        }

        public static IChildObjectBuilder<TRootModel,TModel> FromReturnResult<TRootModel,TModel>(this IObjectBuilder<TRootModel, TModel> source,
            Func<IJsonTemplateBuilderContext<TRootModel,TModel>, (bool isSuccess, string message)> resultFactory)
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

        public static IChildObjectBuilder<TRootModel,TModel> FromReturnResult<TRootModel,TModel>(
            this IObjectBuilder<TRootModel,TModel> source,
            Func<IJsonTemplateBuilderContext<TRootModel,TModel>, (bool isSuccess, int returnCode, string message)> resultFactory)
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

        public static IChildObjectBuilder<TRootModel,TModel> FromReturnResult<TRootModel,TModel>(
            this IObjectBuilder<TRootModel,TModel> source,
            Func<IJsonTemplateBuilderContext<TRootModel,TModel>, ResultReturn> resultFactory)
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

        public static IArrayBuilder<TRootModel,TElement> FromPagedList<TRootModel,TModel, TElement>(this IObjectBuilder<TRootModel,TModel> builder,
            [NotNull] Func<IJsonTemplateBuilderContext<TRootModel,TModel>, IPagedList<TElement>> valueFactory
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

        public static IArrayBuilder<TRootModel,TElement> FromPagedList<TRootModel,TElement>(
            this IObjectBuilder<TRootModel,IPagedList<TElement>> builder)
        {
            return FromPagedList(builder, x => x.Model);
        }

        public static IArrayBuilder<TRootModel,TElement> FromPagedList<TRootModel,TModel, TElement>(this IChildObjectBuilder<TRootModel,TModel> builder,
            [NotNull] Func<IJsonTemplateBuilderContext<TRootModel,TModel>, IPagedList<TElement>> valueFactory
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

        public static IArrayBuilder<TRootModel,TElement> FromPagedList<TRootModel,TElement>(
            this IChildObjectBuilder<TRootModel,IPagedList<TElement>> builder)
        {
            return FromPagedList(builder, x => x.Model);
        }
    }
}
