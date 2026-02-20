#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity;

/// <summary>
/// An object that contains a reactive value. Reading the value inside an effect will cause it to re-run when it
/// changes.
/// </summary>
/// <typeparam name="T">The type of value this object contains.</typeparam>
/// <remarks>
/// This can be used to abstract over a <see cref="State{T}" /> or <see cref="Derived{T}" /> as needed.
/// </remarks>
#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public interface IState<T>
{
	/// <summary>
	/// The current value.
	/// </summary>
	T Value { get; set; }
}

/// <inheritdoc cref="IState{T}" />
#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public interface IReadOnlyState<out T>
{
	/// <inheritdoc cref="IState{T}.Value" />
	T Value { get; }
}
