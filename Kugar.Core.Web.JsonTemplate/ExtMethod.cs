using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml;
using Kugar.Core.ExtMethod;
using NJsonSchema;

namespace Kugar.Core.Web.JsonTemplate
{
    public static class ExtMethod
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

        public static IArrayBuilder<TModel> AddProperty<TModel, TValue>(
            this IArrayBuilder<TModel> builder,
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

        public static IArrayBuilder<TModel> AddProperties<TModel>(
            this IArrayBuilder<TModel> builder,
            Expression<Func<TModel, object>>[] objectPropertyExpList
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

        public static IChildObjectBuilder< TNewObject> FromObject<TChildModel, TNewObject>(this IChildObjectBuilder<TChildModel> builder,
            Func<IJsonTemplateBuilderContext<TChildModel>, TNewObject> objectFactory
        )
        {
            return (IChildObjectBuilder<TNewObject>)new ChildJsonTemplateObjectBuilder<TChildModel, TNewObject>(builder,
                objectFactory,builder.SchemaBuilder,builder.Generator,builder.Resolver,false).Start();
        }
    }

    internal static class ExpressionHelpers
    {
        private static Dictionary<string,string> _typeXmlDesc=new Dictionary<string, string>();

        public static void InitXMl(Type type)
        {
            readXmlFile(type, _typeXmlDesc);
        }

        public static string GetExporessionPropertyName<T1, T2>(Expression<Func<T1, T2>> expression)
        {
            if (expression.Body.NodeType == ExpressionType.Convert)
            {
                var p = expression.Body as UnaryExpression;

                if (p.Operand is MemberExpression m1)
                {
                    return m1.Member.Name;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(expression), $"表达式{expression.Body.ToString()}无效");
                }

            }
            else if (expression.Body is MemberExpression m)
            {
                return m.Member.Name;
            }

            throw new ArgumentException();

        }

        public static Type GetExprReturnType<T1, T2>(Expression<Func<T1, T2>> expression)
        {
            if (expression.Body.NodeType == ExpressionType.Convert)
            {
                var p = expression.Body as UnaryExpression;

                if (p.Operand is MemberExpression m1)
                {
                    if (m1.Member is PropertyInfo p1)
                    {
                        return p1.PropertyType;
                    }
                    else if (m1.Member is FieldInfo f1)
                    {
                        return f1.FieldType;
                    }
                    else if (m1.Member is MethodInfo m2)
                    {
                        return m2.ReturnType;
                    }
                }
                else if (p.Operand.NodeType == ExpressionType.Invoke)
                {
                    return p.Operand.Type;
                }

                throw new ArgumentOutOfRangeException(nameof(expression), $"表达式{expression.Body.ToString()}无效");
            }
            else if (expression.Body is MemberExpression m)
            {
                if (m.Member is PropertyInfo p1)
                {
                    return p1.PropertyType;
                }
                else if (m.Member is FieldInfo f1)
                {
                    return f1.FieldType;
                }
                else if (m.Member is MethodInfo m2)
                {
                    return m2.ReturnType;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(expression), $"表达式{expression.Body.ToString()}无效");
        }

        public static MemberExpression GetMemberExpr<T1, T2>(Expression<Func<T1, T2>> expression)
        {
            if (expression.Body.NodeType == ExpressionType.Convert)
            {
                var p = expression.Body as UnaryExpression;

                if (p.Operand is MemberExpression m1)
                {
                    return m1;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(expression), $"表达式{expression.Body.ToString()}无效");
                }

            }
            else if (expression.Body is MemberExpression m)
            {
                return m;
            }

            throw new ArgumentException();
        }

        public static string GetMemberDescription(MemberExpression expr)
        {
            var propertyName =$"{expr.Member.DeclaringType.Namespace}.{expr.Member.DeclaringType.Name}.{expr.Member.Name}";

            var desciption = _typeXmlDesc.TryGetValue( propertyName, "");

            if (string.IsNullOrWhiteSpace(desciption))
            {
                desciption = expr.Member.GetCustomAttribute<DescriptionAttribute>()?.Description;
            }

            if (string.IsNullOrWhiteSpace(desciption))
            {
                desciption = expr.Member.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
            }
            
            return desciption;
        }

        private static void readXmlFile(Type type, Dictionary<string,string> dic)
        {
            //var type = typeof(T);

            var xmlFilePath = Path.Join( Path.GetDirectoryName(type.Assembly.Location),Path.GetFileNameWithoutExtension(type.Assembly.Location)+".xml");

            if (!File.Exists(xmlFilePath))
            {
                return;
            }


            var xml=new XmlDocument();

            try
            {
                xml.Load(xmlFilePath);
            }
            catch (Exception e)
            {
                return;
            }

            var l1 = xml.GetElementsByTagName("member").AsEnumerable<XmlElement>();

            var lst= l1
                .Where(x => x.GetAttribute("name").StartsWith($"P:{type.FullName}"))
                .Select(x =>
                    new KeyValuePair<string, string>(
                        x.GetAttribute("name").Substring($"P:{type.FullName}".Length + 1).ToStringEx(),
                        x.GetElementsByTagName("summary").AsEnumerable<XmlElement>().FirstOrDefault()?.InnerText.ToStringEx()))
                .ToArrayEx();

            if (!lst.HasData())
            {
                return;
            }

            foreach (var item in lst)
            {
                var key = $"{type.FullName}.{item.Key}";

                if (dic.ContainsKey(key))
                {
                    continue;
                }
                dic.Add(key,item.Value.ToStringEx().Trim());
            }

            if (!type.IsInterface && type.BaseType!=null)
            {
                if (type.BaseType != null && type.BaseType != typeof(object))
                {
                    readXmlFile(type.BaseType, dic);
                }
            }

            if (type!=null)
            {
                foreach (var face in type.GetInterfaces())
                {
                    readXmlFile(face, dic);
                }
            }
            
        }
    }
}
