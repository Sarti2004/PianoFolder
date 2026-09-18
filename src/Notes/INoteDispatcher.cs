using MacroDeck.Sdk.Actions;

namespace PianoFolder.Notes;

// Fancy way to dispatch events
public interface INoteDispatcher
{
    /// <param name="noteId">A note id as produced by Model, e.g. <c>"cs4"</c>.</param>
    /// <param name="transposeSemitones">Simitone shift on top of the note MIDI number</param>
    Task<ActionResult> DispatchAsync(string noteId, int transposeSemitones, CancellationToken cancellationToken);
}
