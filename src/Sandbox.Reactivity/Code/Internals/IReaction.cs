using Sandbox.Reactivity.Internals.Runtimes;

namespace Sandbox.Reactivity.Internals;

/// <summary>
/// An object that reacts to changes made by any <see cref="IProducer" /> added as dependencies.
/// </summary>
internal interface IReaction
{
	/// <summary>
	/// The list of <see cref="IProducer" />s that this reaction depends on. If any of the producers change their value,
	/// this reaction will run.
	/// </summary>
	List<IProducer> Dependencies { get; }

	/// <summary>
	/// The <see cref="Runtime.Version" /> at which this reaction last ran. This is used to check if this reaction has
	/// any dependencies out of date. Reactions updating their read version do not modify the global version.
	/// </summary>
	uint ReadVersion { get; }

	/// <summary>
	/// Whether this reaction might potentially need to re-run.
	/// </summary>
	ReactionState State { get; set; }

	/// <summary>
	/// Whether this reaction is an effect or is depended upon by an effect, either directly or through an ancestor.
	/// Reactions can use this to avoid running if they won't cause any observable difference.
	/// </summary>
	bool IsConnectedToEffect { get; }

	/// <summary>
	/// Called when one of this reaction's dependencies (or ancestor dependencies) have changed.
	/// </summary>
	/// <param name="state">The new state that is being propagated by the dependency.</param>
	void OnDependencyChanged(ReactionState state);

	/// <summary>
	/// Called when this reaction should run due to a direct dependency changing its value.
	/// </summary>
	void Run();
}

internal static class ReactionExtensions
{
	extension(IReaction reaction)
	{
		/// <summary>
		/// <para>
		/// Whether this reaction should call its <see cref="IReaction.Run" /> method. The result is derived from its
		/// current <see cref="IReaction.State" />, and whether any of its direct dependencies have changed their value.
		/// </para>
		/// <para>
		/// Some reactions can also produce values (e.g. derived states), so they will be run if possible in order to
		/// bring them up to date before checking if its value changed.
		/// </para>
		/// </summary>
		internal bool ShouldRun
		{
			get
			{
				switch (reaction.State)
				{
					case ReactionState.Stale:
						// definitely run if we're completely stale
						return true;
					case ReactionState.PossiblyStale:
					{
						// check if any direct dependencies changed their value
						foreach (var producer in reaction.Dependencies)
						{
							// run any reactions to make sure their values are up to date
							if (producer is IReaction { ShouldRun: true } producerReaction)
							{
								producerReaction.Run();
							}

							if (producer.WriteVersion > reaction.ReadVersion)
							{
								return true;
							}
						}

						if (reaction.IsConnectedToEffect)
						{
							// one of our ancestor dependencies changed but didn't cause an observable difference to
							// this reaction, so we're up to date
							reaction.State = ReactionState.UpToDate;
						}

						// reactions that aren't used in any effects remove themselves as a reaction to their
						// dependencies to avoid unnecessary runs. in this case, we always need to check dependencies
						// in order to determine if we should run again
						return false;
					}
					case ReactionState.UpToDate:
						return false;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// Adds a producer as a dependency to this reaction.
		/// </summary>
		/// <param name="producer">The producer to add as a dependency.</param>
		internal void AddDependency(IProducer producer)
		{
			if (!reaction.Dependencies.Contains(producer))
			{
				reaction.Dependencies.Add(producer);
			}
		}
	}
}
