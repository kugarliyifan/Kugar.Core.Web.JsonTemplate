using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using NUnit.Framework;

namespace Kugar.Core.Web.JsonTemplate.UnitTest
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var httpContext = new DefaultHttpContext(new FeatureCollection()
            {

            });
            

            Assert.Pass();
        }
    }
}