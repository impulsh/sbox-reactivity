#if SANDBOX
using Sandbox.Reactivity.Internals;
using static Sandbox.Reactivity.Reactive;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity;

#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public class ReactivePanelComponent : PanelComponent, IReactivePropertyContainer
{
	/// <summary>
	/// The disposable for this component's effect root that exists while it's enabled.
	/// </summary>
	private IDisposable? _effectRoot;

	/// <summary>
	/// A monotonically increasing counter that's incremented when a reactive property on this component updates its
	/// current value. Used to trigger a re-render on this panel via the build hash.
	/// </summary>
	private int _version;

	Dictionary<int, IProducer> IReactivePropertyContainer.Producers { get; } = [];

	// we're starting here instead of OnEnabled since the tree would've already read any properties it's interested in
	// and created the backing producers
	protected sealed override void OnTreeFirstBuilt()
	{
		_effectRoot?.Dispose();

		var initial = true;

		_effectRoot = EffectRoot(() =>
		{
			Effect(() =>
			{
				foreach (var producer in ((IReactivePropertyContainer)this).Producers.Values)
				{
					producer.TrackRead();
				}

				if (initial)
				{
					initial = false;
				}
				else
				{
					_version++;
				}
			});

			OnActivate();
		});
	}

	protected sealed override void OnDisabled()
	{
		_effectRoot?.Dispose();
		_effectRoot = null;
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
