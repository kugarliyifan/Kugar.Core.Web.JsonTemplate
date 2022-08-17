using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.JsonTemplate.Builders;
using Kugar.Core.Web.JsonTemplate.Invokers;
using Newtonsoft.Json.Linq;

namespace Kugar.Core.Web.JsonTemplate.Helpers
{
    public static class IObjectBuilderExtMethod
    {
        /// <summary>
        /// 添加一个值属性
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="builder"></param>
        /// <param name="objectPropertyExp">属性表达式</param>
        /// <param name="description">描述,如果为空,则为objectPropertyExp设置的属性的备注或DescriptionAttribute对应值</param>
        /// <param name="isNull">是否允许为null</param>
        /// <param name="example">值示例</param>
        /// <param name="newPropertyName">新属性名,如果需要修改objectPropertyExp输出的属性,则传入该参数</param>
        /// <param name="ifCheckExp">传入一个回调,用于判断在运行时是否添加该属性</param>
        /// <returns></returns>
        public static ITemplateBuilder<TRootModel, TModel> AddProperty<TRootModel, TModel, TValue>(
            this ITemplateBuilder<TRootModel, TModel> builder,
            Expression<Func< TModel, TValue>> objectPropertyExp,
            string description = "",
            bool isNull = false,
            object example = null,
            string newPropertyName = null,
            Func<IJsonTemplateBuilderContext<TRootModel, TModel>, bool> ifCheckExp = null
        )
        {
            if (objectPropertyExp == null)
            {
                throw new ArgumentNullException(nameof(objectPropertyExp));
            }

            if (string.IsNullOrEmpty(newPropertyName))
            {
                newPropertyName = ExpressionHelpers.GetExpressionPropertyName(objectPropertyExp);
            }

            //var callerReturnType = ExpressionHelpers.GetExprReturnType(objectPropertyExp);

            //var invoker = objectPropertyExp.Compile();

            if (string.IsNullOrEmpty(description))
            {
                description = ExpressionHelpers.GetMemberDescription(ExpressionHelpers.GetMemberExpr(objectPropertyExp));
            }

            var tmp = new PropertyExpInvoker<TRootModel, TModel, TValue>(newPropertyName, objectPropertyExp);

            return builder.AddProperty(newPropertyName, tmp.Invoke, description, isNull, example, typeof(TValue),
                ifCheckExp);
        }

        /// <summary>
        /// 添加多个属性
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="builder"></param>
        /// <param name="objectPropertyExpList">属性表达式</param>
        /// <returns></returns>
        public static ITemplateBuilder<TRootModel, TModel> AddProperties<TRootModel, TModel>(
            this ITemplateBuilder<TRootModel, TModel> builder,
            params Expression<Func<TModel, object>>[] objectPropertyExpList
        )
        {
            foreach (var item in objectPropertyExpList)
            {
                var returnType = ExpressionHelpers.GetExprReturnType(item);

                var propertyName = ExpressionHelpers.GetExpressionPropertyName(item);

                //var callerReturnType = ExpressionHelpers.GetExprReturnType(objectPropertyExp);

                //var invoker = item.Compile();

                var description = ExpressionHelpers.GetMemberDescription(ExpressionHelpers.GetMemberExpr(item));

                var tmp = new PropertyExpInvoker<TRootModel, TModel, object>(propertyName, item);

                builder.AddProperty(propertyName, tmp.Invoke, description, newValueType: returnType);

            }

            return builder;
        }

        /// <summary>
        /// 从objectFactory返回的对象中读取属性,与AddObject不同的是,FromObject不会添加新的属性,而是在当前对象中添加
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TNewObject"></typeparam>
        /// <param name="builder"></param>
        /// <param name="objectFactory"></param>
        /// <returns></returns>
        public static ITemplateBuilder<TRootModel, TNewObject> FromObject<TRootModel, TModel, TNewObject>(this ITemplateBuilder<TRootModel, TModel> builder,
            Func<IJsonTemplateBuilderContext<TRootModel, TModel>, TNewObject> objectFactory
        )
        {
            if (objectFactory == null)
            {
                throw new ArgumentNullException(nameof(objectFactory));
            }

            return new ChildJsonTemplateObjectBuilder<TRootModel, TModel, TNewObject>("","", builder,
                objectFactory, builder.SchemaBuilder, builder.Generator, builder.Resolver, isNewObject: false).Start();
        }

        /// <summary>
        /// 将json中微支付所需的参数输出
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="builder"></param>
        /// <param name="objectFactory"></param>
        /// <returns></returns>
        public static ITemplateBuilder<TRootModel, TModel> FromWechatPayProperties<TRootModel, TModel>(this ITemplateBuilder<TRootModel, TModel> builder,
            Func<IJsonTemplateBuilderContext<TRootModel, TModel>, JObject> objectFactory)
        {
            if (objectFactory == null)
            {
                throw new ArgumentNullException(nameof(objectFactory));
            }

            using (var b = builder.FromObject(objectFactory))
            {
                b.AddProperty("appId", x => x.Model.GetString("appId", "", StringComparison.CurrentCultureIgnoreCase), "公众号/小程序的AppId")
                    .AddProperty("timeStamp", x => x.Model.GetString("timeStamp", "", StringComparison.CurrentCultureIgnoreCase), "支付时间戳")
                    .AddProperty("nonceStr", x => x.Model.GetString("nonceStr", "", StringComparison.CurrentCultureIgnoreCase), "")
                    .AddProperty("package", x => x.Model.GetString("package", "", StringComparison.CurrentCultureIgnoreCase), "")
                    .AddProperty("signType", x => x.Model.GetString("signType", "", StringComparison.CurrentCultureIgnoreCase), "签名类型")
                    .AddProperty("paySign", x => x.Model.GetString("paySign", "", StringComparison.CurrentCultureIgnoreCase), "签名")
                    .AddProperty("total_fee", x => x.Model.GetString("total_fee", "", StringComparison.CurrentCultureIgnoreCase), "支付金额", ifCheckExp: x => x.Model.ContainsKey("total_fee"))
                    ;
            }

            return builder;
        }

        /// <summary>
        /// 添加一个object并直接添加进指定的属性,并且不需要使用using的方式
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TChildModel"></typeparam>
        /// <param name="builder"></param>
        /// <param name="propName"></param>
        /// <param name="objectValueFunc"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static ITemplateBuilder<TRootModel, TModel> AddObjectProperties<TRootModel,TModel, TChildModel>(
            this ITemplateBuilder<TRootModel, TModel> builder,
            string propName,
            Func<IJsonTemplateBuilderContext<TRootModel,TModel>, TChildModel> objectValueFunc,
            params Expression<Func<TChildModel, object>>[] properties)
        {
            using (var b = builder.AddObject(propName, objectValueFunc))
            {
                b.AddProperties(properties);
            }

            return builder;
        }

        /// <summary>
        /// 添加一个数组对象,并添加指定属性,且无需使用using
        /// </summary>
        /// <typeparam name="TRootModel"></typeparam>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TNewElement"></typeparam>
        /// <param name="builder"></param>
        /// <param name="propName"></param>
        /// <param name="objectValueFunc"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static ITemplateBuilder<TRootModel, TModel> AddArrayObjectProperties<TRootModel, TModel,TNewElement>(
            this ITemplateBuilder<TRootModel, TModel> builder,
            string propName,
            Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TNewElement>> objectValueFunc,
            params Expression<Func<TNewElement, object>>[] properties
        )
        {
            using (var b = builder.AddArrayObject(propName, objectValueFunc))
            {
                b.AddProperties(properties);
            }

            return builder;
        }
    }
}