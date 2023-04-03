using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml;
using Kugar.Core.ExtMethod;
using Kugar.Core.Log;
using Kugar.Core.Web.JsonTemplate.Helpers;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema;
using NSwag;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Kugar.Core.Web.JsonTemplate.Processors
{
    /// <summary>
    /// 用于当Action为FromBodyJson时,自动将参数生成的swagger转为json格式
    /// </summary>
    public class ActionParamtersToJsonBodyProcessor : IOperationProcessor
    {
        public bool Process(OperationProcessorContext context)
        {
            var c = (AspNetCoreOperationProcessorContext)context;

            if (c.ApiDescription.ActionDescriptor.EndpointMetadata.Any(x => x is FromBodyJsonAttribute) &&
                c.ApiDescription.ActionDescriptor.EndpointMetadata.Any(x => x is HttpPostAttribute) &&
                c.OperationDescription.Method == "post"
            )
            {

                //context.OperationDescription.Operation.Tags.Add("formbodyjson");
                //var lst = c.OperationDescription.Operation.Parameters.ToArrayEx();

                try
                {
                    if (!c.ApiDescription.ActionDescriptor.EndpointMetadata.Any(x => x is ConsumesAttribute))
                    {
                        c.OperationDescription.Operation.Consumes.Add("application/json-patch+json");
                        c.OperationDescription.Operation.Consumes.Add("application/json");
                        c.OperationDescription.Operation.Consumes.Add("application/*+json");
                    }

                    var args = context.Parameters.Where(x => isFromBody(x.Key)).ToArrayEx();

                    context.OperationDescription.Operation.Parameters.Remove(x => args.Any(y => y.Key.Name == x.Name));

                    var methodName = context.MethodInfo.DeclaringType.FullName + "." + context.MethodInfo.Name;

                    var methodXmlNode = ExpressionHelpers.XmlDoc?.GetElementsByTagName("member")
                        .AsEnumerable<XmlElement>()
                        .Where(x => x.GetAttribute("name").StartsWith($"M:{methodName}"))
                        .FirstOrDefault();

                    var paramXmlNodes =
                        (methodXmlNode?.GetElementsByTagName("param").AsEnumerable<XmlElement>().ToArrayEx()) ??
                        Array.Empty<XmlElement>();

                    var jsonSechma = new JsonSchema();

                    foreach (var p1 in args)
                    {
                        if (p1.Value == null)
                        {
                            continue;
                        }

                        var pschema = p1.Value.Schema;

                        var isRequired = p1.Value.IsRequired;

                        if (p1.Key.GetCustomAttribute<RequiredAttribute>() != null)
                        {
                            isRequired = true;
                        }

                        var description = p1.Value.Description;

                        if (paramXmlNodes.Any(x => x.GetAttribute("name") == p1.Value.Name))
                        {
                            var tmp = paramXmlNodes.FirstOrDefault(x => x.GetAttribute("name") == p1.Value.Name)
                                .InnerText;

                            if (tmp.Length > (description?.Length ?? 0))
                            {
                                description = tmp;
                            }
                        }

                        var prop = new JsonSchemaProperty()
                        {
                            Type = pschema?.Type ?? NSwagSchemeBuilder.NetTypeToJsonObjectType(p1.Key.ParameterType),
                            IsNullableRaw = (pschema?.IsNullableRaw ?? p1.Value.IsNullableRaw) ?? false,
                            IsRequired = isRequired,
                            Default = (pschema?.Default) ?? p1.Value.Default,
                            Description = description,
                            IsDeprecated = pschema?.IsDeprecated ?? p1.Value.IsDeprecated,
                            Example = p1.Value.Example, // pschema.Example,
                            Item = pschema?.Item ?? p1.Value.Item,
                            MaxItems = pschema.MaxItems,
                            MaxLength = pschema.MaxLength,
                            Maximum = pschema.Maximum,
                            MinLength = pschema.MinLength,
                            Minimum = pschema.Minimum,
                            MinItems = pschema.MinItems
                        };



                        if ((pschema?.Items ?? p1.Value.Items).HasData())
                        {
                            var lst = (pschema?.Items ?? p1.Value.Items);

                            foreach (var item in lst)
                            {
                                prop.Items.Add(item);
                            }
                        }

                        if ((pschema?.Properties ?? p1.Value.Properties).HasData())
                        {
                            var lst = (pschema?.Properties ?? p1.Value.Properties);

                            foreach (var item in lst)
                            {
                                prop.Properties.Add(item);
                            }
                        }

                        if ((pschema?.Enumeration ?? p1.Value.Enumeration).HasData())
                        {
                            var lst = (pschema?.Enumeration ?? p1.Value.Enumeration);

                            foreach (var item in lst)
                            {
                                prop.Enumeration.Add(item);
                            }
                        }

                        if ((pschema?.EnumerationNames ?? p1.Value.EnumerationNames).HasData())
                        {
                            var lst = (pschema?.EnumerationNames ?? p1.Value.EnumerationNames);

                            foreach (var item in lst)
                            {
                                prop.EnumerationNames.Add(item);
                            }
                        }

                        jsonSechma.Properties.Add(p1.Value.Name, prop);
                    }

                    jsonSechma.Title = context.OperationDescription.Operation.OperationId.Replace("/", "_") + "_" +
                                       "RequestParamter";

                    context.Document.Components.Schemas.Add(jsonSechma.Title, jsonSechma);

                    context.OperationDescription.Operation.RequestBody = new OpenApiRequestBody()
                    {
                        Description = "",
                        IsRequired = true,
                        Name = "body",
                        Position = 1,
                        Content =
                        {
                            ["application/json"] = new OpenApiMediaType()
                            {
                                Schema = new JsonSchema()
                                {
                                    Reference = jsonSechma,

                                }
                            }
                        }
                    };

                    var p = new OpenApiParameter();
                    p.Name = "body";
                    p.Kind = OpenApiParameterKind.Body;
                    p.Type = JsonObjectType.Object;
                    p.Schema = jsonSechma;
                    p.Reference = jsonSechma;
                    p.IsRequired = true;
                    //opt.Operation.Parameters.Add( doc.Document.Operations.Where(x=>x.Path.Contains("Add")).FirstOrDefault().Operation.Parameters[0].);

                    context.OperationDescription.Operation.Parameters.Add(p);
                }
                catch (ArgumentException e)
                {
                    LoggerManager.Default.Error($"生成{context.OperationDescription.Operation.OperationId}函数的文档出错");
                    throw new Exception($"生成{context.OperationDescription.Operation.OperationId}函数的文档出错", e);
                }
                catch (Exception e)
                {
                    Debugger.Break();
                    Console.WriteLine(context.OperationDescription.Operation.OperationId);
                    throw ;
                }

                
            }
            return true;
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