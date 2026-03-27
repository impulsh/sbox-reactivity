#if !SANDBOX
using System.Threading.Tasks;
#endif
using System.Diagnostics;
using System.Threading;
using Sandbox.Reactivity.Internals;
using Sandbox.Reactivity.Internals.Runtimes;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity;

/// <summary>
/// The singleton that manages the reactivity system.
/// </summary>
#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public static partial class Reactive
{
#if SANDBOX
	internal static readonly Runtime Runtime = new();
#else
	// ReSharper disable once InconsistentNaming
	private static readonly AsyncLocal<Runtime> _runtime = new();

	internal static Runtime Runtime => _runtime.Value ??= new Runtime();
#endif

	/// <summary>
	/// Creates a reactive value that can be changed at any time.
	/// </summary>
	/// <param name="initialValue">The initial value of the state.</param>
	/// <param name="name">The display name of this state for debug purposes.</param>
	/// <typeparam name="T">The type of value this state holds.</typeparam>
	/// <returns>The created state.</returns>
	public static State<T> State<T>(T initialValue, string? name = null)
	{
		var state = new State<T>(initialValue);
		state.SetDebugInfo(name, location: new CallLocation(1));

		return state;
	}

	/// <summary>
	/// <para>
	/// Creates a reactive value that gets its value from a compute function. The result of the function is stored and
	/// reused until any of the reactive values read inside of it change.
	/// </para>
	/// <para>
	/// A value can be assigned to this reactive value that will override its current value until the next time the
	/// compute function runs again.
	/// </para>
	/// </summary>
	/// <param name="compute">
	/// The function to call when the value needs to be computed. Whenever a reactive value that was read during its
	/// execution has changed, it will re-run.
	/// </param>
	/// <param name="name">The display name of this derived state for debug purposes.</param>
	/// <typeparam name="T">The type of value this derived state holds.</typeparam>
	/// <returns>The created derived state.</returns>
	public static Derived<T> Derived<T>(Func<T> compute, string? name = null)
	{
		var derived = new Derived<T>(compute);
		derived.SetDebugInfo(name, location: new CallLocation(1));

		return derived;
	}

	/// <inheritdoc cref="Effect(Action)" />
	/// <remarks>
	/// Effects may optionally return a teardown function that will run when a reactive value it depends on is about
	/// to change its value, or when the effect is being disposed.
	/// </remarks>
	public static void Effect(Func<Action?> callback)
	{
		var parent = Runtime.EnsureCurrentEffect();
		var effect = new Effect(callback, parent, true);

		effect.SetDebugInfo(location: new CallLocation(1), parent: parent);
		effect.Run();
	}

	/// <summary>
	/// Creates a function that will re-run whenever a reactive value that was read during its execution has changed.
	/// </summary>
	/// <param name="callback">The function to run.</param>
	/// <seealso cref="Effect(Func{Action})" />
	[StackTraceHidden]
	public static void Effect(Action callback)
	{
		Effect([StackTraceHidden] [DebuggerStepThrough]() =>
		{
			callback();
			return null;
		});
	}

	/// <summary>
	/// Creates an effect root that sets up a reactivity scope, allowing for effects to be created inside of it.
	/// </summary>
	/// <remarks>
	/// It should be noted that this does <b>not</b> do any reactivity tracking, reactive values read directly
	/// inside the root will not do anything.
	/// </remarks>
	/// <param name="callback">The function to call while inside the reactivity scope.</param>
	/// <returns>A disposable that disposes all descendant effects and stops reactivity for them.</returns>
	public static IDisposable EffectRoot(Action callback)
	{
		var parent = Runtime.CurrentEffect;
		var root = new Effect([StackTraceHidden] [DebuggerStepThrough]() =>
			{
				callback();
				return null;
			},
			parent,
			false);

		root.SetDebugInfo(icon: "anchor", location: new CallLocation(1), parent: parent);
		root.Run();
		return root;
	}

	/// <summary>
	/// Creates a function that runs after a delay. If the current reactivity scope is disposed, the timer is cancelled
	/// and the function will not run.
	/// </summary>
	/// <param name="callback">The function to run.</param>
	/// <param name="milliseconds">How many milliseconds to wait before running the function.</param>
#pragma warning disable TUnit0031
	public static async void Timeout(Action callback, int milliseconds)
#pragma warning restore TUnit0031
	{
		try
		{
			var token = Runtime.EnsureCurrentEffect().CancelToken;
#if SANDBOX
			await GameTask.Delay(milliseconds, token);
#else
			await Task.Delay(milliseconds, token);
#endif
			token.ThrowIfCancellationRequested();

			callback();
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception e)
		{
#if SANDBOX
			Log.Error
#else
			await Console.Error.WriteLineAsync
#endif
				($"Exception occurred during timeout: {e}");
		}
	}

	/// <summary>
	/// Creates a function that runs after a delay. If the current reactivity scope is disposed, the timer is cancelled
	/// and the function will not run.
	/// </summary>
	/// <param name="callback">The function to run.</param>
	/// <param name="duration">How long to wait before running the function.</param>
	[StackTraceHidden]
	public static void Timeout(Action callback, TimeSpan duration)
	{
		Timeout(callback, (int)duration.TotalMilliseconds);
	}

	/// <summary>
	/// Creates a function that runs at a regular interval. If the current reactivity scope is disposed, the interval
	/// is cancelled and the function will no longer run.
	/// </summary>
	/// <param name="callback">The function to run.</param>
	/// <param name="milliseconds">How long to wait between runs.</param>
	/// <param name="immediate">
	/// Whether to run the function immediately instead of waiting for the first interval.
	/// </param>
#pragma warning disable TUnit0031
	public static async void Interval(Action callback, int milliseconds, bool immediate = false)
#pragma warning restore TUnit0031
	{
		try
		{
			var token = Runtime.EnsureCurrentEffect().CancelToken;

			if (immediate)
			{
				using (new UntrackScope())
				{
					callback();
				}
			}

			while (!token.IsCancellationRequested)
			{
#if SANDBOX
				await GameTask.Delay(milliseconds, token);
#else
				await Task.Delay(milliseconds, token);
#endif
				token.ThrowIfCancellationRequested();

				callback();
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception e)
		{
#if SANDBOX
			Log.Error
#else
			await Console.Error.WriteLineAsync
#endif
				($"Exception occurred during interval: {e}");
		}
	}

	/// <summary>
	/// Creates a function that runs at a regular interval. If the current reactivity scope is disposed, the interval
	/// is cancelled and the function will no longer run.
	/// </summary>
	/// <param name="callback">The function to run.</param>
	/// <param name="duration">How long to wait between runs.</param>
	/// <param name="immediate">
	/// Whether to run the function immediately instead of waiting for the first interval.
	/// </param>
	[StackTraceHidden]
	public static void Interval(Action callback, TimeSpan duration, bool immediate = false)
	{
		Interval(callback, (int)duration.TotalMilliseconds, immediate);
	}

	/// <summary>
	/// Creates a function that runs during every game tick (i.e. every fixed update). Game ticks run before frames. If
	/// the current reactivity scope is disposed, the tick function will no longer run.
	/// </summary>
	/// <param name="callback">The function to run.</param>
	/// <param name="immediate">
	/// Whether to run the function immediately instead of waiting for the next game tick.
	/// </param>
	/// <param name="stage">
	/// At what point during the game tick the function should run. Defaults to <see cref="TickStage.Start" />
	/// </param>
	public static void Tick(Action callback, bool immediate = false, TickStage stage = TickStage.Start)
	{
#if SANDBOX
		var parent = Runtime.EnsureCurrentEffect();
		var effect = new Effect(() =>
			{
				SandboxRuntimeExecutor.AddTickFunction(stage, callback);
				return () => SandboxRuntimeExecutor.RemoveTickFunction(stage, callback);
			},
			parent,
			false);

		effect.SetDebugInfo("Tick", "update", new CallLocation(1), parent);
		effect.Run();
#else
		Interval(callback, 33);
#endif

		if (immediate)
		{
			using (new UntrackScope())
			{
				callback();
			}
		}
	}

	/// <summary>
	/// Creates a function that runs during every frame (i.e. every update). Frames run after game ticks. If the current
	/// reactivity scope is disposed, the frame function will no longer run.
	/// </summary>
	/// <param name="callback">The function to run.</param>
	/// <param name="immediate">
	/// Whether to run the function immediately instead of waiting for the next frame.
	/// </param>
	/// <param name="stage">
	/// At what point during the frame the function should run. Defaults to <see cref="FrameStage.Start" />
	/// </param>
	public static void Frame(Action callback, bool immediate = false, FrameStage stage = FrameStage.Start)
	{
#if SANDBOX
		var parent = Runtime.EnsureCurrentEffect();
		var effect = new Effect(() =>
			{
				SandboxRuntimeExecutor.AddFrameFunction(stage, callback);
				return () => { SandboxRuntimeExecutor.RemoveFrameFunction(stage, callback); };
			},
			parent,
			false);

		effect.SetDebugInfo("Frame", "burst_mode", new CallLocation(1), parent);
		effect.Run();
#else
		Interval(callback, 33);
#endif

		if (immediate)
		{
			using (new UntrackScope())
			{
				callback();
			}
		}
	}

	/// <inheritdoc cref="Runtime.Flush" />
	public static void Flush()
	{
		Runtime.Flush();
	}

	/// <summary>
	/// Disables dependency tracking for the current scope.
	/// </summary>
	public static UntrackScope Untrack()
	{
		return new UntrackScope();
	}

	/// <summary>
	/// Runs a function without dependency tracking and returns the value.
	/// </summary>
	public static T Untrack<T>(Func<T> callback)
	{
		var previousIsUntracking = Runtime.IsUntracking;
		Runtime.IsUntracking = true;

		try
		{
			return callback();
		}
		finally
		{
			Runtime.IsUntracking = previousIsUntracking;
		}
	}

	/// <summary>
	/// Runs a function without dependency tracking.
	/// </summary>
	public static void Untrack(Action callback)
	{
		var previousIsUntracking = Runtime.IsUntracking;
		Runtime.IsUntracking = true;

		try
		{
			callback();
		}
		finally
		{
			Runtime.IsUntracking = previousIsUntracking;
		}
	}

	/// <summary>
	/// Whether the code is currently inside a reactivity tracking context.
	/// </summary>
	public static bool IsTracking()
	{
		return Runtime is { IsUntracking: false, CurrentEffect: { } };
	}

	/// <summary>
	/// Returns a cancellation token for the currently executing effect. If there's no current effect,
	/// <see cref="CancellationToken.None" /> will be returned instead.
	/// </summary>
	public static CancellationToken GetEffectCancelToken()
	{
		return Runtime.CurrentEffect is { CancelToken: var cancelToken } ? cancelToken : CancellationToken.None;
	}

	/// <summary>
	/// Sets the name of the currently executing effect for debug purposes.
	/// </summary>
	/// <param name="debugName">The name to assign to the effect.</param>
	/// <remarks>Calls to this method are removed in release builds.</remarks>
	[Conditional("DEBUG")]
	public static void SetEffectName(string debugName)
	{
#if DEBUG && SANDBOX
		Runtime.EnsureCurrentEffect().Name = debugName;
#endif
	}
}
