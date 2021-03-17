using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Fasterflect;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation.TypeMappers;
using NSwag;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Kugar.Core.Web.JsonTemplate
{
    public static class ApplicationBuilder
    {
        public static void AddJsonTemplateV2(this AspNetCoreOpenApiDocumentGeneratorSettings opt,
            params Assembly[] typeAssemblies)
        {
            var types = typeAssemblies.SelectMany(x => x.GetTypes())
                .Where(x => x.IsImplementlInterface(typeof(IJsonTemplateObject)) && !x.IsAbstract &&
                            x.IsPublic)
                .ToArrayEx();


            foreach (var t in types)
            {
                opt.TypeMappers.Add(new ObjectTypeMapper(t, (gen, resolver) =>
                {
                    var builder = GlobalJsonTemplateCache.GetTemplateInfo(t);

                    return builder.SchemaBuilder.Schema;

                }));
            }
            
            opt.OperationProcessors.Add(new ValueTupleOperationProcessor());
            
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

    public class ValueTupleOperationProcessor : IOperationProcessor
    {
        public bool Process(OperationProcessorContext context)
        {
            //if (!context.MethodInfo.GetCustomAttributes().Any(x => x.GetType().FullName.Contains("FromBodyJson")))
            //{
            //    return true;
            //}

            var sourceParamters = context.OperationDescription.Operation.Parameters.ToArrayEx();
            
            context.OperationDescription.Operation.Parameters.Clear();

            var jsonScheme = new JsonSchema();
            jsonScheme.Type = JsonObjectType.Object;

            var methodParamters = context.MethodInfo.GetParameters();

            for (int i = 0; i < methodParamters.Length; i++)
            {
                if (!isFromBody(methodParamters[i]))
                {
                    context.OperationDescription.Operation.Parameters.Add(sourceParamters[i]);
                }
            }


            for (int i = 0; i < sourceParamters.Length; i++)
            {
                var p = sourceParamters[i];

                if (!isFromBody(methodParamters[i]))
                {
                    continue;
                }

                if (p.Type == JsonObjectType.Object)
                {
                    var methodParamter = methodParamters[i];

                    var newObject = new JsonSchemaProperty()
                    {
                        Type = JsonObjectType.Object,
                        Description = p.Description
                    };

                    jsonScheme.Properties.Add(p.Name, newObject);

                    if (methodParamter.ParameterType.Name.StartsWith("ValueTuple`"))
                    {
                        var tupleElement = methodParamter.GetCustomAttribute<TupleElementNamesAttribute>();

                        var valueTupleArgsTypes = methodParamter.ParameterType.GetGenericArguments();

                        var p1 = valueTupleToJsonProperties(tupleElement, valueTupleArgsTypes).Properties;

                        foreach (var property in p1)
                        {
                            newObject.Properties.Add(property);
                        }

                        //newObject.Properties.Add(p.Name, );
                    }
                    else
                    {
                        var p1 = JsonSchema.FromType(methodParamter.ParameterType);

                        var jp = new JsonSchemaProperty();

                        foreach (var property in p1.Properties)
                        {
                            jp.Properties.Add(property);
                        }

                        jp.Description = p1.Description;
                        jp.Type = JsonObjectType.Object;
                        

                        newObject.Properties.Add(p.Name, jp);


                    }
                }
                else
                {
                    jsonScheme.Properties.Add(p.Name, new JsonSchemaProperty()
                    {
                        Type = p.Type,
                        Description = p.Description,
                        IsNullableRaw = p.IsNullableRaw,
                        Example = p.Examples,

                    });
                }

            }


            context.OperationDescription.Operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "body",
                Type = JsonObjectType.Object,
                Schema = jsonScheme,
                Kind = OpenApiParameterKind.Body
            });

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
