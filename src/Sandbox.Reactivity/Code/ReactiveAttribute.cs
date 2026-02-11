#if SANDBOX
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity;

/// <summary>
/// Enables reactivity for a property. This allows it to cause effects to re-run when the value changes.
/// </summary>
/// <remarks>This only works when used in a component that subclasses <see cref="ReactiveComponent" />.</remarks>
/// <example>
/// <code>
/// public class MyComponent : ReactiveComponent
/// {
/// 	[Reactive]
/// 	public string MyProperty { get; set; } = "";
/// }
/// </code>
/// </example>
[CodeGenerator(CodeGeneratorFlags.WrapPropertyGet | CodeGeneratorFlags.Instance,
	"Sandbox.Reactivity.Internals.ReactivePropertyContainer.GetReactiveValue",
	100)]
[CodeGenerator(CodeGeneratorFlags.WrapPropertySet | CodeGeneratorFlags.Instance,
	"Sandbox.Reactivity.Internals.ReactivePropertyContainer.SetReactiveValue",
	100)]
[AttributeUsage(AttributeTargets.Property)]
#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public sealed class ReactiveAttribute : Attribute
{
}
#endif
