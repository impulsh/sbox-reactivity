using Sandbox.Reactivity.Internals;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity;

/// <summary>
/// A reactive object that contains a value that can be changed at any time.
/// </summary>
/// <typeparam name="T">The type of value this state contains.</typeparam>
/// <seealso cref="Reactive.State" />
#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public sealed class State<T> : IProducer<T>, IWritableProducer<T>, IState<T>
{
	private readonly List<IReaction> _reactions = [];

	/// <summary>
	/// The stored value of this state.
	/// </summary>
	private T _value;

	private uint _writeVersion;

	internal State(T initialValue)
	{
		_value = initialValue;
	}

	List<IReaction> IProducer.Reactions => _reactions;

	/// <summary>
	/// The version at which this state's value last changed.
	/// </summary>
	uint IProducer.WriteVersion => _writeVersion;

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
			_writeVersion = ++Reactive.Runtime.Version;

			this.PropagateReactivity();
		}
	}

	void IProducer.AddReaction(IReaction reaction)
	{
		if (!reaction.IsConnectedToEffect)
		{
			return;
		}

		if (!_reactions.Contains(reaction))
		{
			_reactions.Add(reaction);
		}
	}

	void IProducer.RemoveReaction(IReaction reaction)
	{
		_reactions.Remove(reaction);
	}
}
