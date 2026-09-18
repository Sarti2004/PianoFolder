using MacroDeck.Localization;
using MacroDeck.Sdk.Actions;
using PianoFolder.Piano;
using Serilog;

namespace PianoFolder.Notes;

public sealed class NoteDispatcher(INoteOutput output, ILogger logger) : INoteDispatcher
{
    private readonly ILogger _logger = logger.ForContext<NoteDispatcher>();

    public async Task<ActionResult> DispatchAsync(string noteId, int transposeSemitones, CancellationToken cancellationToken)
    {
        if (!PianoKeyboardModel.TryGetKey(noteId, out var key))
        {
            _logger.Warning("unknown note id {NoteId}.", noteId);
            return ActionResult.Failed(
                ActionErrorCodes.InvalidParameter,
                LocalizedText.FromLiteral($"'{noteId}' is not a recognized note id."));
        }

        var midiNumber = key.MidiNumber + PianoKeyboardModel.ClampTranspose(transposeSemitones);

        try
        {
            await output.SendNoteOnAsync(midiNumber, cancellationToken).ConfigureAwait(false);
            return ActionResult.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.Warning(exception, "Note output failed for {NoteId} ({MidiNumber}).", noteId, midiNumber);
            return ActionResult.Failed(ActionErrorCodes.ProviderError, LocalizedText.FromLiteral(exception.Message));
        }
    }
}
