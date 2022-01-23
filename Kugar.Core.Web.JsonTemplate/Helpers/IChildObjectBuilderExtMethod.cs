using System;
using System.Linq.Expressions;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.JsonTemplate.Builders;
using Kugar.Core.Web.JsonTemplate.Exceptions;
using Newtonsoft.Json.Linq;

namespace Kugar.Core.Web.JsonTemplate.Helpers
{
    public static class IChildObjectBuilderExtMethod
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
        public static IChildObjectBuilder<TModel> AddProperty<TModel, TValue>(
            this IChildObjectBuilder<TModel> builder,
            Expression<Func<TModel, TValue>> objectPropertyExp,
            string description = "",
            bool isNull = false,
            object example = null,
            string newPropertyName = null,
            Func<IJsonTemplateBuilderContext<TModel>, bool> ifCheckExp = null
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

            if (string.IsNullOrEmpty(description))
            {
                description = ExpressionHelpers.GetMemberDescription(ExpressionHelpers.GetMemberExpr(objectPropertyExp));
            }

            var tmp = new PropertyExpInvoker<TModel, TValue>(newPropertyName, objectPropertyExp);

            return builder.AddProperty(newPropertyName, tmp.Invoke, description, isNull, example, typeof(TValue),
                ifCheckExp);


            //return builder.AddProperty(newPropertyName, (context) =>
            //    {
            //        var displayName = $"{context.PropertyName}.{newPropertyName}";

            //        context.PropertyName = newPropertyName;

            //        if (context.Model == null)
            //        {
            //            throw new OutputRenderException(context, "传入的Model为null")
            //            {
            //                DisplayPropertyPath = displayName
            //            };
            //        }

            //        return invoker(context.Model);
            //    }, description, isNull, example, typeof(TValue),
            //    ifCheckExp);
        }

        /// <summary>
        /// 添加多个属性
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="builder"></param>
        /// <param name="objectPropertyExpList">属性表达式</param>
        /// <returns></returns>
        public static IChildObjectBuilder<TModel> AddProperties<TModel>(
            this IChildObjectBuilder<TModel> builder,
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

                var tmp = new PropertyExpInvoker<TModel, object>(propertyName, item);
                
                builder.AddProperty(propertyName, tmp.Invoke, description, newValueType: returnType);
            }

            return builder;
        }

        /// <summary>
        /// 从objectFactory返回的对象中读取属性,与AddObject不同的是,FromObject不会添加新的属性,而是在当前对象中添加
        /// </summary>
        /// <typeparam name="TChildModel"></typeparam>
        /// <typeparam name="TNewObject"></typeparam>
        /// <param name="builder"></param>
        /// <param name="objectFactory"></param>
        /// <returns></returns>
        public static IChildObjectBuilder<TNewObject> FromObject<TChildModel, TNewObject>(this IChildObjectBuilder<TChildModel> builder,
            Func<IJsonTemplateBuilderContext<TChildModel>, TNewObject> objectFactory
        )
        {
            if (objectFactory == null)
            {
                throw new ArgumentNullException(nameof(objectFactory));
            }

            return (IChildObjectBuilder<TNewObject>)new ChildJsonTemplateObjectBuilder<TChildModel, TNewObject>(
                "",
                builder.DisplayPropertyName,
                builder,
                objectFactory, builder.SchemaBuilder, builder.Generator, builder.Resolver, isNewObject: false).Start();
        }




        /// <summary>
        /// 将json中微支付所需的参数输出
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="builder"></param>
        /// <param name="objectFactory"></param>
        /// <returns></returns>
        public static IChildObjectBuilder<TModel> FromWechatPayProperties<TModel>(this IChildObjectBuilder<TModel> builder,
            Func<IJsonTemplateBuilderContext<TModel>, JObject> objectFactory)
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
    }

    internal class PropertyExpInvoker<TModel, TValue>
    {
        private Func<TModel, TValue> _invoke = null;

        public string NewPropertyName { set; get; }

        public Expression<Func<TModel, TValue>> objectPropertyExp { set; get; }

        public PropertyExpInvoker(string newPropertyName, Expression<Func<TModel, TValue>> prop)
        {
            if (string.IsNullOrEmpty(newPropertyName))
            {
                NewPropertyName = ExpressionHelpers.GetExpressionPropertyName(objectPropertyExp);
            }

            if (prop==null)
            {
                throw new ArgumentNullException(nameof(prop));
            }

            _invoke = prop.Compile();

            
        }

        public TValue Invoke(IJsonTemplateBuilderContext<TModel> context)
        {
            var displayName = $"{context.PropertyName}.{NewPropertyName}";

            context.PropertyName = displayName;

            if (context.Model == null)
            {
                throw new OutputRenderException(context, "传入的Model为null")
                {
                    DisplayPropertyPath = displayName
                };
            }

            try
            {
                return _invoke(context.Model);
            }
            catch (Exception e)
            {
                throw new OutputRenderException(context, $"输出{displayName}出现错误", e)
                {
                    DisplayPropertyPath = displayName
                };
            }
            
        }
    }
}