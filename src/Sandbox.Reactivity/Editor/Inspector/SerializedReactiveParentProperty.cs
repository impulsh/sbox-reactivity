#if DEBUG

using Sandbox.Reactivity.Internals;

namespace Sandbox.Reactivity.Editor.Inspector;

/// <summary>
/// A property that corresponds to an <see cref="IReactiveObject" />'s parent object.
/// </summary>
internal sealed class SerializedReactiveParentProperty(IReactiveObject reactive)
	: SerializedReactiveObjectProperty(reactive)
{
	public override Type PropertyType =>
		ReactiveObject.Parent switch
		{
			GameObject => typeof(GameObject),
			Component => typeof(Component),
			IReactiveObject => ReactiveObject.GetType(),
			_ => typeof(object),
		};

	public override string Name => "Parent";

	public override string DisplayName => "Parent Object";

	public override string Description =>
		"The object that created and/or manages the lifetime of this reactive object, including any descendant reactive objects.";

	public override bool IsValid => ReactiveObject.Parent is { } && base.IsValid;

	public override T GetValue<T>(T defaultValue = default!)
	{
		return ValueToType(ReactiveObject.Parent, defaultValue);
	}
}

#endif
