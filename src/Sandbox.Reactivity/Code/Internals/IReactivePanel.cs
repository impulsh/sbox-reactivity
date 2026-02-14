#if SANDBOX
namespace Sandbox.Reactivity.Internals;

/// <summary>
/// A panel that creates an effect root every time it builds its render tree.
/// </summary>
internal interface IReactivePanel
{
	/// <summary>
	/// The current effect root created during rendering.
	/// </summary>
	Effect? RenderEffectRoot { get; set; }

	/// <summary>
	/// A monotonically increasing counter that's incremented when a panel's dependencies have changed and needs to
	/// rebuild its render tree.
	/// </summary>
	int Version { get; set; }
}
#endif
