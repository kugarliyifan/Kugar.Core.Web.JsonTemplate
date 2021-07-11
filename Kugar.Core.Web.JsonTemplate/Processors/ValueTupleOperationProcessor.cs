using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kugar.Core.Web.JsonTemplate.Attributes;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema;
using NSwag;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Kugar.Core.Web.JsonTemplate.Processors
{
    /// <summary>
    /// 将ValueTuple转换为object类型参数
    /// </summary>
    public class ValueTupleOperationProcessor : IOperationProcessor
    {
        public bool Process(OperationProcessorContext context)
        {
            var c = (AspNetCoreOperationProcessorContext)context;

            if (c.ApiDescription.ActionDescriptor.EndpointMetadata.Any(x => x is FromBodyJsonAttribute) &&
                c.ApiDescription.ActionDescriptor.EndpointMetadata.Any(x => x is HttpPostAttribute) &&
                c.OperationDescription.Method == "post"
            )
            {
                var paramters = context.Parameters;

                if (!paramters.Any(x => x.Key.ParameterType.Name.StartsWith("ValueTuple`")))
                {
                    return true;
                }

                foreach (var parameter in paramters)
                {
                    if (!isFromBody(parameter.Key))
                    {
                        continue;
                    }

                    if (!parameter.Key.ParameterType.Name.StartsWith("ValueTuple`"))
                    {
                        continue;
                    }

                    var tupleElement = parameter.Key.GetCustomAttribute<TupleElementNamesAttribute>();

                    var valueTupleArgsTypes = parameter.Key.ParameterType.GetGenericArguments();

                    var p1 = valueTupleToJsonProperties(tupleElement, valueTupleArgsTypes).Properties;

                    parameter.Value.Schema.Type = JsonObjectType.Object;
                    parameter.Value.Kind = OpenApiParameterKind.Body;
                    parameter.Value.Schema.Properties.Clear();

                    var attrs = parameter.Key.GetCustomAttributes(typeof(ValueTupleDescroptionAttribute)).Select(x=>x as ValueTupleDescroptionAttribute).ToArray();
                    
                    foreach (var property in p1)
                    {
                        property.Value.Description =(attrs?.FirstOrDefault(x=>x.Name==property.Key)?.Description)??"";

                        parameter.Value.Schema.Properties.Add(property);
                        
                    }

                }
            }

            return true;
        }

        private JsonSchemaProperty valueTupleToJsonProperties(TupleElementNamesAttribute attr,
            Type[] argsType)
        {
            var valueTupleNames = attr.TransformNames;

            var valueTupleArgsTypes = argsType;

            var jp = new JsonSchemaProperty();
            jp.Type = JsonObjectType.Object;

            for (int i = 0; i < valueTupleArgsTypes.Length; i++)
            {
                jp.Properties.Add(valueTupleNames[i], new JsonSchemaProperty()
                {
                    Type = NSwagSchemeBuilder.NetTypeToJsonObjectType(valueTupleArgsTypes[i]),
                    Description = "",
                });
            }

            return jp;
        }

        private bool isFromBody(ParameterInfo parameter)
        {
            return !parameter.GetCustomAttributes().Select(x => x.GetType())
                .Any(x => x == typeof(FromQueryAttribute) ||
                          x == typeof(FromRouteAttribute) ||
                          x == typeof(FromHeaderAttribute) ||
                          x == typeof(FromServicesAttribute));
        }

    }
}