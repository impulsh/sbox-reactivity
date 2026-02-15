using Sandbox.Reactivity.Internals;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity;

/// <summary>
/// A reactive object that derives its value from a function. Whenever any reactive values inside the function change,
/// the function will be re-run to get the new value.
/// </summary>
/// <typeparam name="T">The type of value this derived state contains.</typeparam>
#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public sealed class Derived<T> : IProducer<T>, IWritableProducer<T>, IReaction, IState<T>
{
	/// <summary>
	/// The function to call when this derived state needs to recompute its value.
	/// </summary>
	private readonly Func<T> _compute;

	private readonly List<IProducer> _dependencies = [];

	private readonly List<IReaction> _reactions = [];

	private bool _isConnectedToEffect;

	private uint _readVersion;

	private ReactionState _state = ReactionState.Stale;

	/// <summary>
	/// The last computed value.
	/// </summary>
	private T _value = default!;

	/// <inheritdoc cref="IProducer.WriteVersion" />
	/// <remarks>This will not change if this derived state recomputes to the same value.</remarks>
	private uint _writeVersion;

	internal Derived(Func<T> compute)
	{
		_compute = compute;
	}

	List<IReaction> IProducer.Reactions => _reactions;

	uint IProducer.WriteVersion => _writeVersion;

	object? IProducer.NonReactiveValue
	{
		get => _value;
		set => _value = (T)value!;
	}

	/// <summary>
	/// The current value of this derived state. Reading this inside of an effect will cause it to add this state as a
	/// dependency. You can override the current value and it will remain until the next time this derived state
	/// recomputes its value due to a dependency changing.
	/// </summary>
	public T Value
	{
		get
		{
			this.TrackRead();

			// always check here since we could be evaluating outside a tracking context
			if (this.ShouldRun)
			{
				((IReaction)this).Run();
			}

			return _value;
		}
		set
		{
			if (EqualityComparer<T>.Default.Equals(_value, value))
			{
				return;
			}

			if (_state == ReactionState.Stale)
			{
				// if we're being assigned an overridden value while it's stale, this derived state has either not
				// computed yet, or is already potentially changing its dependencies due to a reactivity propagation.
				// we'll compute in order to track the correct dependencies
				((IReaction)this).Run();
			}

			_value = value;
			_writeVersion = ++Reactive.Runtime.Version;
			_state = _isConnectedToEffect ? ReactionState.UpToDate : ReactionState.PossiblyStale;

			this.PropagateReactivity();
		}
	}

	void IProducer.AddReaction(IReaction reaction)
	{
		if (!reaction.IsConnectedToEffect || _reactions.Contains(reaction))
		{
			return;
		}

		_reactions.Add(reaction);

		if (!_isConnectedToEffect)
		{
			// we're now connected to an effect; re-add this reaction to all of our dependencies
			_isConnectedToEffect = true;

			foreach (var dependency in _dependencies)
			{
				dependency.AddReaction(this);
			}
		}
	}

	void IProducer.RemoveReaction(IReaction reaction)
	{
		if (!_reactions.Remove(reaction))
		{
			return;
		}

		if (_reactions.Count == 0)
		{
			// this derived state is no longer being used in an effect; remove it from its dependencies to avoid
			// unnecessary recomputation
			_isConnectedToEffect = false;
			_state = ReactionState.PossiblyStale;

			foreach (var dependency in _dependencies)
			{
				dependency.RemoveReaction(this);
			}
		}
	}

	List<IProducer> IReaction.Dependencies => _dependencies;

	uint IReaction.ReadVersion => _readVersion;

	ReactionState IReaction.State
	{
		get => _state;
		set => _state = value;
	}

	bool IReaction.IsConnectedToEffect => _isConnectedToEffect;

	void IReaction.OnDependencyChanged(ReactionState newState)
	{
		if (_state < newState)
		{
			return;
		}

		_state = newState;
		this.PropagateReactivity(ReactionState.PossiblyStale);
	}

	void IReaction.Run()
	{
		foreach (var dep in _dependencies)
		{
			dep.RemoveReaction(this);
		}

		_dependencies.Clear();

		var previousReaction = Reactive.Runtime.CurrentReaction;
		Reactive.Runtime.CurrentReaction = this;

		try
		{
			var oldValue = _value;
			_value = _compute();

			// we can't know for sure if we're up to date if we're not connected to an effect; we remove ourselves as
			// reactions to dependencies to avoid recomputation in this case
			_state = _isConnectedToEffect ? ReactionState.UpToDate : ReactionState.PossiblyStale;

			if (!EqualityComparer<T>.Default.Equals(oldValue, _value))
			{
				_writeVersion = ++Reactive.Runtime.Version;
			}

			_readVersion = Reactive.Runtime.Version;
		}
		finally
		{
			Reactive.Runtime.CurrentReaction = previousReaction;
		}
	}
}
