#if SANDBOX
using Sandbox.Reactivity.Internals;
using Sandbox.UI;
using static Sandbox.Reactivity.Reactive;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity;

/// <summary>
/// The reactive counterpart to <see cref="Panel" /> that allows usage of reactive properties.
/// </summary>
/// <remarks>
/// Make sure you set up an effect root using <see cref="PanelRoot" /> at the top of your razor markup:
/// <code>
/// @{ using var _ = PanelRoot(); }
/// </code>
/// Engine limitations prevent this from being done automatically.
/// </remarks>
#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public class ReactivePanel : Panel, IReactivePropertyContainer, IReactivePanel
{
	private IDisposable? _effectRoot;

	private Effect? _renderEffectRoot;

	private int _version;

	public ReactivePanel()
	{
		_effectRoot = EffectRoot(OnActivate);
	}

	Effect? IReactivePanel.RenderEffectRoot
	{
		get => _renderEffectRoot;
		set => _renderEffectRoot = value;
	}

	int IReactivePanel.Version
	{
		get => _version;
		set => _version = value;
	}

	Dictionary<int, IProducer> IReactivePropertyContainer.Producers { get; } = [];

	protected ReactivePanelScope PanelRoot()
	{
		return new ReactivePanelScope(this);
	}

	public sealed override void Delete(bool immediate = false)
	{
		_renderEffectRoot?.Dispose();
		_renderEffectRoot = null;

		_effectRoot?.Dispose();
		_effectRoot = null;

		base.Delete(immediate);
	}

	protected sealed override int BuildHash()
	{
		return _version;
	}

	/// <summary>
	/// Called inside an effect root when this panel is instantiated, allowing for effects to be created. When this
	/// panel is deleted, the effect root (and all of its descendants) are disposed.
	/// </summary>
	protected virtual void OnActivate()
	{
	}
}
#endif
