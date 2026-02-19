#if SANDBOX
using Sandbox.Reactivity.Internals;
using static Sandbox.Reactivity.Reactive;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity;

/// <summary>
/// The base component used to opt into the reactivity system. This allows usage of the reactive property attributes,
/// and automatically creates an effect root in <see cref="OnActivate" />.
/// </summary>
/// <seealso cref="ReactiveAttribute" />
/// <seealso cref="DerivedAttribute" />
#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public abstract class ReactiveComponent : Component, IReactivePropertyContainer
{
	/// <summary>
	/// The disposable for this component's effect root that exists while it's enabled.
	/// </summary>
	private IDisposable? _effectRoot;

	Dictionary<int, IProducer> IReactivePropertyContainer.Producers { get; } = [];

	/// <inheritdoc cref="GameObjectExtensions.SendDirect" />
	public void SendDirect<T>(T eventData)
	{
		GameObject?.SendDirect(eventData);
	}

	/// <inheritdoc cref="GameObjectExtensions.SendUp" />
	public void SendUp<T>(T eventData)
	{
		GameObject?.SendUp(eventData);
	}

	/// <inheritdoc cref="GameObjectExtensions.SendDown" />
	public void SendDown<T>(T eventData)
	{
		GameObject?.SendDown(eventData);
	}

	/// <summary>
	/// Runs a function when this component's game object receives an event.
	/// </summary>
	/// <param name="callback">
	/// The function to run when the event is received. If the function returns <c>true</c>, the event will prevent
	/// further propagation to ancestor/descendant game objects.
	/// </param>
	/// <typeparam name="T">
	/// The type of event to listen to. Any received game objects that are assignable to this type will run the
	/// function.
	/// </typeparam>
	public void OnEvent<T>(Func<T, bool> callback)
	{
		if (GameObject is not { } go)
		{
			return;
		}

		var manager = GameObjectEventManager.GetOrCreate(go);

		Effect(() =>
		{
			manager.Add(callback);

			return () => { manager.Remove(callback); };
		});
	}

	protected sealed override void OnEnabled()
	{
		_effectRoot?.Dispose();
		_effectRoot = EffectRoot(OnActivate);
	}

	protected sealed override void OnDisabled()
	{
		_effectRoot?.Dispose();
		_effectRoot = null;
	}

	/// <summary>
	/// Called inside an effect root when this component is enabled, allowing for effects to be created. When this
	/// component is disabled, the effect root (and all of its descendants) are disposed.
	/// </summary>
	protected virtual void OnActivate()
	{
	}
}
#endif
