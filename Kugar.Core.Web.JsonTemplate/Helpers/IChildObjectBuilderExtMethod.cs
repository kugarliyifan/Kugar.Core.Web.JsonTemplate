using System;
using System.Linq.Expressions;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.JsonTemplate.Builders;
using Newtonsoft.Json.Linq;

namespace Kugar.Core.Web.JsonTemplate.Helpers
{
    public static class IChildObjectBuilderExtMethod
    {
        public static IChildObjectBuilder<TModel> AddProperty<TModel, TValue>(
            this IChildObjectBuilder<TModel> builder,
            Expression<Func<TModel, TValue>> objectPropertyExp,
            string description = "",
            bool isNull = false,
            object example = null,
            string propertyName = null,
            Func<IJsonTemplateBuilderContext<TModel>, bool> ifCheckExp = null
        )
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                propertyName = ExpressionHelpers.GetExporessionPropertyName(objectPropertyExp);
            }

            //var callerReturnType = ExpressionHelpers.GetExprReturnType(objectPropertyExp);

            var invoker = objectPropertyExp.Compile();

            if (string.IsNullOrEmpty(description))
            {
                description = ExpressionHelpers.GetMemberDescription(ExpressionHelpers.GetMemberExpr(objectPropertyExp));
            }
            

            return builder.AddProperty(propertyName, (context) => invoker(context.Model), description, isNull, example, typeof(TValue),
                ifCheckExp);
        }

        public static IChildObjectBuilder<TModel> AddProperties<TModel>(
            this IChildObjectBuilder< TModel> builder,
            params Expression<Func<TModel, object>>[] objectPropertyExpList
        )
        {
            foreach (var item in objectPropertyExpList)
            {
                var returnType = ExpressionHelpers.GetExprReturnType(item);

                var propertyName = ExpressionHelpers.GetExporessionPropertyName(item);

                //var callerReturnType = ExpressionHelpers.GetExprReturnType(objectPropertyExp);

                var invoker = item.Compile();

                var description = ExpressionHelpers.GetMemberDescription(ExpressionHelpers.GetMemberExpr(item));


                builder.AddProperty(propertyName, (context) => invoker(context.Model), description, newValueType: returnType);
            }

            return builder;
        }

        public static IChildObjectBuilder< TNewObject> FromObject<TChildModel, TNewObject>(this IChildObjectBuilder<TChildModel> builder,
            Func<IJsonTemplateBuilderContext<TChildModel>, TNewObject> objectFactory
        )
        {
            return (IChildObjectBuilder<TNewObject>)new ChildJsonTemplateObjectBuilder<TChildModel, TNewObject>(builder,
                objectFactory,builder.SchemaBuilder,builder.Generator,builder.Resolver,false).Start();
        }

        public static IChildObjectBuilder<TModel> FromWechatPayProperties<TModel>(this IChildObjectBuilder<TModel> builder,
            Func<IJsonTemplateBuilderContext<TModel>, JObject> objectFactory)
        {
            using (var b=builder.FromObject(objectFactory))
            {
                b.AddProperty("appId",x=>x.Model.GetString("appId","",StringComparison.CurrentCultureIgnoreCase),"公众号/小程序的AppId")
                    .AddProperty("timeStamp",x=>x.Model.GetString("timeStamp","",StringComparison.CurrentCultureIgnoreCase),"支付时间戳")
                    .AddProperty("nonceStr",x=>x.Model.GetString("nonceStr","",StringComparison.CurrentCultureIgnoreCase),"")
                    .AddProperty("package",x=>x.Model.GetString("package","",StringComparison.CurrentCultureIgnoreCase),"")
                    .AddProperty("signType",x=>x.Model.GetString("signType","",StringComparison.CurrentCultureIgnoreCase),"签名类型")
                    .AddProperty("paySign",x=>x.Model.GetString("paySign","",StringComparison.CurrentCultureIgnoreCase),"签名")
                    .AddProperty("total_fee",x=>x.Model.GetString("total_fee","",StringComparison.CurrentCultureIgnoreCase),"支付金额",ifCheckExp:x=>x.Model.ContainsKey("total_fee"))
                    ;
            }

            return builder;
        }
    }
}