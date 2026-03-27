using System.Buffers;
using System.Threading;

namespace Sandbox.Reactivity.Internals;

/// <summary>
/// A function that runs whenever any of the dependencies that were read during its execution have changed.
/// </summary>
/// <seealso cref="Reactive.Effect(Action)" />
/// <seealso cref="Reactive.Effect(Func{Action?})" />
internal partial class Effect : IReaction, IDisposable
{
	/// <summary>
	/// The list of effects that were created while this effect was executing.
	/// </summary>
	private readonly List<Effect> _children = [];

	/// <summary>
	/// The function to call when this effect runs, if any.
	/// </summary>
	private readonly Func<Action?>? _fn;

	/// <summary>
	/// Whether this effect will track reads of reactive objects during execution to add as dependencies.
	/// </summary>
	internal readonly bool ShouldTrackDependencies;

	/// <summary>
	/// The current cancellation token source for this effect, if any.
	/// </summary>
	private CancellationTokenSource? _cancelSource;

	/// <summary>
	/// The read value of each dependency as this effect was running. Used if this effect returns a teardown function.
	/// </summary>
	private List<object?>? _capturedValues;

	/// <summary>
	/// The teardown function that was returned in the last run. This can be non-null before the first run if one was
	/// specified during instantiation.
	/// </summary>
	private Action? _teardown;

	internal Effect(Func<Action?>? fn, Effect? parent, bool shouldTrackDependencies, Action? overrideTeardown = null)
	{
		_fn = fn;
		ShouldTrackDependencies = shouldTrackDependencies;
		_teardown = overrideTeardown;

		if (_fn == null)
		{
			// calling Run with no function will not update the state, we'll do it here since no dependencies means it's
			// always up to date
			State = ReactionState.UpToDate;
		}

		parent?._children.Add(this);
	}

	/// <summary>
	/// Whether this effect is disposed and can no longer run.
	/// </summary>
	internal bool IsDisposed { get; private set; }

	/// <summary>
	/// Returns a cancellation token for this effect that will cancel when it re-runs, or when it disposes.
	/// </summary>
	public CancellationToken CancelToken =>
		IsDisposed ? CancellationToken.None : (_cancelSource ??= new CancellationTokenSource()).Token;

	public void Dispose()
	{
		Dispose(true);
	}

	public List<IProducer> Dependencies { get; } = [];

	public uint ReadVersion { get; private set; }

	public ReactionState State { get; set; }

	bool IReaction.IsConnectedToEffect => true;

	void IReaction.OnDependencyChanged(ReactionState newState)
	{
		if (IsDisposed || State < newState)
		{
			// disposed or this effect has already been scheduled to run
			return;
		}

		// cancel any async code that started in this effect immediately instead of waiting for it to flush. if an async
		// function resumes execution between the time a dependency changed and this effect ran its teardown function,
		// it would end up reading the updated value before being cancelled. proper async code shouldn't really read any
		// data that could mutate during its execution anyway, but we'll try to account for it here
		if (_cancelSource != null)
		{
			_cancelSource.Cancel();
			_cancelSource.Dispose();
			_cancelSource = null;
		}

		State = newState;
		Reactive.Runtime.ScheduleEffect(this);
	}

	/// <summary>
	/// Called when the effect has been instantiated for the first time, or when a dependency changes.
	/// </summary>
	public void Run()
	{
		if (IsDisposed)
		{
			return;
		}

		Teardown();
		using var _ = new ExecutionScope(this);

		if (_fn != null)
		{
			_teardown = _fn();
		}

		// capture the value of each dependency if there's a teardown function so we can restore it later
		if (_teardown != null && Dependencies.Count > 0)
		{
			_capturedValues ??= new List<object?>(Dependencies.Count);

			foreach (var producer in Dependencies)
			{
				_capturedValues.Add(producer.NonReactiveValue);
			}
		}
	}

	/// <summary>
	/// Disposes this effect along with any child effects, preventing them from ever running again.
	/// </summary>
	/// <param name="performTeardown">
	/// Whether to run the teardown function for this effect, if any. Child effects always run their teardown functions.
	/// </param>
	public void Dispose(bool performTeardown)
	{
		if (IsDisposed)
		{
			return;
		}

		IsDisposed = true;
#if DEBUG && SANDBOX
		OnDisposed?.Invoke();

		OnChildEffectCreated = null;
		OnRerun = null;
		OnDisposed = null;
#endif
		if (performTeardown)
		{
			Teardown();
		}
	}

	/// <summary>
	/// Disposes of any child effects, runs the teardown function if possible, and removes any dependencies + this
	/// effect as a reaction to them.
	/// </summary>
	private void Teardown()
	{
		// perform cancellation
		if (_cancelSource != null)
		{
			_cancelSource.Cancel();
			_cancelSource.Dispose();
			_cancelSource = null;
		}

		// clear any child effects
		if (_children.Count > 0)
		{
			foreach (var child in _children)
			{
				child.Dispose();
			}

			_children.Clear();
		}

		// run teardown function if any
		if (_teardown != null)
		{
			if (_capturedValues is not { Count: > 0 })
			{
				_teardown();
				_teardown = null;
			}
			else
			{
				// save current dependency values and restore captured values
				var count = _capturedValues.Count;
				var currentValues = ArrayPool<object?>.Shared.Rent(count);

				for (var i = 0; i < count; i++)
				{
					var producer = Dependencies[i];

					currentValues[i] = producer.NonReactiveValue;
					producer.NonReactiveValue = _capturedValues[i];
				}

				Reactive.Runtime.IsRunningTeardown = true;

				try
				{
					_teardown();
				}
				finally
				{
					// restore current values
					for (var i = 0; i < count; i++)
					{
						Dependencies[i].NonReactiveValue = currentValues[i];
					}

					ArrayPool<object?>.Shared.Return(currentValues);
					_teardown = null;
					_capturedValues.Clear();

					Reactive.Runtime.IsRunningTeardown = false;
				}
			}
		}

		// clear any dependencies
		if (Dependencies.Count > 0)
		{
			foreach (var producer in Dependencies)
			{
				producer.RemoveReaction(this);
			}

			Dependencies.Clear();
		}
	}

#if DEBUG && SANDBOX
	/// <summary>
	/// Called after an effect with no parent has been created.
	/// </summary>
	public static Action<Effect>? OnEffectRootCreated;

	/// <summary>
	/// Whether this effect has ever run.
	/// </summary>
	private bool _hasEverRun;

	/// <summary>
	/// Called when an effect was created inside this effect.
	/// </summary>
	internal event Action<Effect>? OnChildEffectCreated;

	/// <summary>
	/// Called when this effect re-runs. Does not call for the first run.
	/// </summary>
	internal event Action? OnRerun;

	/// <summary>
	/// Called when this effect is disposed.
	/// </summary>
	internal event Action? OnDisposed;

	public string? Name { get; set; }

	public string? Icon { get; set; }

	public string? Location { get; set; }

	public object? Parent { get; set; }

	public PropertyDescription? Container { get; set; }
#endif
}
