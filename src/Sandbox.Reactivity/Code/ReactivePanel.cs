#if SANDBOX
using Sandbox.Reactivity.Internals;
using Sandbox.UI;
using static Sandbox.Reactivity.Reactive;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity;

#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public class ReactivePanel : Panel, IReactivePropertyContainer
{
	/// <summary>
	/// The disposable for this component's effect root that exists while it's enabled.
	/// </summary>
	private IDisposable? _effectRoot;

	/// <summary>
	/// A monotonically increasing counter that's incremented when a reactive property on this panel updates its
	/// current value. Used to trigger a re-render on this panel via the build hash.
	/// </summary>
	private int _version;

	Dictionary<int, IProducer> IReactivePropertyContainer.Producers { get; } = [];

	protected override void OnAfterTreeRender(bool firstTime)
	{
		if (!firstTime)
		{
			return;
		}

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

	public sealed override void Delete(bool immediate = false)
	{
		_effectRoot?.Dispose();
		_effectRoot = null;

		base.Delete(immediate);
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
