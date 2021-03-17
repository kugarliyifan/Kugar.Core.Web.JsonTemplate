using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Fasterflect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation;
using NSwag;
using NSwag.Generation.AspNetCore;

namespace Kugar.Core.Web.JsonTemplate
{
    internal static class GlobalJsonTemplateCache
    {
        private static ConcurrentDictionary<Type, object> _cache = new ConcurrentDictionary<Type, object>();

        private static ConcurrentDictionary<string, ConstructorInvoker>
            _cacheActionResultTypes = new ConcurrentDictionary<string, ConstructorInvoker>();

        public static IServiceProvider Provider { set; get; }
        
        public static IObjectBuilderPipe<TModel> GetTemplate<TModel>(Type builderType)
            //where TBuilder : JsonTemplateObjectBase<TModel>, new()
        {
            //var m= typeof(GlobalJsonTemplateCache)
            //    .GetMethod("Build")
            //    .MakeGenericMethod(builderType, typeof(TModel));


            //return (IObjectBuilderPipe<TModel>)m.Invoke(null,new []{builderType,typeof(TModel)});

            var objectBuilder=(IObjectBuilderPipe<TModel>)_cache.GetOrAdd(builderType, (type) =>
            {
                var m= typeof(GlobalJsonTemplateCache)
                    .GetMethod("Build")
                    .MakeGenericMethod(builderType, typeof(TModel));


                return m.Invoke(null,new []{builderType,typeof(TModel)});
                // return Build<TBuilder, TModel>();
            });

            return objectBuilder;
        }

        public static IObjectBuilderInfo GetTemplateInfo(Type builderType)
        {
            var modelType = builderType.BaseType.GetGenericArguments().FirstOrDefault();

            var objectBuilder=_cache.GetOrAdd(builderType, (type) =>
            {
                var m= typeof(GlobalJsonTemplateCache)
                    .GetMethod("Build")
                    .MakeGenericMethod(builderType, modelType);


                return m.Invoke(null,new []{builderType,modelType});
            });

            return (IObjectBuilderInfo) objectBuilder;
        }

        public static ConstructorInvoker GetActionResultType(Type builderType,Type modelType)
        {
            return _cacheActionResultTypes.GetOrAdd($"{builderType.FullName}-{modelType.FullName}", (b) =>
            {
                var actionResultType = typeof(JsonTemplateActionResult<>).MakeGenericType(modelType);

                return actionResultType.DelegateForCreateInstance(Flags.InstancePublic,
                    typeof(Type));
            });
        }

        public static IObjectBuilderPipe<TModel> Build<TBuilder,TModel>(Type builderType,Type modelType)  where TBuilder : JsonTemplateObjectBase<TModel>, new()
        {
            var opt =
                (IOptions<AspNetCoreOpenApiDocumentGeneratorSettings>)Provider.GetService(
                    typeof(IOptions<AspNetCoreOpenApiDocumentGeneratorSettings>));

            var g = (JsonSchemaGenerator)Provider.GetService(typeof(JsonSchemaGenerator));

            //var register = (OpenApiDocumentRegistration)HttpContext.Current.RequestServices.GetService(typeof(OpenApiDocumentRegistration));

            //var opt1 = HttpContext.Current.Features.Get<IOptions<AspNetCoreOpenApiDocumentGeneratorSettings>>();

            var document = new OpenApiDocument();
            //var settings = new AspNetCoreOpenApiDocumentGeneratorSettings();
            var schemaResolver = new OpenApiSchemaResolver(document, opt.Value);
            var generator = g ?? new JsonSchemaGenerator(opt.Value);
            
            DefaultContractResolver jsonResolver = null;

            var scheme = new JsonSchema();

#if NETCOREAPP3_0 || NETCOREAPP3_1
            var jsonOpt = (IOptionsSnapshot<MvcNewtonsoftJsonOptions>)Provider.GetService(typeof(IOptions<MvcNewtonsoftJsonOptions>));

            if (jsonOpt!=null &&jsonOpt.Value!=null)
            {
                jsonResolver = jsonOpt.Value.SerializerSettings.ContractResolver as DefaultContractResolver;
            }
            else
            {
                jsonResolver = JsonConvert.DefaultSettings?.Invoke().ContractResolver as DefaultContractResolver;
            }
#endif
#if NETCOREAPP2_1
            jsonResolver = JsonConvert.DefaultSettings?.Invoke().ContractResolver as DefaultContractResolver;
            //var _defaultSettings = JsonConvert.DefaultSettings?.Invoke();
#endif

            using var builder = new JsonTemplateObjectBuilder<TModel>(
                new NSwagSchemeBuilder(scheme, s =>jsonResolver?.NamingStrategy?.GetPropertyName(s, false)??s),
                generator,
                schemaResolver);

            var b = new TBuilder();

            builder.Start();
            b.BuildScheme(builder);
            builder.End();
            
            return builder;
        }
    }
}