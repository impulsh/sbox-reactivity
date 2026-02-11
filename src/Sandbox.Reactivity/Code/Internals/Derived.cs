namespace Sandbox.Reactivity.Internals;

/// <summary>
/// A reactive object that derives its value from a function. Whenever any reactive values inside the function change,
/// the function will be re-run to get the new value.
/// </summary>
/// <typeparam name="T">The type of value this derived state contains.</typeparam>
internal sealed class Derived<T> : IProducer<T>, IWritableProducer<T>, IReaction, IState<T>
{
	/// <summary>
	/// The function to call when this derived state needs to recompute its value.
	/// </summary>
	private readonly Func<T> _compute;

	/// <summary>
	/// The last computed value.
	/// </summary>
	private T _value = default!;

	internal Derived(Func<T> compute)
	{
		_compute = compute;
	}

	public List<IReaction> Reactions { get; } = [];

	/// <summary>
	/// The version at which this derived's value last changed. This won't change if it recomputes to the same value.
	/// </summary>
	public uint WriteVersion { get; private set; }

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

			if (State == ReactionState.Stale)
			{
				// if we're being assigned an overridden value while it's stale, this derived state has either not
				// computed yet, or is already potentially changing its dependencies due to a reactivity propagation.
				// we'll compute in order to track the correct dependencies
				((IReaction)this).Run();
			}

			_value = value;
			WriteVersion = ++Reactive.Runtime.Version;
			State = IsConnectedToEffect ? ReactionState.UpToDate : ReactionState.PossiblyStale;

			this.PropagateReactivity();
		}
	}

	public void AddReaction(IReaction reaction)
	{
		if (!reaction.IsConnectedToEffect || Reactions.Contains(reaction))
		{
			return;
		}

		Reactions.Add(reaction);

		if (!IsConnectedToEffect)
		{
			// we're now connected to an effect; re-add this reaction to all of our dependencies
			IsConnectedToEffect = true;

			foreach (var dependency in Dependencies)
			{
				dependency.AddReaction(this);
			}
		}
	}

	public void RemoveReaction(IReaction reaction)
	{
		if (!Reactions.Remove(reaction))
		{
			return;
		}

		if (Reactions.Count == 0)
		{
			// this derived state is no longer being used in an effect; remove it from its dependencies to avoid
			// unnecessary recomputation
			IsConnectedToEffect = false;
			State = ReactionState.PossiblyStale;

			foreach (var dependency in Dependencies)
			{
				dependency.RemoveReaction(this);
			}
		}
	}

	public List<IProducer> Dependencies { get; } = [];

	public uint ReadVersion { get; private set; }

	public ReactionState State { get; set; } = ReactionState.Stale;

	public bool IsConnectedToEffect { get; private set; }

	void IReaction.OnDependencyChanged(ReactionState newState)
	{
		if (State < newState)
		{
			return;
		}

		State = newState;
		this.PropagateReactivity(ReactionState.PossiblyStale);
	}

	void IReaction.Run()
	{
		foreach (var dep in Dependencies)
		{
			dep.RemoveReaction(this);
		}

		Dependencies.Clear();

		var previousReaction = Reactive.Runtime.CurrentReaction;
		Reactive.Runtime.CurrentReaction = this;

		try
		{
			var oldValue = _value;
			_value = _compute();

			// we can't know for sure if we're up to date if we're not connected to an effect; we remove ourselves as
			// reactions to dependencies to avoid recomputation in this case
			State = IsConnectedToEffect ? ReactionState.UpToDate : ReactionState.PossiblyStale;

			if (!EqualityComparer<T>.Default.Equals(oldValue, _value))
			{
				WriteVersion = ++Reactive.Runtime.Version;
			}

			ReadVersion = Reactive.Runtime.Version;
		}
		finally
		{
			Reactive.Runtime.CurrentReaction = previousReaction;
		}
	}
}
