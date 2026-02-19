#if SANDBOX
using Sandbox.Reactivity.Internals;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity;

#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public static class GameObjectExtensions
{
	extension(GameObject go)
	{
		/// <summary>
		/// Sends an event to this game object.
		/// </summary>
		/// <param name="eventData">The event to send.</param>
		/// <typeparam name="T">
		/// The type of event to send. Any registered event functions that accept an event that is assignable to this
		/// type will also be called.
		/// </typeparam>
		public void SendDirect<T>(T eventData)
		{
			if (GameObjectEventManager.Get(go) is not { } handler)
			{
				return;
			}

			handler.Dispatch(eventData);
		}

		/// <summary>
		/// Sends an event to this game object and all of its ancestors.
		/// </summary>
		/// <param name="eventData">The event to send.</param>
		/// <typeparam name="T">
		/// The type of event to send. Any registered event functions that accept an event that is assignable to this
		/// type will also be called.
		/// </typeparam>
		/// <remarks>
		/// An event function can stop propagation of this event to its ancestors.
		/// </remarks>
		public void SendUp<T>(T eventData)
		{
			var next = go;

			while (next != null)
			{
				if (GameObjectEventManager.Get(next) is { } handler)
				{
					if (handler.Dispatch(eventData))
					{
						return;
					}
				}

				next = next.Parent;
			}
		}

		/// <summary>
		/// Sends an event to this game object and all of its descendants.
		/// </summary>
		/// <param name="eventData">The event to send.</param>
		/// <typeparam name="T">
		/// The type of event to send. Any registered event functions that accept an event that is assignable to this
		/// type will also be called.
		/// </typeparam>
		/// <remarks>
		/// An event function can stop propagation of this event to its own descendants.
		/// </remarks>
		public void SendDown<T>(T eventData)
		{
			if (GameObjectEventManager.Get(go) is { } handler)
			{
				if (handler.Dispatch(eventData))
				{
					return;
				}
			}

			foreach (var child in go.Children)
			{
				child.SendDown(eventData);
			}
		}
	}
}
#endif
