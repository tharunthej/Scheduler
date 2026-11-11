
namespace Scheduler
{
    internal readonly record struct Job(Func<Task> Work, long EnqueuedAt);
}
