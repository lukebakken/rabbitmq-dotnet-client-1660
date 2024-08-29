using RabbitMQ.Client.Events;
using System.Threading.Tasks.Dataflow;

namespace Genie.Common.Adapters.RabbitMQ;

// https://stackoverflow.com/questions/34437298/how-can-i-read-messages-from-a-queue-in-parallel

public sealed class RabbitMQPump<T>
{

    /// <summary>
    /// Creates a <see cref="PulsarMessagePump"/> and immediately starts pumping.
    /// </summary>
    public static RabbitMQPump<T> Run(AsyncEventingBasicConsumer messageQueue, Func<BasicDeliverEventArgs, Task> processMessage, int maxDegreeOfParallelism,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(messageQueue, nameof(messageQueue));
        ArgumentNullException.ThrowIfNull(processMessage, nameof(processMessage));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxDegreeOfParallelism, 0, nameof(maxDegreeOfParallelism));

        ct.ThrowIfCancellationRequested();

        return new(messageQueue, processMessage, maxDegreeOfParallelism, ct);
    }

    private readonly TaskCompletionSource<bool> stop = new();
    private readonly AutoResetEvent WaitHandle = new(false);

    /// <summary>
    /// <see cref="Task"/> which completes when this instance
    /// stops due to a <see cref="Stop"/> or cancellation request.
    /// </summary>
    public Task Completion { get; }

    /// <summary>
    /// Maximum number of parallel message processors.
    /// </summary>
    public int MaxDegreeOfParallelism { get; }

    /// <summary>
    /// <see cref="MessageQueue"/> that is pumped by this instance.
    /// </summary>
    public AsyncEventingBasicConsumer MessageQueue { get; }

    public TaskCompletionSource<bool> Stop1 => stop;


    /// <summary>
    /// Creates a new <see cref="KafkaMessagePump"/> instance.
    /// </summary>
    private RabbitMQPump(AsyncEventingBasicConsumer messageQueue, Func<BasicDeliverEventArgs, Task> processMessage, int maxDegreeOfParallelism, CancellationToken ct)
    {
        MessageQueue = messageQueue;
        MaxDegreeOfParallelism = maxDegreeOfParallelism;

        // Kick off the loop.
        Completion = RunAsync(processMessage, ct);
    }

    /// <summary>
    /// Soft-terminates the pump so that no more messages will be pumped.
    /// Any messages already removed from the message queue will be
    /// processed before this instance fully completes.
    /// </summary>
    public void Stop()
    {
        // Multiple calls to Stop are fine.
        Stop1.TrySetResult(true);
    }

    /// <summary>
    /// Pump implementation.
    /// </summary>
    private async Task RunAsync(Func<BasicDeliverEventArgs, Task> processMessage, CancellationToken ct = default)
    {
        using CancellationTokenSource producerCTS = ct.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(ct) : new CancellationTokenSource();
        {
            // This CancellationToken will either be signaled
            // externally, or if our consumer errors.
            ct = producerCTS.Token;

            // Handover between producer and consumer.
            DataflowBlockOptions bufferOptions = new()
            {
                // There is no point in dequeuing more messages than we can process,
                // so we'll throttle the producer by limiting the buffer capacity.
                BoundedCapacity = MaxDegreeOfParallelism,
                CancellationToken = ct
            };

            // NOTE: you MUST copy the body, so a custom
            // class rather than BasicDeliverEventArgs should
            // be used
            BufferBlock<BasicDeliverEventArgs> buffer = new(bufferOptions);

            Task producer = Task.Run(async () =>
            {
                try
                {
                    MessageQueue.Received += async (sender, ea) =>
                    {
                        // NOTE: you MUST copy the body because
                        // it is no longer valid after the event handler
                        // finishes
                        WaitHandle.Set();
                        await buffer.SendAsync(ea, ct).ConfigureAwait(false);
                    };

                    while (Stop1.Task.Status != TaskStatus.RanToCompletion)
                    {
                        WaitHandle.WaitOne(30000);

                        ct.ThrowIfCancellationRequested();
                    }

                }
                finally
                {
                    buffer.Complete();
                    await Task.CompletedTask;
                }
            },
            ct);

            // Wire up the parallel consumers.
            ExecutionDataflowBlockOptions executionOptions = new()
            {
                CancellationToken = ct,
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                SingleProducerConstrained = true, // We don't require thread safety guarantees.
                BoundedCapacity = MaxDegreeOfParallelism,
            };

            ActionBlock<BasicDeliverEventArgs> consumer = new(async message =>
            {
                ct.ThrowIfCancellationRequested();

                await processMessage(message).ConfigureAwait(false);
            },
            executionOptions);

            buffer.LinkTo(consumer, new DataflowLinkOptions { PropagateCompletion = true });

            if (await Task.WhenAny(producer, consumer.Completion).ConfigureAwait(false) == consumer.Completion)
            {
                // If we got here, consumer probably errored. Stop the producer
                // before we throw so we don't go dequeuing more messages.
                producerCTS.Cancel();
            }

            // Task.WhenAll checks faulted tasks before checking any
            // canceled tasks, so if our consumer threw a legitimate
            // execption, that's what will be rethrown, not the OCE.
            await Task.WhenAll(producer, consumer.Completion).ConfigureAwait(false);
        }
    }
}