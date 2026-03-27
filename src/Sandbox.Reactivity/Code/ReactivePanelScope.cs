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
	private readonly Effect.ExecutionScope _executionScope;

	internal ReactivePanelScope(IReactivePanel panel)
	{
		if (panel.RenderEffectRoot is { } previousRoot)
		{
			// don't teardown previous root since we're already building the render tree by this point
			previousRoot.Dispose(false);
		}

		// nested panels don't render immediately when a containing panel's tree is rendering, so the parent is
		// always null anyway
		var effectRoot = new Effect(null, null, true, () => panel.Version++);
		effectRoot.SetDebugInfo(panel.GetType().ToSimpleString(false) + " (Render)",
			panel is ReactivePanel ? "view_quilt" : "monitor",
			new CallLocation(2),
			panel is ReactivePanel reactive ? reactive.GameObject?.GetComponent<IReactivePanel>() : panel);

		panel.RenderEffectRoot = effectRoot;
		_executionScope = new Effect.ExecutionScope(effectRoot);
	}

	public void Dispose()
	{
		_executionScope.Dispose();
	}
}
#endif
