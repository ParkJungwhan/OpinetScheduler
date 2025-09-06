using OpinetScheduler.Services;
using Quartz;

namespace OpinetScheduler.Jobs
{
    public class JobB : IJob
    {
        private readonly OpinetClient _client;

        public JobB(OpinetClient client)
        {
            _client = client;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss K}] JobB 실행");
            // 필요 시 다른 엔드포인트 호출로 확장 (예: 시도별 평균가)
            try
            {
                var res = await _client.GetAvgSidoPriceAsync(sido: "01", prodcd: "B027", context.CancellationToken);
                var first = res.RESULT?.OIL?.FirstOrDefault();
                if (first != null)
                {
                    Console.WriteLine($"[Opinet] 시도:{first.SIDONM} {first.PRODCD} {first.PRICE}원 (Δ {first.DIFF})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Opinet] 호출 실패: {ex.Message}");
            }
        }
    }
}
