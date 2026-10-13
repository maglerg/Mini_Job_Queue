using System.Threading.Channels;

namespace Mini_Job_Queue.API.Queue
{
    public class JobQueue : IJobQueue
    {
        private readonly Channel<Guid> _channel;

        public JobQueue()
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };

            _channel = Channel.CreateBounded<Guid>(options);
        }

        public ValueTask EnqueueAsync(
            Guid jobId,
            CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(
                jobId,
                cancellationToken);
        }

        public IAsyncEnumerable<Guid> ReadAllAsync(
            CancellationToken cancellationToken = default)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
