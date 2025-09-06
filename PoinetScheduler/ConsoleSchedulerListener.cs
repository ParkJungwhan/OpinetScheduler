using Quartz;

namespace OpinetScheduler
{
    public class ConsoleSchedulerListener : Quartz.Listener.SchedulerListenerSupport
    {
        public override Task JobScheduled(ITrigger trigger, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[Quartz] Scheduled: {trigger.Key} -> {trigger.Description}");
            return Task.CompletedTask;
        }

        public override Task JobUnscheduled(TriggerKey triggerKey, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[Quartz] Unscheduled: {triggerKey}");
            return Task.CompletedTask;
        }

        public override Task SchedulerStarted(CancellationToken cancellationToken = default)
        {
            Console.WriteLine("[Quartz] Scheduler started");
            return Task.CompletedTask;
        }

        public override Task SchedulerShutdown(CancellationToken cancellationToken = default)
        {
            Console.WriteLine("[Quartz] Scheduler shutdown");
            return Task.CompletedTask;
        }
    }
}