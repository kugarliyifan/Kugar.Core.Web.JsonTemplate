using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.JsonTemplate.Helpers;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate.Processors
{
    public class EnumProcessor:ISchemaProcessor
    {
        static readonly ConcurrentDictionary<Type, Tuple<string, object>[]> dict = new ConcurrentDictionary<Type, Tuple<string, object>[]>();
         public void Process(SchemaProcessorContext context)
         {
             var schema = context.Schema;
             if (context.Type.IsEnum)
             {
                 var items = GetTextValueItems(context.Type);
                 if (items.Length > 0)
                 {
                     string decription = string.Join(",", items.Select(f => $"{f.Item1}={f.Item2}"));
                     schema.Description = string.IsNullOrEmpty(schema.Description) ? decription : $"{schema.Description}:{decription}";
                 }
             }
             else if (context.Type.IsClass && context.Type != typeof(string))
             {
                 UpdateSchemaDescription(schema);
             }
         }
         private void UpdateSchemaDescription(JsonSchema schema)
         {
             if (schema.HasReference)
             {
                 var s = schema.ActualSchema;
                 if (s != null && s.Enumeration != null && s.Enumeration.Count > 0)
                 {
                     if (!string.IsNullOrEmpty(s.Description))
                     {
                         string description = $"【{s.Description}】";
                         if (string.IsNullOrEmpty(schema.Description) || !schema.Description.EndsWith(description))
                         {
                             schema.Description += description;
                         }
                     }
                 }
             }
 
             foreach (var key in schema.Properties.Keys)
             {
                 var s = schema.Properties[key];
                 UpdateSchemaDescription(s);
             }
         }
         /// <summary>
         /// 获取枚举值+描述  
         /// </summary>
         /// <param name="enumType"></param>
         /// <returns></returns>
         private Tuple<string, object>[] GetTextValueItems(Type enumType)
         {
             Tuple<string, object>[] tuples;
             if (dict.TryGetValue(enumType, out tuples) && tuples != null)
             {
                 return tuples;
             }
 
             FieldInfo[] fields = enumType.GetFields();
             List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
             foreach (var field in fields)
             {
                 //if (field.FieldType.IsEnum)
                 {
                     var key = "";

                     var v = field.GetValue(Activator.CreateInstance(enumType));

                     var attribute = field.GetCustomAttribute<DescriptionAttribute>();
                     if (attribute == null)
                     {
                         var node = ExpressionHelpers.XmlDoc.GetElementsByTagName("member").AsEnumerable<XmlElement>()
                             .Where(x => x.GetAttribute("name") == $"F:{field.DeclaringType.FullName}.{Enum.GetName(enumType,v)}")
                             .FirstOrDefault();

                         key = node.GetFirstElementsByTagName("summary").InnerText.Trim();
                     }
                     else
                     {
                         key = attribute?.Description ?? field.Name;
                     }

                     //int value = ((int)enumType.InvokeMember(field.Name, BindingFlags.GetField, null, null, null));
                     if (string.IsNullOrEmpty(key))
                     {
                         continue;
                     }
 
                     list.Add(new KeyValuePair<string, int>(key, (int)v));
                 }
             }
             tuples = list.Distinct((x,y)=>x.Value==y.Value).OrderBy(f => f.Value).Select(f => new Tuple<string, object>(f.Key, f.Value.ToString())).ToArray();
             dict.TryAdd(enumType, tuples);
             return tuples;
         }
    }
}
