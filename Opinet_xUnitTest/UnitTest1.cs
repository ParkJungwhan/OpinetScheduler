<<<<<<< HEAD
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
=======
>>>>>>> origin/develop
using OpinetScheduler.Services;

namespace Opinet_xUnitTest
{
    public class UnitTest1
    {
        private OpinetClient opinet;

        public UnitTest1()
        {
            var http = new HttpClient();
            http.BaseAddress = new Uri("https://www.opinet.co.kr");
            http.DefaultRequestHeaders.Add("Accept", "application/json");
            http.DefaultRequestHeaders.Add("User-Agent", "OpinetSchedulerTestClient/1.0");

<<<<<<< HEAD
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            opinet = new OpinetClient(http, config);
=======
            opinet = new OpinetClient(http);
>>>>>>> origin/develop
        }

        [Fact]
        public void Test1()
        {
            var result = opinet.GetAvgAllPriceAsync();
<<<<<<< HEAD
            result.Wait();

            var sss = result.Result.RESULT.OIL;
        }

        [Fact]
        public void Test2()
        {
            var result = opinet.GetAvgSidoPriceAsync("01", "0106");
            result.Wait();

            //var sss = result.;
        }

        [Fact]
        public void Test3()
        {
            var result = opinet.GetAreaCodeAsync("01");
            result.Wait();

            var sss = result.Result.RESULT.OIL;
=======
>>>>>>> origin/develop
        }
    }
}