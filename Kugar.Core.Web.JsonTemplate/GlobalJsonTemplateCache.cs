using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Fasterflect;
using Kugar.Core.Web.JsonTemplate.Builders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation;
using NSwag;
using NSwag.Generation.AspNetCore;

namespace Kugar.Core.Web.JsonTemplate
{
    public static class GlobalJsonTemplateCache
    {
        private static ConcurrentDictionary<Type, object> _cache = new ConcurrentDictionary<Type, object>();

        private static ConcurrentDictionary<Type, ConcurrentDictionary<Type, ConstructorInvoker>>
            _cacheActionResultTypes = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, ConstructorInvoker>>();

        public static IServiceProvider Provider { set; get; }

        public static IObjectBuilderPipe<TModel> GetTemplate<TBuilder, TModel>()
            where TBuilder : JsonTemplateBase<TModel>, new()
        {
            var builderType = typeof(TBuilder);

            var objectBuilder = (IObjectBuilderPipe<TModel>)_cache.GetOrAdd(builderType, (type) =>
              {
                  var m = typeof(GlobalJsonTemplateCache)
                      .GetMethod("Build")
                      .MakeGenericMethod(builderType, typeof(TModel));


                  return m.Invoke(null, new[] { builderType, typeof(TModel) });
                // return Build<TBuilder, TModel>();
            });

            return objectBuilder;
        }

        public static IObjectBuilderInfo GetTemplateInfo(Type builderType)
        {
            //此处代码用于防止传入的modelType无法构建Build的TModel泛型,比如定义的是 IEnumerable,但传入的modelType是Array的情况
            var t = enumAllParentType(builderType).FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(JsonTemplateBase<>));

            var mtype = t.GetGenericArguments()[0];

            var objectBuilder = _cache.GetOrAdd(builderType, (type) =>
              {
                  var m = typeof(GlobalJsonTemplateCache)
                      .GetMethod("Build")
                      .MakeGenericMethod(builderType, mtype);


                  return m.Invoke(null, new[] { builderType, mtype });
              });

            return (IObjectBuilderInfo)objectBuilder;
        }

        public static ConstructorInvoker GetActionResultType(Type builderType, Type modelType)
        {
            return _cacheActionResultTypes
                .GetOrAdd(builderType, t => new ConcurrentDictionary<Type, ConstructorInvoker>())
                .GetOrAdd(modelType, b =>
                {
                    //此处代码用于防止传入的modelType无法构建JsonTemplateActionResult的TModel泛型,比如定义的是 IEnumerable,但传入的modelType是Array的情况
                    var t = enumAllParentType(builderType).FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(JsonTemplateBase<>));
                    var mtype = t.GetGenericArguments()[0];

                    Type actionResultType = null;

                    if (mtype==modelType)
                    {
                        actionResultType = typeof(JsonTemplateActionResult<,>).MakeGenericType(builderType, modelType);    
                    }
                    else
                    {
                        actionResultType=typeof(JsonTemplateActionResult<,>).MakeGenericType(builderType, mtype);    
                    }
                    return actionResultType.DelegateForCreateInstance(Flags.InstancePublic,
                        typeof(Type));
                });

            //return _cacheActionResultTypes.GetOrAdd($"{builderType.FullName}-{modelType.FullName}", (b) =>
            //{
            //    var actionResultType = typeof(JsonTemplateActionResult<>).MakeGenericType(modelType);

            //    return actionResultType.DelegateForCreateInstance(Flags.InstancePublic,
            //        typeof(Type));
            //});
        }

        private static IEnumerable<Type> enumAllParentType(Type type)
        {
            var currentType = type.BaseType;

            while (currentType!=null && currentType != typeof(object))
            {
                yield return currentType;
                currentType = currentType.BaseType;
            }
        }

        private static IEnumerable<Type> enumAllInterface(Type type)
        {
            var currentType = type.BaseType;

            while (currentType!=null && currentType != typeof(object))
            {
                yield return currentType;
                currentType = currentType.BaseType;
            }
        }

        public static IObjectBuilderPipe<TModel> Build<TBuilder, TModel>(Type builderType, Type modelType) where TBuilder : JsonTemplateBase<TModel>, new()
        {
            if (Provider==null)
            {
                throw new Exception("Provider为null,请检查是否已调用 app.UseJsonTemplate();");
            }

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

            if (jsonOpt?.Value != null)
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

            var builder = new JsonTemplateObjectBuilder<TModel>(
                new NSwagSchemeBuilder(scheme, s => jsonResolver?.NamingStrategy?.GetPropertyName(s, false) ?? s),
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