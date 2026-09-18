using PianoFolder.Actions;
using PianoFolder.Piano;
using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Sdk.FolderViews;
using MacroDeck.Sdk.Ui;
using MacroDeck.Ui.Model.Surfaces;
using MacroDeck.Ui.Runtime;
using PianoFolder.Notes;
using Serilog;

namespace PianoFolder;

/// <summary>
/// The plugin's one integration: declares the <c>play-note</c> action, registers the piano keyboard
/// folder view, and serves its UI session. Folder-view registration (<see cref="IFolderViewProvider"/>)
/// is deliberately separate from view composition (<see cref="Piano.PianoKeyboardView"/>) and from note
/// dispatch (<see cref="INoteDispatcher"/>): this class only wires the three together.
/// </summary>
public sealed class PluginIntegration : IPluginIntegration, IFolderViewProvider, IUiProvider
{
    /// <summary>
    /// Local folder-view id, stable across releases: folders in the user's Macro Deck profile reference
    /// it, and renaming it would strand every folder already using this view.
    /// </summary>
    public const string FolderViewId = "piano-keyboard";

    private readonly ILogger _logger;
    private readonly INoteDispatcher _dispatcher;

    public PluginIntegration(ILogger logger, INoteDispatcher dispatcher)
    {
        _logger = logger.ForContext<PluginIntegration>();
        _dispatcher = dispatcher;
        Actions = [new PlayNoteAction(dispatcher)];
    }

    public IReadOnlyList<IActionDefinition> Actions { get; }

    public string ProviderName => "Piano Folder";

    public Task InitializeAsync(IIntegrationContext context)
    {
        _logger.Information("Initialized.");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync() => Task.CompletedTask;

    private static FolderViewDescriptor Descriptor { get; } = new(
        FolderViewId,
        Strings.FolderViews.PianoKeyboard.Name(),
        Strings.FolderViews.PianoKeyboard.Description(),
        HasConfiguration: true);

    /// <summary>
    /// The id Macro Deck actually sends back as <c>UiFolderSurfaceAttributes.ViewId</c> when opening a
    /// session. The SDK docs' own worked example compares against the qualified form
    /// (<c>"your.plugin.id::dashboard"</c>) returned by <see cref="IFolderViewProviderContext.RegisterFolderViewAsync"/>
    /// - captured here rather than reconstructed by hand, since nothing in the public SDK documents the
    /// separator/format as a contract. <see cref="CreateSessionAsync"/> also accepts the bare local id as
    /// a defensive fallback, since that point is not pinned down precisely enough to bet the whole session
    /// negotiation on one form.
    /// </summary>
    private string? _qualifiedFolderViewId;

    /// <summary>Static declaration, so the folder view can be discovered without a running session.</summary>
    public IReadOnlyList<FolderViewDescriptor> GetFolderViews() => [Descriptor];

    /// <summary>Runtime registration, per the SDK's folder-view registration contract.</summary>
    public async Task InitializeAsync(IFolderViewProviderContext context, CancellationToken cancellationToken = default)
    {
        var registration = await context.RegisterFolderViewAsync(Descriptor, cancellationToken);
        _qualifiedFolderViewId = registration.FolderViewId;
        _logger.Information(
            "Registered folder view {LocalId} as {QualifiedId}.",
            FolderViewId,
            _qualifiedFolderViewId);
    }

    public IReadOnlyList<UiSurfaceDeclaration> Surfaces { get; } =
    [
        new UiSurfaceDeclaration { Kind = UiSurfaceKinds.Folder, SessionMode = UiSessionModes.Shared },
    ];

    public Task<IUiSession?> CreateSessionAsync(UiSessionRequest request, CancellationToken cancellationToken)
    {
        if (request.Surface.Kind != UiSurfaceKinds.Folder)
        {
            return Task.FromResult<IUiSession?>(null);
        }

        request.Surface.Attributes.TryGetValue(UiFolderSurfaceAttributes.ViewId, out var viewIdElement);
        var viewId = viewIdElement.ValueKind == System.Text.Json.JsonValueKind.String ? viewIdElement.GetString() : null;
        var isOurs = viewId == FolderViewId || (viewId is not null && viewId == _qualifiedFolderViewId);

        _logger.Information(
            "Folder view session requested: viewId={ViewId}, matched={Matched} (local={LocalId}, qualified={QualifiedId}).",
            viewId,
            isOurs,
            FolderViewId,
            _qualifiedFolderViewId);

        if (!isOurs)
        {
            return Task.FromResult<IUiSession?>(null);
        }

        var model = new PianoKeyboardViewModel(_dispatcher);
        var view = new UiView(request.Surface, PianoKeyboardView.Build(model));
        return Task.FromResult<IUiSession?>(new PianoKeyboardSession(view));
    }
}
