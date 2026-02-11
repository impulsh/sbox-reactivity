using Sandbox.Reactivity.Internals.Runtimes;

namespace Sandbox.Reactivity.Internals;

/// <summary>
/// A reactive object that produces a value and can be depended upon by a <see cref="IReaction" />.
/// </summary>
internal interface IProducer
{
	/// <summary>
	/// The list of <see cref="IReaction" />s that have added this producer as a dependency.
	/// </summary>
	List<IReaction> Reactions { get; }

	/// <summary>
	/// The <see cref="Runtime.Version" /> at which this producer's value last changed. When a producer updates its
	/// write version, it will increment the global version.
	/// </summary>
	uint WriteVersion { get; }

	/// <summary>
	/// The producer's raw current value without any reactivity tracking.
	/// </summary>
	object? NonReactiveValue { get; set; }

	/// <summary>
	/// Adds a reaction to notify when this producer's value changes.
	/// </summary>
	/// <param name="reaction">The reaction to add.</param>
	void AddReaction(IReaction reaction);

	/// <summary>
	/// Removes a reaction from being notified when this producer's value changes.
	/// </summary>
	/// <param name="reaction">The reaction to remove.</param>
	void RemoveReaction(IReaction reaction);
}

/// <inheritdoc />
/// <typeparam name="T">The type of this producer's value.</typeparam>
internal interface IProducer<out T> : IProducer
{
	/// <summary>
	/// The producer's current value.
	/// </summary>
	T Value { get; }
}

/// <inheritdoc />
/// <remarks>This producer can be modified directly by users to cause reactions.</remarks>
internal interface IWritableProducer<in T> : IProducer
{
	/// <summary>
	/// Sets this producer's current value.
	/// </summary>
	T Value { set; }
}

internal static class ProducerExtensions
{
	extension(IProducer producer)
	{
		/// <summary>
		/// Adds this producer as a dependency to any current reaction being tracked.
		/// </summary>
		internal void TrackRead()
		{
			if (!Reactive.Runtime.IsUntracking && Reactive.Runtime.CurrentReaction is { } reaction)
			{
				producer.AddReaction(reaction);
				reaction.AddDependency(producer);
			}
		}

		/// <summary>
		/// Calls <see cref="IReaction.OnDependencyChanged" /> on any reactions that this producer is a dependency of.
		/// Reactions can schedule themselves to run and/or also propagate reactivity to their children if they are
		/// producers themselves.
		/// </summary>
		/// <param name="state">
		/// The state that should be assigned to reactions. This should be <see cref="ReactionState.Stale" />
		/// if you're propagating due to a direct change. Producers that change their value based on a reaction should
		/// propagate <see cref="ReactionState.PossiblyStale" /> to allow for its value to be evaluated only when
		/// needed.
		/// </param>
		internal void PropagateReactivity(ReactionState state = ReactionState.Stale)
		{
			foreach (var reaction in producer.Reactions)
			{
				// reactions will check if the new state is applicable (i.e. they're not less stale than the new state)
				reaction.OnDependencyChanged(state);
			}
		}
	}
}
