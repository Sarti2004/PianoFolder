using MacroDeck.Ui.Runtime;
using PianoFolder.Notes;

namespace PianoFolder.Piano;


public sealed class PianoKeyboardViewModel
{
    public PianoKeyboardViewModel(INoteDispatcher dispatcher)
    {
        Dispatcher = dispatcher;
        Octaves = new UiState<int>(PianoKeyboardModel.DefaultOctaveCount);
        Transpose = new UiState<int>(0);

        PressedByNoteId = PianoKeyboardModel.AllPlayableKeys().ToDictionary(
            key => key.NoteId,
            _ => new UiState<bool>(false),
            StringComparer.Ordinal);
    }

    public INoteDispatcher Dispatcher { get; }

    public UiState<int> Octaves { get; }

    public UiState<int> Transpose { get; }

    public UiState<int> TransposeOctaveNumber { get; } = new(0);

    public IReadOnlyDictionary<string, UiState<bool>> PressedByNoteId { get; }
}
