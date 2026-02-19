#if SANDBOX
using Sandbox.Reactivity.Internals;
using static Sandbox.Reactivity.Reactive;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity;

/// <summary>
/// The reactive counterpart to <see cref="PanelComponent" /> that allows usage of reactive properties.
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
public class ReactivePanelComponent : PanelComponent, IReactivePropertyContainer, IReactivePanel
{
	private IDisposable? _effectRoot;

	private Effect? _renderEffectRoot;

	private int _version;

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
		_renderEffectRoot?.Dispose();
		_renderEffectRoot = null;

		return new ReactivePanelScope(this);
	}

	protected sealed override void OnEnabled()
	{
		_effectRoot = EffectRoot(OnActivate);
	}

	protected sealed override void OnDisabled()
	{
		_renderEffectRoot?.Dispose();
		_renderEffectRoot = null;

		_effectRoot?.Dispose();
		_effectRoot = null;

		base.OnDisabled();
	}

	protected sealed override int BuildHash()
	{
		return _version;
	}

	/// <summary>
	/// Called inside an effect root when this component is enabled, allowing for effects to be created. When this
	/// component is disabled, the effect root (and all of its descendants) are disposed.
	/// </summary>
	protected virtual void OnActivate()
	{
	}
}
#endif
