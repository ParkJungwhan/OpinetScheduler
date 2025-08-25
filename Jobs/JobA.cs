using OpinetScheduler.Services;
using Quartz;

namespace OpinetScheduler.Jobs
{
    public class JobA : IJob
    {
        private readonly OpinetClient _client;

        public JobA(OpinetClient client)
        {
            _client = client;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss K}] JobA 실행");
            // 샘플: 전국 평균가 조회
            try
            {
                var res = await _client.GetAvgAllPriceAsync(context.CancellationToken);
                var first = res.RESULT?.OIL?.FirstOrDefault();
                if (first != null)
                {
                    Console.WriteLine($"[Opinet] {first.TRADE_DT} {first.PRODNM} {first.PRICE}원 (Δ {first.DIFF})");
                }
                else
                {
                    Console.WriteLine("[Opinet] 응답은 성공했지만 데이터가 비어있습니다.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Opinet] 호출 실패: {ex.Message}");
            }
        }
    }
}
