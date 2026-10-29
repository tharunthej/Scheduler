
namespace Scheduler
{
    internal readonly record struct Job(Action Work, long EnqueuedAt);
}
