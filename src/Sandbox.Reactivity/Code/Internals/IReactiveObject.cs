using System.Diagnostics;

namespace Sandbox.Reactivity.Internals;

internal interface IReactiveObject
{
#if DEBUG && SANDBOX
	/// <summary>
	/// The display name of this reactive object for debug purposes.
	/// </summary>
	string? Name { get; set; }

	/// <summary>
	/// The icon to display for this reactive object in the editor.
	/// </summary>
	string? Icon { get; set; }

	/// <summary>
	/// The file path to where this reactive object was defined. For reactive objects that have an "implementation" like
	/// reactions, this will be the code that actually runs. Otherwise, this will be where the object was instantiated.
	/// </summary>
	string? Location { get; set; }

	/// <summary>
	/// The object that created and/or manages the lifetime of this reactive object.
	/// </summary>
	object? Parent { get; set; }

	/// <summary>
	/// The property on <see cref="Parent" /> that contains this reactive object.
	/// </summary>
	PropertyDescription? Container { get; set; }
#endif
}

internal static class ReactiveObjectExtensions
{
	extension(IReactiveObject reactive)
	{
		[Conditional("DEBUG")]
		public void SetDebugInfo(
			string? name = null,
			string? icon = null,
			string? location = null,
			object? parent = null,
#if SANDBOX
			PropertyDescription? container = null
#else
			object? container = null
#endif
		)
		{
#if DEBUG && SANDBOX
			reactive.Name = name ?? reactive.Name;
			reactive.Icon = icon ?? reactive.Icon;
			reactive.Location = location ?? reactive.Location;
			reactive.Parent = parent ?? reactive.Parent;
			reactive.Container = container ?? reactive.Container;
#endif
		}
	}
}
