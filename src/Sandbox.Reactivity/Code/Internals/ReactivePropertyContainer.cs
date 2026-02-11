#if SANDBOX
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity.Internals;

/// <summary>
/// Implemented by classes that support annotating properties with <see cref="ReactiveAttribute" />.
/// </summary>
internal interface IReactivePropertyContainer
{
	/// <summary>
	/// The dictionary of this container's reactive property member IDs to their backing producers.
	/// </summary>
	Dictionary<int, IProducer> Producers { get; }
}

// required to be public due to codegen
public static class ReactivePropertyContainer
{
	/// <summary>
	/// Checks whether the backing producer for a property should be a <see cref="Derived{T}" /> based on its attributes.
	/// </summary>
	/// <param name="container">The object that contains a producer dictionary.</param>
	/// <param name="memberIdent">The ID of the object to check.</param>
	/// <param name="derivedComputeMethod">
	/// The method to call on the object if a <see cref="Derived{T}" /> should be created,
	/// or <c>null</c> if a <see cref="State{T}" /> should be created.
	/// </param>
	/// <exception cref="InvalidOperationException">Thrown if the property could not be found</exception>
	private static void CheckShouldCreateDerived(
		IReactivePropertyContainer container,
		int memberIdent,
		out MethodDescription? derivedComputeMethod
	)
	{
		if (!TypeLibrary.TryGetType(container.GetType(), out var description))
		{
			throw new InvalidOperationException($"Could not find type for {container.GetType().Name}");
		}

		if (TypeLibrary.GetMemberByIdent(memberIdent) is not PropertyDescription property)
		{
			throw new InvalidOperationException(
				$"Could not find member identity {memberIdent} on type {description.Name}");
		}

		if (property.GetCustomAttribute<DerivedAttribute>() is { ComputeMethod: { } computeMethodName })
		{
			if (description.GetMethod(computeMethodName) is not { } method)
			{
				throw new InvalidOperationException(
					$"Could not find derived compute method {computeMethodName} on type {description.Name}");
			}

			derivedComputeMethod = method;
		}
		else
		{
			derivedComputeMethod = null;
		}
	}

	/// <summary>
	/// Returns a value from an object's backing producer, enabling reactivity.
	/// </summary>
	/// <param name="wrapped">The original property being accessed.</param>
	/// <typeparam name="T">The type of property being accessed.</typeparam>
	/// <returns>The value of the backing producer.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the object does not implement
	/// <see cref="IReactivePropertyContainer" />
	/// </exception>
#if JETBRAINS_ANNOTATIONS
	[UsedImplicitly]
#endif
	public static T GetReactiveValue<T>(in WrappedPropertyGet<T> wrapped)
	{
		if (wrapped.Object is not IReactivePropertyContainer { Producers: var states } container)
		{
			throw new InvalidOperationException("Reactive property defined on unsupported type");
		}

		if (states.TryGetValue(wrapped.MemberIdent, out var state))
		{
			return ((IProducer<T>)state).Value;
		}

		IProducer<T> producer;
		CheckShouldCreateDerived(container, wrapped.MemberIdent, out var derivedComputeMethod);

		if (derivedComputeMethod != null)
		{
			producer = new Derived<T>(derivedComputeMethod.CreateDelegate<Func<T>>(container));
		}
		else
		{
			producer = new State<T>(wrapped.Value);
		}

		states.Add(wrapped.MemberIdent, producer);
		return producer.Value;
	}

	/// <summary>
	/// Sets the value for an object's backing producer.
	/// </summary>
	/// <param name="wrapped">The original property being accessed.</param>
	/// <typeparam name="T">The type of property being accessed.</typeparam>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the object does not implement
	/// <see cref="IReactivePropertyContainer" />
	/// </exception>
#if JETBRAINS_ANNOTATIONS
	[UsedImplicitly]
#endif
	public static void SetReactiveValue<T>(in WrappedPropertySet<T> wrapped)
	{
		if (wrapped.Object is not IReactivePropertyContainer { Producers: var states } container)
		{
			throw new InvalidOperationException("Reactive property defined on unsupported type");
		}

		if (states.TryGetValue(wrapped.MemberIdent, out var state))
		{
			((IWritableProducer<T>)state).Value = wrapped.Value;
			return;
		}

		IWritableProducer<T> producer;
		CheckShouldCreateDerived(container, wrapped.MemberIdent, out var derivedComputeMethod);

		if (derivedComputeMethod != null)
		{
			producer = new Derived<T>(derivedComputeMethod.CreateDelegate<Func<T>>(container));
		}
		else
		{
			producer = new State<T>(wrapped.Value);
		}

		states.Add(wrapped.MemberIdent, producer);
		producer.Value = wrapped.Value;
	}
}
#endif
