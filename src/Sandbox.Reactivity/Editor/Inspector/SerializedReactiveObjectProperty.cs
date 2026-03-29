#if DEBUG

using Sandbox.Reactivity.Internals;

namespace Sandbox.Reactivity.Editor.Inspector;

internal class SerializedReactiveObjectProperty(IReactiveObject reactive) : SerializedProperty
{
	protected readonly IReactiveObject ReactiveObject = reactive;

	public override string Name { get; } = reactive.Name ?? "Reactive Object";

	public override string DisplayName => Name;

	public override Type PropertyType => ReactiveObject.GetType();

	public override bool IsEditable => false;

	public override bool IsValid => ReactiveObject is not Effect { IsDisposed: true } && base.IsValid;

	public override void SetValue<T>(T value)
	{
	}

	public override T GetValue<T>(T defaultValue = default!)
	{
		return ValueToType(ReactiveObject, defaultValue);
	}
}

#endif
