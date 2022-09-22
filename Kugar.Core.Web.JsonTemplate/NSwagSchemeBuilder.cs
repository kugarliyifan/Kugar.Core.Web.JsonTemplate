using System;
using System.Collections.Generic;
using Kugar.Core.ExtMethod;
using NJsonSchema;

namespace Kugar.Core.Web.JsonTemplate
{
    public class NSwagSchemeBuilder
    {
        private IDictionary<string, JsonSchemaProperty> _properties = null;
        private Func<string, string> _getPropertyTitle = null;


        public NSwagSchemeBuilder(JsonSchema schema,
            Func<string, string> getPropertyTitle)
        {
            //_propertyName = propertyName;
            Schema = schema;

            _properties = schema.Properties;

            _getPropertyTitle = getPropertyTitle ?? throw new ArgumentNullException(nameof(getPropertyTitle));

        }

        public NSwagSchemeBuilder AddSingleProperty(string name, JsonObjectType type, string desciption,
            object example = null, bool nullable = false)
        {
            var realName = _getPropertyTitle(name);

            if (_properties.ContainsKey(realName))
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"参数名:{realName}重复");
            }

            _properties.Add(realName, new JsonSchemaProperty()
            {
                Type = type,
                Description = desciption,
                Example = example,
                IsNullableRaw = nullable
            });

            return this;
        }

        public NSwagSchemeBuilder AddObjectProperty(string name, string desciption, bool nullable = false)
        {
            var realName = _getPropertyTitle(name);

            if (_properties.ContainsKey(realName))
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"参数名:{realName}重复");
            }

            var p = new JsonSchemaProperty();
            p.Type = JsonObjectType.Object;
            p.Description = desciption;
            p.IsNullableRaw = nullable;
            _properties.Add(realName, p);

            return new NSwagSchemeBuilder(p, _getPropertyTitle);
        }

        public NSwagSchemeBuilder AddObjectArrayProperty(string name, string desciption, bool nullable = false)
        {
            var realName = _getPropertyTitle(name);

            if (_properties.ContainsKey(realName))
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"参数名:{realName}重复");
            }

            var p = new JsonSchemaProperty();
            p.Type = JsonObjectType.Array;
            p.Description = desciption;
            p.Item = new JsonSchema();
            p.IsNullableRaw = nullable;
            _properties.Add(realName, p);


            return new NSwagSchemeBuilder(p.Item, _getPropertyTitle);
        }

        public NSwagSchemeBuilder AddValueArray(string name, JsonObjectType type, string desciption = "",
            bool nullable = false)
        {
            var realName = _getPropertyTitle(name);

            if (_properties.ContainsKey(realName))
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"参数名:{realName}重复");
            }

            var p = new JsonSchemaProperty();
            p.Type = JsonObjectType.Array;
            p.Description = desciption;
            p.IsNullableRaw = nullable;
            p.Item = new JsonSchema()
            {
                Type = type
            };
            _properties.Add(realName, p);


            return this;
        }

        public JsonSchema Schema { get; }

        public  static JsonObjectType NetTypeToJsonObjectType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
            }

            if (type == typeof(int) ||
                type == typeof(byte) ||
                type == typeof(short) ||
                type == typeof(long) ||
                type == typeof(uint) ||
                type == typeof(ushort) ||
                type == typeof(ulong) ||
                type.IsEnum
            )
            {
                return JsonObjectType.Integer;
            }
            else if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            {
                return JsonObjectType.Number;
            }
            else if (type == typeof(string))
            {
                return JsonObjectType.String;
            }
            else if (type == typeof(bool))
            {
                return JsonObjectType.Boolean;
            }
            else if (type.IsIEnumerable())
            {
                return JsonObjectType.Array;
            }
            else if (type == typeof(DateTime))
            {
                return JsonObjectType.String;
            }
            else if (type == typeof(Guid))
            {
                return JsonObjectType.String;
            }
            else
            {
                return JsonObjectType.Object;
            }
        }

        public string GetFormatPropertyName(string name)
        {
            return _getPropertyTitle?.Invoke(name) ?? name;
        }
    }
}