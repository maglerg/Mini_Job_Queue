namespace Mini_Job_Queue.API.Queue
{
    public interface IJobQueue
    {
        ValueTask EnqueueAsync(
            Guid jobId,
            CancellationToken cancellationToken = default);

        IAsyncEnumerable<Guid> ReadAllAsync(
            CancellationToken cancellationToken = default);
    }
}
