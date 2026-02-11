namespace Sandbox.Reactivity.Internals;

/// <summary>
/// A reactive object that contains a value that can be changed at any time.
/// </summary>
/// <typeparam name="T">The type of value this state contains.</typeparam>
/// <seealso cref="Reactive.State" />
internal sealed class State<T> : IProducer<T>, IWritableProducer<T>, IState<T>
{
	/// <summary>
	/// The stored value of this state.
	/// </summary>
	private T _value;

	internal State(T initialValue)
	{
		_value = initialValue;
	}

	public List<IReaction> Reactions { get; } = [];

	/// <summary>
	/// The version at which this state's value last changed.
	/// </summary>
	public uint WriteVersion { get; private set; }

	object? IProducer.NonReactiveValue
	{
		get => _value;
		set => _value = (T)value!;
	}

	/// <summary>
	/// The current value of this state. Reading this inside of an effect will cause it to add this state as a
	/// dependency.
	/// </summary>
	/// <seealso cref="Reactive.Untrack()" />
	/// <seealso cref="Reactive.Untrack(Action)" />
	public T Value
	{
		get
		{
			this.TrackRead();
			return _value;
		}
		set
		{
			if (EqualityComparer<T>.Default.Equals(_value, value))
			{
				return;
			}

			_value = value;
			WriteVersion = ++Reactive.Runtime.Version;

			this.PropagateReactivity();
		}
	}

	void IProducer.AddReaction(IReaction reaction)
	{
		if (!reaction.IsConnectedToEffect)
		{
			return;
		}

		if (!Reactions.Contains(reaction))
		{
			Reactions.Add(reaction);
		}
	}

	void IProducer.RemoveReaction(IReaction reaction)
	{
		Reactions.Remove(reaction);
	}
}
