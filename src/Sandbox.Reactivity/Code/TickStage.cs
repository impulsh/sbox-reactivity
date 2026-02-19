namespace Sandbox.Reactivity;

/// <summary>
/// Describes at what point during a game tick a function should run.
/// </summary>
public enum TickStage
{
	/// <summary>
	/// While physics is being simulated. This happens before game ticks.
	/// </summary>
	Physics,

	/// <summary>
	/// The beginning of a tick, after physics.
	/// </summary>
	Start,

	/// <summary>
	/// The end of a tick.
	/// </summary>
	End,
}
