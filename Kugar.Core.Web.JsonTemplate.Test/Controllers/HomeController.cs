using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using NSwag.Annotations;

namespace Kugar.Core.Web.JsonTemplate.Test.Controllers
{
    [Area("api")]
    [ApiExplorerSettings(GroupName = "api")]
    [Route("api/[controller]/[action]")]
    [Produces("application/json")]
    public class HomeController : Controller
    {
        /// <summary>
        /// api测试
        /// </summary>
        /// <param name="str1">测试参数1</param>
        /// <param name="int2">测试参数2</param>
        /// <returns></returns>
        [HttpPost,ProducesResponseType(typeof(TestTemplate2),200)]
        public async Task<IActionResult> Index([FromQuery]string fromq1, string str1,int int2,(string str2,int p) args2 )
        {
            return this.Json<TestTemplate2>(new Test<string, string>("22", "33"));
            
        }

        [ProducesResponseType(typeof(TestTemplate2),200)]
        public async Task<IActionResult> Index2()
        {
            return this.Json<TestTemplate2>(new Test<string, string>("22", "33"));
            
        }
    }

    public class Input
    {
        /// <summary>
        /// 测试333
        /// </summary>
        public string Ints { set; get; }

        /// <summary>
        /// sdfsdfsdf
        /// </summary>
        public int Strr { set;get; }
    }


    public class TestTemplate2 : WrapResultReturnJsonBuilder<Test<string, string>>
    {
        protected override void BuildReturnDataScheme(IChildObjectBuilder<Test<string, string>> builder)
        {
            builder.AddProperty(x=>x.Prop1)
                .AddProperty("Prop2",x=>x.Model.Prop2,"sdfsfsf");
            builder.AddProperty("Prop4", x => DateTime.Now)
                .AddProperty("prop3", x => x.Model.Prop3.sss2, "测试属性2")
                ;
            using (var b1 = builder.AddObject("sssd", x => x.Model.Prop3))
            {
                b1.AddProperty(x => x.sss2).AddProperty(x => x.ppp);

                using (var b2 = b1.FromObject(x => new { x.Model.ppp, x.Model.sss2 }))
                {
                    b2.AddProperty(x => x.ppp);
                }
            }

            builder.AddArrayValue("arrayTest", x => x.Model.ArrayTest);
        }

        protected override ResultReturnFactory<Test<string, string>> ResultFactory =>
            (c) => new FailResultReturn("sdfs");
    }

    /// <summary>
    /// 在输出的数据外层加多一个ResultReturn的头部
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public abstract class WrapResultReturnJsonBuilder<TModel> : JsonTemplateObjectBase<TModel>
    {
        private static readonly ResultReturnFactory<TModel>
            _defaultResultFactory = (context) => SuccessResultReturn.Default;

        protected WrapResultReturnJsonBuilder()
        {
            this.ResultFactory = _defaultResultFactory;
        }

        public override void BuildScheme(IObjectBuilder<TModel> builder)
        {
            using (var b = BuildWrap(builder))
            {
                BuildReturnDataScheme(b);
            }
        }

        protected abstract void BuildReturnDataScheme(IChildObjectBuilder<TModel> builder);

        protected virtual ResultReturnFactory<TModel> ResultFactory
        {
            get;
        }

        protected virtual IChildObjectBuilder<TModel> BuildWrap(IObjectBuilder<TModel> builder)
        {
            return builder.FromReturnResult(context=>(ResultFactory??_defaultResultFactory).Invoke(context));
        }
    }

    public delegate ResultReturn ResultReturnFactory<in TModel>(IJsonTemplateBuilderContext<TModel> context);

    public static class Ext
    {
        public static IChildObjectBuilder<TModel> FromReturnResult<TModel>(this IObjectBuilder<TModel> source,
            Func<IJsonTemplateBuilderContext<TModel>,bool> resultFactory)
        {
            source.AddProperty("isSuccess",resultFactory)
                .AddProperty("message",x=>string.Empty)
                .AddProperty("returnCode",x=>0,"返回的执行结果代码",example:0)
                ;

            return source.AddObject("returnData", x => x.Model);
        }

        public static IChildObjectBuilder<TModel> FromReturnResult<TModel>(this IObjectBuilder<TModel> source,
            Func<IJsonTemplateBuilderContext<TModel>, (bool isSuccess, string message)> resultFactory)
        {
            using (var f = source.FromObject(resultFactory))
            {
                f.AddProperties(x => x.isSuccess, x => x.message)
                    .AddProperty("returnCode",x=>0,"返回的执行结果代码",example:0)
                    ;
            }

            return source.AddObject("returnData", x => x.Model);
        }

        public static IChildObjectBuilder<TModel> FromReturnResult<TModel>(
            this IObjectBuilder<TModel> source,
            Func<IJsonTemplateBuilderContext<TModel>,(bool isSuccess,int returnCode,string message)> resultFactory)
        {
            using (var f = source.FromObject(resultFactory))
            {
                f.AddProperties(x => x.isSuccess, x => x.message,x=>x.returnCode);
            }

            return source.AddObject("returnData", x => x.Model);
        }

        public static IChildObjectBuilder<TModel> FromReturnResult<TModel>(
            this IObjectBuilder<TModel> source,
            Func<IJsonTemplateBuilderContext<TModel>,ResultReturn> resultFactory)
        {
            using (var f = source.FromObject(resultFactory))
            {
                f.AddProperties(x => x.IsSuccess,x=>x.ReturnCode)
                    .AddProperty("message",x=>x.Model.Message.IfEmptyOrWhileSpace(x.Model.Error?.Message??""),description:"结果文本消息");
            }

            return source.AddObject("returnData", x => x.Model);
        }

        public static IArrayBuilder<TElement> FromPagedList<TModel,TElement>(this IObjectBuilder<TModel> builder,
            [NotNull] Func<IJsonTemplateBuilderContext<TModel>, IPagedList<TElement>> valueFactory
            )
        {
            if (valueFactory==null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            return builder.FromObject(valueFactory)
                .AddProperties(x => x.PageCount, x => x.PageSize, x => x.PageIndex, x => x.TotalCount)
                .AddArrayObject("Data", x => x.Model.GetData(), description: "数据内容");
        }

        public static IArrayBuilder<TElement> FromPagedList<TElement>(
            this IObjectBuilder<IPagedList<TElement>> builder)
        {
            return FromPagedList(builder, x => x.Model);
        }
        
        public static IArrayBuilder<TElement> FromPagedList<TModel,TElement>(this IChildObjectBuilder<TModel> builder,
            [NotNull] Func<IJsonTemplateBuilderContext<TModel>, IPagedList<TElement>> valueFactory
        )
        {
            if (valueFactory==null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            return builder.FromObject(valueFactory)
                .AddProperties(x => x.PageCount, x => x.PageSize, x => x.PageIndex, x => x.TotalCount)
                .AddArrayObject("Data", x => x.Model.GetData(), description: "数据内容");
        }

        public static IArrayBuilder<TElement> FromPagedList<TElement>(
            this IChildObjectBuilder<IPagedList<TElement>> builder)
        {
            return FromPagedList(builder, x => x.Model);
        }
    }
        
    public class Test<T1, T2>
    {
        public Test(T1 p1,T2 p2)
        {
            Prop1 = p1;
            Prop2 = p2;
        }

        /// <summary>
        /// prop2原备注
        /// </summary>
        public T2 Prop2 { set; get; }

        /// <summary>
        /// prop1原备注
        /// </summary>
        public T1 Prop1 { set; get; }

        /// <summary>
        /// prop3备注
        /// </summary>
        public (string sss2, string ppp) Prop3 { set; get; } = ("33333", "4444");

        /// <summary>
        /// 数组测试备注
        /// </summary>
        public T2[] ArrayTest => Enumerable.Repeat(Prop2, 20).ToArrayEx();

        /// <summary>
        /// 数组2测试备注
        /// </summary>
        public AP[] ArrayTest2 { get; }=new AP[]
        {
            new AP(){str2 = "11",str3 = "222",int2 = 10},
            new AP(){str2 = "12",str3 = "223",int2 = 11},
            new AP(){str2 = "13",str3 = "224",int2 = 12},
            new AP(){str2 = "14",str3 = "225",int2 = 13},

        };
    }

    public class AP
    {
        /// <summary>
        /// str2原备注
        /// </summary>
        public string str2 { set; get; }

        /// <summary>
        /// str3原备注
        /// </summary>
        public string str3
        {
            set;
            get;
        }

        /// <summary>
        /// int2原备注
        /// </summary>
        public int int2 { set; get; }
    }
}
