#if SANDBOX
using System.Collections;

namespace Sandbox.Reactivity.Internals;

file sealed class EventList<T> : List<Func<T, bool>>;

/// <summary>
/// Maintains the list of registered event callbacks on a game object.
/// </summary>
internal sealed class GameObjectEventManager(GameObject go)
{
	/// <summary>
	/// The map of game objects to their event managers.
	/// </summary>
	private static readonly Dictionary<GameObject, GameObjectEventManager> RegisteredObjects = [];

	/// <summary>
	/// The map of types to the list of callbacks to run when used as an event.
	/// </summary>
	private readonly Dictionary<Type, IList> _handledEvents = [];

	/// <summary>
	/// The game object this event manager belongs to.
	/// </summary>
	private readonly GameObject _owningObject = go;

	/// <summary>
	/// Returns the event manager for the given game object.
	/// </summary>
	/// <param name="go">The game object to get the event handler for.</param>
	/// <returns>The event manager that corresponds to the given game object, or <c>null</c> if not found.</returns>
	public static GameObjectEventManager? Get(GameObject go)
	{
		return RegisteredObjects.GetValueOrDefault(go);
	}

	/// <summary>
	/// Returns an event manager for the given game object. If it doesn't already have one, it will be created.
	/// </summary>
	/// <param name="go">The game object to get an event handler for.</param>
	/// <returns>An event handler that corresponds to the given game object.</returns>
	public static GameObjectEventManager GetOrCreate(GameObject go)
	{
		if (!RegisteredObjects.TryGetValue(go, out var handler))
		{
			handler = new GameObjectEventManager(go);
			RegisteredObjects[go] = handler;
		}

		return handler;
	}

	/// <summary>
	/// Adds a function to run when an event is received.
	/// </summary>
	/// <param name="callback">The function to run.</param>
	/// <typeparam name="T">
	/// The event type the function accepts. The function will also run for any events that are assignable to this type.
	/// </typeparam>
	public void Add<T>(Func<T, bool> callback)
	{
		foreach (var type in TypeHierarchy<T>.Types)
		{
			if (!_handledEvents.TryGetValue(type, out var eventList))
			{
				eventList = new EventList<T>();
				_handledEvents[type] = eventList;
			}

			((EventList<T>)eventList).Add(callback);
		}
	}

	/// <summary>
	/// Removes a function from running when an event is received.
	/// </summary>
	/// <param name="callback">The function to remove.</param>
	/// <typeparam name="T">The event type the function accepts.</typeparam>
	public void Remove<T>(Func<T, bool> callback)
	{
		foreach (var type in TypeHierarchy<T>.Types)
		{
			if (!_handledEvents.TryGetValue(type, out var eventList))
			{
				continue;
			}

			var callbacks = (EventList<T>)eventList;

			if (callbacks.Remove(callback) && callbacks.Count == 0)
			{
				_handledEvents.Remove(type);
			}
		}

		if (_handledEvents.Count == 0)
		{
			RegisteredObjects.Remove(_owningObject);
		}
	}

	/// <summary>
	/// Runs any functions on this handler with the given event.
	/// </summary>
	/// <param name="eventData">The event object to use when running a function.</param>
	/// <typeparam name="T">
	/// The event type to pass to this event handler's functions. This will call any functions that accept a type that
	/// is assignable to this one.
	/// </typeparam>
	/// <returns>Whether a function has requested to stop propagation to other game objects.</returns>
	public bool Dispatch<T>(T eventData)
	{
		if (!_handledEvents.TryGetValue(typeof(T), out var eventList))
		{
			return false;
		}

		var stopPropagation = false;

		using (new UntrackScope())
		{
			foreach (var callback in (EventList<T>)eventList)
			{
				stopPropagation = callback(eventData) || stopPropagation;
			}
		}

		return stopPropagation;
	}
}
#endif
