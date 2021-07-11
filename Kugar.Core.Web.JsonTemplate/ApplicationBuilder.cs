using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Fasterflect;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.JsonTemplate.Helpers;
using Kugar.Core.Web.JsonTemplate.Processors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation.TypeMappers;
using NSwag.Generation.AspNetCore;

namespace Kugar.Core.Web.JsonTemplate
{
    public static class ApplicationBuilderExtMethod
    {
        public static void AddJsonTemplateV2(this AspNetCoreOpenApiDocumentGeneratorSettings opt,
            params Assembly[] typeAssemblies)
        {

            var types = typeAssemblies.SelectMany(x => x.GetTypes())
                .Where(x => x.IsImplementlInterface(typeof(IJsonTemplateObject)) && !x.IsAbstract &&
                            x.IsPublic)
                .ToArrayEx();

            foreach (var c in typeAssemblies.SelectMany(x => x.GetTypes()).Concat(typeof(ResultReturn).Assembly.GetTypes()))
            {
                ExpressionHelpers.InitXMl(c);
            }


            foreach (var t in types)
            {
                opt.TypeMappers.Add(new ObjectTypeMapper(t, (gen, resolver) =>
                {
                    var builder = GlobalJsonTemplateCache.GetTemplateInfo(t);

                    return builder.SchemaBuilder.Schema;

                }));
            }

            opt.OperationProcessors.Add(new ValueTupleOperationProcessor());
            //opt.OperationProcessors.Add(new ValueTupleOperationProcessor());
            //opt.OperationProcessors.Add(new OperationProcessor((context) =>
            //{
            //    var c = (AspNetCoreOperationProcessorContext)context;

            //    if (c.ApiDescription.ActionDescriptor.EndpointMetadata.Any(x => x is FromBodyJsonAttribute) &&
            //        c.ApiDescription.ActionDescriptor.EndpointMetadata.Any(x => x is HttpPostAttribute) &&
            //        c.OperationDescription.Method == "post"
            //        )
            //    {

            //        //context.OperationDescription.Operation.Tags.Add("formbodyjson");
            //        //var lst = c.OperationDescription.Operation.Parameters.ToArrayEx();

            //        if (!c.ApiDescription.ActionDescriptor.EndpointMetadata.Any(x => x is ConsumesAttribute))
            //        {
            //            c.OperationDescription.Operation.Consumes.Add("application/json-patch+json");
            //            c.OperationDescription.Operation.Consumes.Add("application/json");
            //            c.OperationDescription.Operation.Consumes.Add("application/*+json");
            //        }

            //        var args = context.Parameters.ToArrayEx();

            //        context.OperationDescription.Operation.Parameters.Clear();

            //        var jsonSechma = new JsonSchema();

            //        foreach (var p1 in args)
            //        {
            //            var pschema = p1.Value.Schema;

            //            var prop = new JsonSchemaProperty()
            //            {
            //                Type = pschema?.Type ?? NSwagSchemeBuilder.NetTypeToJsonObjectType(p1.Key.ParameterType),
            //                IsNullableRaw = pschema?.IsNullableRaw ?? p1.Value.IsNullableRaw,
            //                IsRequired = p1.Value.IsRequired,
            //                Default = pschema?.Default ?? p1.Value.Default,
            //                Description = p1.Value.Description,
            //                IsDeprecated = pschema?.IsDeprecated ?? p1.Value.IsDeprecated,
            //                Example = p1.Value.Example,// pschema.Example,
            //                Item = pschema?.Item ?? p1.Value.Item
            //            };

            //            if ((pschema?.Items ?? p1.Value.Items).HasData())
            //            {
            //                var lst = (pschema?.Items ?? p1.Value.Items);

            //                foreach (var item in lst)
            //                {
            //                    prop.Items.Add(item);
            //                }
            //            }

            //            if ((pschema?.Properties ?? p1.Value.Properties).HasData())
            //            {
            //                var lst = (pschema?.Properties ?? p1.Value.Properties);

            //                foreach (var item in lst)
            //                {
            //                    prop.Properties.Add(item);
            //                }
            //            }

            //            jsonSechma.Properties.Add(p1.Value.Name, prop);
            //        }

            //        jsonSechma.Title = context.OperationDescription.Operation.OperationId.Replace("/", "_") + "_" + "RequestParamter";

            //        context.Document.Components.Schemas.Add(jsonSechma.Title, jsonSechma);

            //        context.OperationDescription.Operation.RequestBody = new OpenApiRequestBody()
            //        {
            //            Description = "",
            //            IsRequired = true,
            //            Name = "body",
            //            Position = 1,
            //            Content =
            //                {
            //                    ["application/json"]=new OpenApiMediaType()
            //                    {
            //                        Schema = new JsonSchema()
            //                        {
            //                            Reference = jsonSechma,

            //                        }
            //                    }
            //                }
            //        };

            //        var p = new OpenApiParameter();
            //        p.Name = "body";
            //        p.Kind = OpenApiParameterKind.Body;
            //        p.Type = JsonObjectType.Object;
            //        p.Schema = jsonSechma;
            //        p.Reference = jsonSechma;
            //        p.IsRequired = true;
            //        //opt.Operation.Parameters.Add( doc.Document.Operations.Where(x=>x.Path.Contains("Add")).FirstOrDefault().Operation.Parameters[0].);

            //        context.OperationDescription.Operation.Parameters.Add(p);
            //    }
            //    return true;
            //}));

        }

        public static void UseJsonTemplate(this IApplicationBuilder app)
        {
            GlobalJsonTemplateCache.Provider = app.ApplicationServices;
        }



        public static IServiceCollection EnableSyncIO(this IServiceCollection services)
        {
            // If using IIS:
            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            return services;
        }
    }
}
