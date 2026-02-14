#if SANDBOX
using Sandbox.Reactivity.Internals;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

// we can't wrap the BuildRenderTree method for razor components, so we need something that can set up the proper
// scope inside the markup itself

namespace Sandbox.Reactivity;

/// <summary>
/// A disposable that's used to enable reactivity for a <see cref="ReactivePanelComponent" /> or
/// <see cref="ReactivePanel" /> during rendering.
/// </summary>
#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public readonly ref struct ReactivePanelScope : IDisposable
{
	private readonly Effect? _previousEffect = Reactive.Runtime.CurrentEffect;

	private readonly IReaction? _previousReaction = Reactive.Runtime.CurrentReaction;

	private readonly bool _previousIsUntracking = Reactive.Runtime.IsUntracking;

	internal ReactivePanelScope(IReactivePanel panel)
	{
		if (panel.RenderEffectRoot is { } previousRoot)
		{
			// don't teardown previous root since we're already building the render tree by this point
			previousRoot.Dispose(false);
		}

		var effectRoot = new Effect(null, _previousEffect, true, () => panel.Version++);
		panel.RenderEffectRoot = effectRoot;

		Reactive.Runtime.CurrentEffect = effectRoot;
		Reactive.Runtime.CurrentReaction = effectRoot;
		Reactive.Runtime.IsUntracking = false;
	}

	public void Dispose()
	{
		Reactive.Runtime.CurrentEffect = _previousEffect;
		Reactive.Runtime.CurrentReaction = _previousReaction;
		Reactive.Runtime.IsUntracking = _previousIsUntracking;
	}
}
#endif
