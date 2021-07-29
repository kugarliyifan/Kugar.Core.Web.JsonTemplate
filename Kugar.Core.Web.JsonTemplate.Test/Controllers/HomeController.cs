using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.Filters;
using Kugar.Core.Web.JsonTemplate.Attributes;
using Kugar.Core.Web.JsonTemplate.Builders;
using Kugar.Core.Web.JsonTemplate.Helpers;
using Kugar.Core.Web.JsonTemplate.Templates;
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
        [HttpPost, FromBodyJson, ProducesResponseType(typeof(TestTemplate2), 200)]
        public async Task<IActionResult> Index([FromQuery]int o0,[Description("ddd")]string str1, [Required]int int2 ,
            
            [ValueTupleDescroption("str2","str2备注"),ValueTupleDescroption("p","p的备注")](string str2, int p) args2)
        {
            return this.Json<TestTemplate2>(new Test<string, string>("22", "33"));

        }

        //[ProducesResponseType(typeof(TestTemplate2),200)]
        //public async Task<IActionResult> Index2()
        //{
        //    return this.Json<TestTemplate2>(new Test<string, string>("22", "33"));

        //}

        //[ProducesResponseType(typeof(TestTemplate3),200)]
        //public async Task<IActionResult> Index3()
        //{
        //    //var p=GlobalJsonTemplateCache.Build<TestTemplate3, IEnumerable<AP>>(typeof(TestTemplate3),
        //    //    typeof(System.Collections.Generic.IEnumerable<AP>));

        //    //var b = new JsonTemplateActionResult<TestTemplate3, IEnumerable<AP>>(typeof(TestTemplate3));

        //    //var t = GlobalJsonTemplateCache.GetActionResultType(typeof(TestTemplate3), Enumerable.Repeat(new AP(){int2 = 2,str2 = "str2",str3 = "33333"},20).GetType());

        //    return this.Json<TestTemplate3>((IEnumerable<AP>)Enumerable.Repeat(new AP(){int2 = 2,str2 = "str2",str3 = "33333",List =Enumerable.Repeat(new APIn(){OO = 3,SSS = "sdfsf"},20)},20).ToArrayEx());

        //}

        /// <summary>
        /// sdfs
        /// </summary>
        /// <param name="p1">222</param>
        /// <param name="o3">333</param>
        /// <returns></returns>
        [HttpGet, ProducesResponseType(typeof(ResultReturn<int>), 200)]
        public async Task<IActionResult> Index4(/*[Required]string p1,[Required]int o3*/)
        {
            var t = new VM_PagedList<(Input input, AP ap)>(
                Enumerable.Repeat(
                    (new Input() {Ints = "222", Strr = 4}, new AP() {int2 = 4, str2 = "str2", str3 = "str3"}), 20),
                pageSize:20,
                totalCount: 200
            );

            return this.Json<Test3Template>(t);
        }

        [HttpPost, FromBodyJson]
        public async Task<IActionResult> Index5((string io, int o3) obj)
        {
            return Json(SuccessResultReturn.Default);
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

    /// <summary>
    /// YYYY
    /// </summary>
    public enum YYY
    {
        /// <summary>
        /// uuuu
        /// </summary>
        UU=0,

        /// <summary>
        /// ooooo
        /// </summary>
        OO=1,

        /// <summary>
        /// pppp
        /// </summary>
        PP=2
    }

    public class Test3Template : WrapResultReturnJsonTemplateBase<IPagedList<(Input input, AP ap)>>
    {
        protected override void BuildReturnDataScheme(IChildObjectBuilder<IPagedList<(Input input, AP ap)>> builder)
        {
            using (var b = builder.FromPagedList(x => x.Model.Cast(y =>
            {
                return (Item: y.input,AP: y.ap);
            })))
            {
                b.AddProperties(x => x.Item.Ints,
                        x => x.Item.Strr,
                        x => x.AP.int2,
                        x => x.AP.str2,
                        x => x.AP.str3);
            }
        }
    }


    public class TestTemplate2 : WrapResultReturnJsonTemplateBase<Test<string, string>>
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

    public class TestTemplate3 : WrapResultReturnArrayJsonTemplateBase<AP>
    {
        //protected override void BuildReturnDataScheme(IArrayBuilder<IEnumerable<AP>> builder)
        //{
        //    builder.AddProperties(x => x.int2, x => x.str2, x => x.str3);
        //}

        protected override void BuildReturnDataScheme(IArrayBuilder<AP> builder)
        {
            builder.AddProperties(x => x.int2, x => x.str2, x => x.str3);

            using (var b=builder.AddArrayObject("List",x=>x.Model.List))
            {
                b.AddProperties(x => x.OO, x => x.SSS);
            }
        }
    }

    public static class Ext
    {

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

        public IEnumerable<APIn> List { set; get; }
    }

    public class APIn
    {
        public string SSS { set; get; }

        public int OO { set; get; }
    }


}
