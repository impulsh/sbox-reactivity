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
	/// The dictionary of static reactive property member IDs to their backing producers.
	/// </summary>
	private static readonly Dictionary<int, IProducer> StaticsContainer = new();

	/// <summary>
	/// Returns an <see cref="IProducer" /> for the given member and target, or creates one if it doesn't exist. What
	/// kind of producer depends on the attributes that are on the member.
	/// </summary>
	/// <param name="memberIdent">The unique ID of the member.</param>
	/// <param name="target">The object to store the producer object on. This can be null for static members.</param>
	/// <param name="defaultValue">The default value to use when creating a producer.</param>
	/// <typeparam name="T">The return type of the member.</typeparam>
	/// <typeparam name="TProducer">The type to cast the producer to.</typeparam>
	/// <returns>
	/// An <see cref="IProducer" /> that can be used as a backing reactive object for the given member and target.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown if <paramref name="target" /> is an object that doesn't implement
	/// <see cref="IReactivePropertyContainer" />.
	/// </exception>
	private static TProducer GetOrCreateProducer<T, TProducer>(int memberIdent, object? target, T defaultValue)
		where TProducer : IProducer
	{
		// find existing producer if possible
		var states = target switch
		{
			// static property
			null => StaticsContainer,
			// instance property
			IReactivePropertyContainer container => container.Producers,
			// instance property that doesnt implement IReactivePropertyContainer; nowhere to put the producer objects
			_ => throw new InvalidOperationException(
				$"Reactive property defined on unsupported type {target.GetType()}"),
		};

		if (states.TryGetValue(memberIdent, out var state))
		{
			return (TProducer)state;
		}

		// none exists, time to make one
		if (TypeLibrary.GetMemberByIdent(memberIdent) is not PropertyDescription property)
		{
			throw new InvalidOperationException($"Could not find member with identity {memberIdent}");
		}

		var containingType = property.TypeDescription;

		if (property.IsStatic && containingType.IsGenericType)
		{
			throw new InvalidOperationException(
				$"Static reactive properties on generic type {containingType.FullName} are not supported");
		}

		IProducer producer;

		// check if we need to create a derived state and wire up the computation method
		if (property.GetCustomAttribute<DerivedAttribute>() is { ComputeMethod: { } computeMethodName })
		{
			var method = target switch
			{
				null => containingType.GetStaticMethod(computeMethodName),
				_ => containingType.GetMethod(computeMethodName),
			};

			if (method == null)
			{
				throw new InvalidOperationException(
					$"Could not find derived compute method {computeMethodName} on {containingType.Name}.{property.Name}");
			}

			producer = new Derived<T>(method.CreateDelegate<Func<T>>(target));
			producer.SetDebugInfo(location: $"{method.SourceFile}:{method.SourceLine}");
		}
		else
		{
			producer = new State<T>(defaultValue);
			producer.SetDebugInfo(location: $"{property.SourceFile}:{property.SourceLine}");
		}

		producer.SetDebugInfo(property.Name,
			parent: target ?? property.TypeDescription.TargetType,
			container: property);

		states.Add(memberIdent, producer);
		return (TProducer)producer;
	}

	/// <summary>
	/// Returns a value from an object's backing producer, enabling reactivity.
	/// </summary>
	/// <param name="wrapped">The original property being accessed.</param>
	/// <typeparam name="T">The type of property being accessed.</typeparam>
	/// <returns>The value of the backing producer.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the object does not implement <see cref="IReactivePropertyContainer" />.
	/// </exception>
#if JETBRAINS_ANNOTATIONS
	[UsedImplicitly]
#endif
	public static T GetReactiveValue<T>(in WrappedPropertyGet<T> wrapped)
	{
		var producer = GetOrCreateProducer<T, IProducer<T>>(wrapped.MemberIdent, wrapped.Object, wrapped.Value);
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
		var producer = GetOrCreateProducer<T, IWritableProducer<T>>(wrapped.MemberIdent, wrapped.Object, wrapped.Value);
		producer.Value = wrapped.Value;
	}
}
#endif
