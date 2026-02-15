using System.Diagnostics;
using Sandbox.Reactivity.Internals;
using Sandbox.Reactivity.Internals.Runtimes;
#if !SANDBOX
using System.Threading;
using System.Threading.Tasks;
#endif
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
public static class Reactive
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
	/// <typeparam name="T">The type of value this state holds.</typeparam>
	/// <returns>The created state.</returns>
	public static State<T> State<T>(T initialValue)
	{
		return new State<T>(initialValue);
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
	/// <typeparam name="T">The type of value this derived state holds.</typeparam>
	/// <returns>The created derived state.</returns>
	public static Derived<T> Derived<T>(Func<T> compute)
	{
		return new Derived<T>(compute);
	}

	/// <inheritdoc cref="Effect(Action)" />
	/// <remarks>
	/// Effects may optionally return a teardown function that will run when a reactive value it depends on is about
	/// to change its value, or when the effect is being disposed.
	/// </remarks>
	public static void Effect(Func<Action?> callback)
	{
		if (Runtime.CurrentEffect is not { } parent)
		{
			throw new InvalidOperationException("Effect must be created inside an effect root");
		}

		var effect = new Effect(callback, parent, true);
		effect.Run();
	}

	/// <summary>
	/// Creates a function that will re-run whenever a reactive value that was read during its execution has changed.
	/// </summary>
	/// <param name="callback">The function to run.</param>
	/// <seealso cref="Effect(Func{Action})" />
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
			if (Runtime.CurrentEffect is not { } parent)
			{
				throw new InvalidOperationException("Timeout must be created inside an effect root");
			}

			var token = parent.CancelToken;
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
			if (Runtime.CurrentEffect is not { } parent)
			{
				throw new InvalidOperationException("Timeout must be created inside an effect root");
			}

			var token = parent.CancelToken;

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
				($"Exception occurred during timeout: {e}");
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
	public static void Interval(Action callback, TimeSpan duration, bool immediate = false)
	{
		Interval(callback, (int)duration.TotalMilliseconds, immediate);
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
}
