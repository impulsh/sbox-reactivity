#if SANDBOX
using System.Diagnostics;
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
public abstract class ReactiveComponent(ReactiveComponent.ActivationStage activationStage) : Component,
	IReactivePropertyContainer
{
	/// <summary>
	/// Describes at what point during its lifecycle a <see cref="ReactiveComponent" /> will run its activation method.
	/// </summary>
	public enum ActivationStage
	{
		/// <summary>
		/// Calls <see cref="ReactiveComponent.OnActivate" /> during <see cref="Component.OnEnabled" />. This is the
		/// default.
		/// </summary>
		OnEnabled,

		/// <summary>
		/// Calls <see cref="ReactiveComponent.OnActivate" /> during <see cref="Component.OnStart" />. Useful for when
		/// you want to access data on other components on the same game object that is only available <i>after</i>
		/// they're enabled.
		/// </summary>
		/// <remarks>
		/// If the component is disabled and re-enabled, the activation will subsequently run during
		/// <see cref="Component.OnEnabled" /> since <see cref="Component.OnStart" /> is only called once ever.
		/// </remarks>
		OnStart,
	}

	/// <summary>
	/// When to run this component's activation method.
	/// </summary>
	private readonly ActivationStage _activationStage = activationStage;

	/// <summary>
	/// The disposable for this component's effect root that exists while it's enabled.
	/// </summary>
	private Effect? _effectRoot;

	/// <summary>
	/// Whether this component has ever called <see cref="Component.OnStart" />.
	/// </summary>
	private bool _hasStarted;

	protected ReactiveComponent()
		: this(ActivationStage.OnEnabled)
	{
	}

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
		var parent = Runtime.EnsureCurrentEffect();

		var effect = new Effect([StackTraceHidden] [DebuggerStepThrough]() =>
			{
				manager.Add(callback);

				return () => manager.Remove(callback);
			},
			parent,
			false);

		effect.SetDebugInfo($"OnEvent<{typeof(T).ToSimpleString(false)}>",
			"electric_bolt",
			new CallLocation(1),
			parent);

		effect.Run();
	}

	[StackTraceHidden]
	private void CreateRootEffect()
	{
		_effectRoot?.Dispose();

		// component lifetimes are managed by their owning game objects, it doesn't make sense to bind it to the
		// lifetime of the currently executing effect
		_effectRoot = new Effect([StackTraceHidden] [DebuggerStepThrough]() =>
			{
				OnActivate();
				return null;
			},
			null,
			false);

		_effectRoot.SetDebugInfo(DisplayInfo.For(this).Name,
			DisplayInfo.For(this).Icon,
			new CallLocation(GetType(), nameof(OnActivate)),
			this);

		_effectRoot.Run();
	}

	protected sealed override void OnEnabled()
	{
		if (_activationStage != ActivationStage.OnStart || _hasStarted)
		{
			CreateRootEffect();
		}
	}

	protected sealed override void OnStart()
	{
		if (_activationStage == ActivationStage.OnStart)
		{
			_hasStarted = true;

			CreateRootEffect();
		}
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
