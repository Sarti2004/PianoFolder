using Serilog;

namespace PianoFolder.Notes;

// Only logging for now, replaced with DoriDeck action later
public sealed class LoggingNoteOutput : INoteOutput
{
    public const int DefaultGroupingWindowMilliseconds = 50;

    private readonly ILogger _logger;
    private readonly TimeSpan _groupingWindow;
    private readonly object _sync = new();
    private readonly SortedSet<int> _pendingMidiNumbers = [];

    private TaskCompletionSource? _currentBatch;

    public LoggingNoteOutput(ILogger logger)
        : this(logger, TimeSpan.FromMilliseconds(DefaultGroupingWindowMilliseconds))
    {
    }

    internal LoggingNoteOutput(ILogger logger, TimeSpan groupingWindow)
    {
        if (groupingWindow <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(groupingWindow),
                groupingWindow,
                "The note-grouping window must be greater than zero.");
        }

        _logger = logger.ForContext<LoggingNoteOutput>();
        _groupingWindow = groupingWindow;
    }

    public async Task SendNoteOnAsync(int midiNumber, CancellationToken cancellationToken)
    {
        Task batchTask;

        lock (_sync)
        {
            _pendingMidiNumbers.Add(midiNumber);

            if (_currentBatch is null)
            {
                _currentBatch = new TaskCompletionSource(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                _ = FlushBatchAsync(_currentBatch);
            }

            batchTask = _currentBatch.Task;
        }

        await batchTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task FlushBatchAsync(TaskCompletionSource batch)
    {
        try
        {
            await Task.Delay(_groupingWindow).ConfigureAwait(false);

            int[] midiNumbers;

            lock (_sync)
            {
                midiNumbers = [.. _pendingMidiNumbers];
                _pendingMidiNumbers.Clear();

                if (ReferenceEquals(_currentBatch, batch))
                {
                    _currentBatch = null;
                }
            }

            _logger.Information(
                "Note trigger: MIDI {MidiNumbers}.",
                string.Join(", ", midiNumbers));

            batch.TrySetResult();
        }
        catch (Exception exception)
        {
            lock (_sync)
            {
                if (ReferenceEquals(_currentBatch, batch))
                {
                    _currentBatch = null;
                    _pendingMidiNumbers.Clear();
                }
            }

            batch.TrySetException(exception);
        }
    }
}
