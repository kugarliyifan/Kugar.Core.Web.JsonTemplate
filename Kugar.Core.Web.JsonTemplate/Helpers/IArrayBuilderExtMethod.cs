﻿using System;
using System.Linq.Expressions;
using Kugar.Core.Web.JsonTemplate.Builders;

namespace Kugar.Core.Web.JsonTemplate.Helpers
{
    public static class IArrayBuilderExtMethod
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
        public static IArrayBuilder<TRootModel,TModel> AddProperty<TRootModel,TModel, TValue>(
            this IArrayBuilder<TRootModel,TModel> builder,
            Expression<Func<TModel, TValue>> objectPropertyExp,
            string description = "",
            bool isNull = false,
            object example = null,
            string newPropertyName = null,
            Func<IJsonTemplateBuilderContext<TRootModel,TModel>, bool> ifCheckExp = null
            )
        {
            if (objectPropertyExp==null)
            {
                throw new ArgumentNullException(nameof(objectPropertyExp));
            }

            if (string.IsNullOrEmpty(newPropertyName))
            {
                newPropertyName = ExpressionHelpers.GetExporessionPropertyName(objectPropertyExp);
            }

            //var callerReturnType = ExpressionHelpers.GetExprReturnType(objectPropertyExp);

            var invoker = objectPropertyExp.Compile();

            if (string.IsNullOrEmpty(description))
            {
                description = ExpressionHelpers.GetMemberDescription(ExpressionHelpers.GetMemberExpr(objectPropertyExp));
            }

            return builder.AddProperty(newPropertyName, (context) => invoker(context.Model), description, isNull, example,typeof(TValue),
                ifCheckExp);
        }

        /// <summary>
        /// 添加多个属性
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="builder"></param>
        /// <param name="objectPropertyExpList">属性表达式</param>
        /// <returns></returns>
        public static IArrayBuilder<TRootModel,TModel> AddProperties<TRootModel,TModel>(
            this IArrayBuilder<TRootModel,TModel> builder,
            params Expression<Func<TModel, object>>[] objectPropertyExpList
            )
        {
            foreach (var item in objectPropertyExpList)
            {
                var returnType = ExpressionHelpers.GetExprReturnType(item);

                var propertyName=ExpressionHelpers.GetExporessionPropertyName(item);

                //var callerReturnType = ExpressionHelpers.GetExprReturnType(objectPropertyExp);

                var invoker = item.Compile();

                var description = ExpressionHelpers.GetMemberDescription(ExpressionHelpers.GetMemberExpr(item));
                

                builder.AddProperty(propertyName, (context) => invoker(context.Model), description,newValueType:returnType);
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
        public static IChildObjectBuilder<TRootModel,TNewObject> FromObject<TRootModel,TModel, TNewObject>(this IArrayBuilder<TRootModel,TModel> builder,
            Func<IJsonTemplateBuilderContext<TRootModel,TModel>, TNewObject> objectFactory
        )
        {
            return (IChildObjectBuilder<TRootModel,TNewObject>)new ChildJsonTemplateObjectBuilder<TRootModel,TModel, TNewObject>(builder,
                objectFactory,builder.SchemaBuilder,builder.Generator,builder.Resolver,false).Start();
        }

        
    }
}
