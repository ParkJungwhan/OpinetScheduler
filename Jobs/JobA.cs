using Quartz;

namespace OpinetScheduler.Jobs
{
    public class JobA : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss K}] Job 실행");
            // restAPI로 호출되는 부분

            await Task.CompletedTask;
        }
    }
}