#if SANDBOX
using Sandbox.Reactivity.Internals;
using static Sandbox.Reactivity.Reactive;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity;

/// <summary>
/// The base component used to opt into the reactivity system. This allows usage of the reactive property attributes,
/// and automatically creates an effect root in <see cref="OnActivate" />.
/// </summary>
/// <seealso cref="ReactiveAttribute" />
/// <seealso cref="DerivedAttribute" />
#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public abstract class ReactiveComponent : Component, IReactivePropertyContainer
{
	/// <summary>
	/// The disposable for this component's effect root that exists while it's enabled.
	/// </summary>
	private IDisposable? _effectRoot;

	Dictionary<int, IProducer> IReactivePropertyContainer.Producers { get; } = [];

	protected sealed override void OnEnabled()
	{
		_effectRoot?.Dispose();
		_effectRoot = EffectRoot(OnActivate);
	}

	protected sealed override void OnDisabled()
	{
		_effectRoot?.Dispose();
		_effectRoot = null;
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
