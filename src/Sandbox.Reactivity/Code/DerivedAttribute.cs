#if SANDBOX
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity;

/// <summary>
/// Makes a reactive property with a <see cref="ReactiveAttribute" /> derive its value from the result of a method call.
/// The method will run only when any of the reactive values read during its execution are changed. The result is
/// cached and reused until any of the reactive values are changed again.
/// </summary>
/// <param name="computeMethod">The name of the method to use as the computation function.</param>
/// <remarks>
/// <para>
/// A auto-property getter will not work; the getter must be defined as a basic <c>get;</c> in order for the code
/// generator to run correctly.
/// </para>
/// <para>
/// Optionally, a setter can be defined for derived properties. This will override the value of the derived property
/// until the next time it recomputes due to a dependency changing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyComponent : ReactiveComponent
/// {
/// 	[Reactive]
/// 	public string MyProperty { get; set; } = "";
/// &#160;
/// 	[Reactive, Derived(nameof(_myPropertyCapitalized)]
/// 	public string MyPropertyCapitalized { get; } = "";
/// &#160;
/// 	private string _myPropertyCapitalized()
/// 	{
/// 		return MyProperty.ToUpper();
/// 	}
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public sealed class DerivedAttribute(string computeMethod) : Attribute
{
	/// <summary>
	/// The name of the method to use as a derived property's compute function.
	/// </summary>
	internal readonly string ComputeMethod = computeMethod;
}
#endif
