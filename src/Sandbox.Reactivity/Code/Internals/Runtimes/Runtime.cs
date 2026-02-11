namespace Sandbox.Reactivity.Internals.Runtimes;

internal sealed class Runtime : IDisposable
{
	/// <summary>
	/// Effects that are waiting to run due to reactivity changes.
	/// </summary>
	private readonly Queue<Effect> _pendingEffects = new(16);

	/// <summary>
	/// Whether pending effects are currently being run.
	/// </summary>
	private bool _isFlushing;

	/// <summary>
	/// The currently executing effect.
	/// </summary>
	public Effect? CurrentEffect { get; set; }

	/// <summary>
	/// The currently executing reaction.
	/// </summary>
	public IReaction? CurrentReaction { get; set; }

	/// <summary>
	/// A monotonically increasing counter that's incremented when an <see cref="IProducer" /> updates its current
	/// value.
	/// </summary>
	public uint Version { get; set; } = 1;

	/// <summary>
	/// Whether dependency tracking is currently disabled.
	/// </summary>
	public bool IsUntracking { get; set; }

	/// <summary>
	/// Whether a flush was scheduled to run at the end of the frame.
	/// </summary>
	public bool IsFlushScheduled { get; private set; }

	public void Dispose()
	{
		_pendingEffects.Clear();
		CurrentEffect = null;
		CurrentReaction = null;
		Version = uint.MaxValue;
		IsUntracking = true;
		IsFlushScheduled = false;
		_isFlushing = false;
	}

	public void ScheduleEffect(Effect effect)
	{
		_pendingEffects.Enqueue(effect);

		if (!IsFlushScheduled && !_isFlushing)
		{
			IsFlushScheduled = true;
		}
	}

	/// <summary>
	/// Empties the queue of effects that are scheduled to run due to one of their dependencies changing. This should
	/// only be run when you want an effect to re-run immediately after changing a reactive value.
	/// </summary>
	public void Flush()
	{
		if (_isFlushing)
		{
			return;
		}

		_isFlushing = true;
		IsFlushScheduled = false;

		try
		{
			while (_pendingEffects.TryDequeue(out var effect))
			{
				if (effect.ShouldRun)
				{
					effect.Run();
				}
			}
		}
		finally
		{
			_isFlushing = false;
		}
	}
}
