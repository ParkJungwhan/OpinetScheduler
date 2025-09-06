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

            opinet = new OpinetClient(http);
        }

        [Fact]
        public void Test1()
        {
            var result = opinet.GetAvgAllPriceAsync();
        }
    }
}