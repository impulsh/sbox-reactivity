#if DEBUG

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Sandbox.Reactivity.Editor.Debugger;

internal partial class DebuggerWidget
{
	/// <summary>
	/// Whether to automatically expand entries in the tree.
	/// </summary>
	[Setting]
	[Title("Auto-expand items")]
	[Icon("expand")]
	public static bool AutoExpand { get; set; } = false;

	/// <summary>
	/// Whether to show where an item in the tree originates from.
	/// </summary>
	[Setting]
	[Title("Show source path")]
	[Icon("code")]
	public static bool ShowSourcePath { get; set; } = true;

	/// <summary>
	/// Whether to highlight newly created effects.
	/// </summary>
	[Setting]
	[Title("Highlight new effects")]
	[Icon("add_circle")]
	[Group("Highlighting")]
	public static bool HighlightNewEffects { get; set; } = true;

	/// <summary>
	/// Whether to highlight effect re-runs.
	/// </summary>
	[Setting]
	[Title("Highlight effect re-runs")]
	[Icon("cached")]
	[Group("Highlighting")]
	public static bool HighlightEffectReruns { get; set; } = true;

	/// <summary>
	/// Whether to show gameplay effects.
	/// </summary>
	[Setting]
	[Title("Show gameplay effects")]
	[Icon("category")]
	[Group("Display")]
	public static bool ShowGameplayEffects { get; set; } = true;

	/// <summary>
	/// Whether to show UI effects.
	/// </summary>
	[Setting]
	[Title("Show UI effects")]
	[Icon("view_quilt")]
	[Group("Display")]
	public static bool ShowUiEffects { get; set; } = false;
}

#endif
