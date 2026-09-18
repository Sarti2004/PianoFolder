using MacroDeck.Plugin.Hosting;
using MacroDeck.Plugin.Serilog;
using Microsoft.Extensions.DependencyInjection;
using PianoFolder;
using PianoFolder.Notes;

// Identity, description and icon are not set here: they come from manifest.json at the content root.
// Strings is generated from Localization/*.resx, so UseLocalization is what makes every LocalizedString
// below resolve in the user's language rather than falling back to its key.
//
// INoteOutput is the seam a real deployment replaces to actually sound a note (MIDI, OSC, a synth, ...);
// see INoteOutput's remarks. Swap LoggingNoteOutput for a real implementation here.
var plugin = MacroDeckPlugin.CreatePlugin(args)
	.UseMacroDeckLogging()
	.UseLocalization(Strings.LocalizationCatalog)
	.ConfigureServices((_, services) => services
		.AddSingleton<INoteOutput, LoggingNoteOutput>()
		.AddSingleton<INoteDispatcher, NoteDispatcher>())
	.RegisterIntegration<PluginIntegration>()
	.Build();

await plugin.RunAsync();
