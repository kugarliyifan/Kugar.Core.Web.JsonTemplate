using System.Linq;
using System.Reflection;
using Kugar.Core.ExtMethod;
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

                if (!c.ApiDescription.ActionDescriptor.EndpointMetadata.Any(x => x is ConsumesAttribute))
                {
                    c.OperationDescription.Operation.Consumes.Add("application/json-patch+json");
                    c.OperationDescription.Operation.Consumes.Add("application/json");
                    c.OperationDescription.Operation.Consumes.Add("application/*+json");
                }

                var args = context.Parameters.Where(x=>isFromBody(x.Key)).ToArrayEx();

                context.OperationDescription.Operation.Parameters.Remove(x=>args.Any(y=>y.Key.Name==x.Name));

                var jsonSechma = new JsonSchema();

                foreach (var p1 in args)
                {
                    var pschema = p1.Value.Schema;

                    var prop = new JsonSchemaProperty()
                    {
                        Type = pschema?.Type ?? NSwagSchemeBuilder.NetTypeToJsonObjectType(p1.Key.ParameterType),
                        IsNullableRaw = pschema?.IsNullableRaw ?? p1.Value.IsNullableRaw,
                        IsRequired = p1.Value.IsRequired,
                        Default = pschema?.Default ?? p1.Value.Default,
                        Description = p1.Value.Description,
                        IsDeprecated = pschema?.IsDeprecated ?? p1.Value.IsDeprecated,
                        Example = p1.Value.Example,// pschema.Example,
                        Item = pschema?.Item ?? p1.Value.Item
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

                    jsonSechma.Properties.Add(p1.Value.Name, prop);
                }

                jsonSechma.Title = context.OperationDescription.Operation.OperationId.Replace("/", "_") + "_" + "RequestParamter";

                context.Document.Components.Schemas.Add(jsonSechma.Title, jsonSechma);

                context.OperationDescription.Operation.RequestBody = new OpenApiRequestBody()
                {
                    Description = "",
                    IsRequired = true,
                    Name = "body",
                    Position = 1,
                    Content =
                    {
                        ["application/json"]=new OpenApiMediaType()
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