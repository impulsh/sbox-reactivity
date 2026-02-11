namespace Sandbox.Reactivity.Internals;

internal enum ReactionState
{
	/// <summary>
	/// This reaction was manually scheduled to run, or a direct dependency of this reaction has changed its value
	/// and needs to re-run.
	/// </summary>
	Stale,

	/// <summary>
	/// This reaction can't definitively determine whether it's stale. Either this reaction is not monitoring changes to
	/// dependencies for optimization, or because an ancestor dependency has propagated a change and the direct
	/// dependencies need to be checked to see if they've actually changed.
	/// </summary>
	PossiblyStale,

	/// <summary>
	/// All dependencies for this reaction have not changed since the last time it ran.
	/// </summary>
	UpToDate,
}
