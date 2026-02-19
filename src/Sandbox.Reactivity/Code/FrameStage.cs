namespace Sandbox.Reactivity;

/// <summary>
/// Describes at what point during a frame a function should run.
/// </summary>
public enum FrameStage
{
	/// <summary>
	/// While transforms are being interpolated.
	/// </summary>
	Interpolation,

	/// <summary>
	/// At the start of the frame, after interpolation.
	/// </summary>
	Start,

	/// <summary>
	/// While bones are being updated.
	/// </summary>
	UpdateBones,

	/// <summary>
	/// At the end of the frame.
	/// </summary>
	End,
}
