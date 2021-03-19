using System;
using System.Linq.Expressions;
using Kugar.Core.Web.JsonTemplate.Builders;

namespace Kugar.Core.Web.JsonTemplate.Helpers
{
    public static class IObjectBuilderExtMethod
    {
        public static IObjectBuilder<TModel> AddProperty<TModel, TValue>(
            this IObjectBuilder<TModel> builder,
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

            return builder.AddProperty(propertyName, (context) => invoker(context.Model), description, isNull, example,typeof(TValue),
                ifCheckExp);
        }

        public static IObjectBuilder<TModel> AddProperties<TModel>(
            this IObjectBuilder<TModel> builder,
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

        public static IChildObjectBuilder<TNewObject> FromObject<TModel, TNewObject>(this IObjectBuilder<TModel> builder,
            Func<IJsonTemplateBuilderContext<TModel>, TNewObject> objectFactory
        )
        {
            return (IChildObjectBuilder<TNewObject>)new ChildJsonTemplateObjectBuilder<TModel, TNewObject>(builder,
                objectFactory,builder.SchemaBuilder,builder.Generator,builder.Resolver,false).Start();
        }
    }
}