namespace PianoFolder.Piano;

// need some comments
public sealed record PianoKey(string NoteId, string ElementKey, int MidiNumber, bool IsAccidental);

/// <summary>
/// Workaround for black keys;
/// </summary>
/// <param name="Natural"></param>
/// <param name="Accidental"></param>
public sealed record BlackBandSlot(PianoKey Natural, PianoKey? Accidental);
