using System.Net.Http;
using System.Threading.Tasks;
using Kugar.Core.Web.JsonTemplate.Test;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Kugar.Core.Web.JsonTemplate.UnitTest
{ 
    public class Tests
    {
        private TestServer _server;
        //private HttpClient _client;

        [SetUp]
        public void Setup()
        {
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());
            //_client = _server.CreateClient();
        }

        [Test]
        public async Task  Test1()
        {

            for (int i = 0; i < 10000; i++)
            {
                var client= _server.CreateClient();

                var response = await client.GetAsync("api/home/index4");
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
            }


            
            //var person = JsonConvert.DeserializeObject<Person>(result);

            //Assert.AreEqual("LN1", person.LastName);

            Assert.Pass();
        }
    }
}