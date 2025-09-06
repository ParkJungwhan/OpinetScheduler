using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpinetScheduler;
using OpinetScheduler.Jobs;
using OpinetScheduler.Services;
using Quartz;
using Quartz.Impl.Matchers;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var builder = Host.CreateApplicationBuilder(args);

        // 타임존: 서울(Asia/Seoul)
        var krTz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");

        // Quartz 등록
        builder.Services.AddQuartz(q =>
        {
            // 스케줄러 기본 설정 (옵션)
            q.SchedulerId = "MainScheduler";
            q.UseMicrosoftDependencyInjectionJobFactory();

            // --- A 작업: 하루 4회 (정시 00:00, 06:00, 12:00, 18:00) ---
            var aJobKey = new JobKey("JobA");
            q.AddJob<JobA>(opts => opts.WithIdentity(aJobKey).StoreDurably());

            q.AddTrigger(opts => opts
                .ForJob(aJobKey)
                .WithIdentity("JobA-Trigger-Cron")
                .WithCronSchedule(
                    "0 0 0,6,12,18 * * ?",      //cron 형태
                    x => x.InTimeZone(krTz)
                          .WithMisfireHandlingInstructionDoNothing()) // 미스파이어 시: 다음 스케줄까진 건너뛰고 정상 진행
                .WithDescription("Run 4 times a day at 00, 06, 12, 18 (KST)")
            );

            // --- B 작업: 하루 10회 (144분 간격) ---
            // 24시간 / 10 = 2.4시간 = 144분
            var bJobKey = new JobKey("JobB");
            q.AddJob<JobB>(opts => opts.WithIdentity(bJobKey).StoreDurably());

            q.AddTrigger(opts => opts
                .ForJob(bJobKey)
                .WithIdentity("JobB-Trigger-144min")
                // 자정 시작을 보장하려면 StartAt를 오늘 자정(KST)로, 아니면 StartNow()
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithInterval(TimeSpan.FromMinutes(144))        // timespan 형태
                    .RepeatForever()
                    .WithMisfireHandlingInstructionNowWithExistingCount())      // 미스파이어 시: 한 번 즉시 실행하고 주기 유지
                .WithDescription("Run every 144 minutes (10 times per day) KST")
            );

            // 필요하다면 전역 타임존 기본값 지정도 가능
            // q.SetProperty("quartz.scheduler.timeZoneId", "Asia/Seoul");
        });

        // 호스티드 서비스: 종료 시 잡 완료 대기
        builder.Services.AddQuartzHostedService(options =>
        {
            options.AwaitApplicationStarted = true;
            options.WaitForJobsToComplete = true;
        });

        // OpinetClient 등록 (HttpClient/DI)
        builder.Services.AddHttpClient<OpinetClient>(client =>
        {
            client.BaseAddress = new Uri("https://www.opinet.co.kr");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        var app = builder.Build();

        // (선택) 스케줄러 상태 로그 예시
        var sched = app.Services.GetRequiredService<ISchedulerFactory>();
        var scheduler = await sched.GetScheduler();
        scheduler.ListenerManager.AddSchedulerListener(new ConsoleSchedulerListener());

        await app.RunAsync();
    }
}

//var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
//var task = Task.Run(async () =>
//{
//    var sw = Stopwatch.StartNew();
//    while (await timer.WaitForNextTickAsync())
//    {
//        Console.WriteLine($"Wake Up!: {DateTime.Now} {sw.ElapsedMilliseconds}");
//        // 1500 ms 소요되는 처리가 발생했다고 가정
//        Thread.Sleep(1500);
//        sw.Restart();
//    }
//    Console.WriteLine("타이머가 종료되었습니다.");
//});
//Console.WriteLine("엔터를 누르면 타이머를 종료합니다.");
//Console.ReadLine();
//timer.Dispose();
//Console.WriteLine("엔터를 누르면 종료합니다.");
//Console.ReadLine();
