using System.Diagnostics;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        var task = Task.Run(async () =>
        {
            var sw = Stopwatch.StartNew();
            while (await timer.WaitForNextTickAsync())
            {
                Console.WriteLine($"Wake Up!: {DateTime.Now} {sw.ElapsedMilliseconds}");

                // 1500 ms 소요되는 처리가 발생했다고 가정
                Thread.Sleep(1500);

                sw.Restart();
            }

            Console.WriteLine("타이머가 종료되었습니다.");
        });

        Console.WriteLine("엔터를 누르면 타이머를 종료합니다.");
        Console.ReadLine();

        timer.Dispose();

        Console.WriteLine("엔터를 누르면 종료합니다.");

        Console.ReadLine();
    }
}