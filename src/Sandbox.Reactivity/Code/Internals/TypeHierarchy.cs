#if SANDBOX
namespace Sandbox.Reactivity.Internals;

/// <summary>
/// Maintains a list of types that are assignable to the given type.
/// </summary>
internal static class TypeHierarchy<T>
{
	/// <summary>
	/// All types that are assignable to <typeparamref name="T"/>.
	/// </summary>
	[SkipHotload]
	// ReSharper disable once StaticMemberInGenericType
	public static readonly IEnumerable<Type> Types;

	static TypeHierarchy()
	{
		// since this is most likely going to be used for simple event types, we're going to assume that the hierarchy
		// won't be very large and that checking a list would be faster than hashing for a set
		var next = typeof(T);
		var hierarchy = new List<Type>();

		while (next != null)
		{
			hierarchy.Add(next);

			foreach (var type in next.GetInterfaces())
			{
				if (!hierarchy.Contains(type))
				{
					hierarchy.Add(type);
				}
			}

			next = next.BaseType;

			if (next == typeof(object))
			{
				break;
			}
		}

		Types = hierarchy;
	}
}
#endif
