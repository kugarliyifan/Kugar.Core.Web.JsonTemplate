using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using Kugar.Core.ExtMethod;

namespace Kugar.Core.Web.JsonTemplate.Helpers
{
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

        public static XmlDocument XmlDoc { set; get; }

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

                XmlDoc = xml;
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