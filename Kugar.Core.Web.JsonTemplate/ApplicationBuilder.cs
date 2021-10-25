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

            opt.SchemaProcessors.Add(new EnumProcessor());

            opt.OperationProcessors.Add(new ValueTupleOperationProcessor());
            opt.OperationProcessors.Add(new ActionParamtersToJsonBodyProcessor());
        }

        public static void UseJsonTemplate(this IApplicationBuilder app)
        {
            GlobalJsonTemplateCache.Provider = app.ApplicationServices;
        }

        public static IServiceCollection AddJsonTemplateOptions(this IServiceCollection services,JsonTemplateOption options)
        {
            services.AddOptions<JsonTemplateOption>().Configure((s) =>
            {
                s.NullArrayFormatting = options.NullArrayFormatting;
                s.NullObjectFormatting = options.NullObjectFormatting;
            });

            return services;
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
