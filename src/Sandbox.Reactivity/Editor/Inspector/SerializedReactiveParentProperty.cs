#if DEBUG

using Sandbox.Reactivity.Internals;

namespace Sandbox.Reactivity.Editor.Inspector;

/// <summary>
/// A property that corresponds to an <see cref="IReactiveObject" />'s parent object.
/// </summary>
internal sealed class SerializedReactiveParentProperty(IReactiveObject reactive) : SerializedProperty
{
	public override Type PropertyType { get; } = reactive.Parent switch
	{
		GameObject => typeof(GameObject),
		Component => typeof(Component),
		IReactiveObject => reactive.GetType(),
		_ => typeof(object),
	};

	public override string Name => "Parent";

	public override string DisplayName => "Parent Object";

	public override string Description =>
		"The object that created and/or manages the lifetime of this reactive object, including any descendant reactive objects.";

	public override bool IsEditable => false;

	public override bool IsValid =>
		reactive is not Effect { IsDisposed: true } && reactive.Parent is { } && base.IsValid;

	public override void SetValue<T>(T value)
	{
	}

	public override T GetValue<T>(T defaultValue = default!)
	{
		return ValueToType(reactive.Parent, defaultValue);
	}
}

#endif
