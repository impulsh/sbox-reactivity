#if SANDBOX
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity.Internals.Runtimes;

#if JETBRAINS_ANNOTATIONS
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
#endif
internal sealed class SandboxRuntimeExecutor : GameObjectSystem<SandboxRuntimeExecutor>
{
	/// <see cref="TickStage.Physics" />
	private static readonly List<Action> PhysicsTickFunctions = new(16);

	/// <see cref="TickStage.Start" />
	private static readonly List<Action> StartTickFunctions = new(16);

	/// <see cref="TickStage.End" />
	private static readonly List<Action> EndTickFunctions = new(16);

	/// <see cref="FrameStage.Interpolation" />
	private static readonly List<Action> InterpolationFrameFunctions = new(1);

	/// <see cref="FrameStage.Start" />
	private static readonly List<Action> StartFrameFunctions = new(16);

	/// <see cref="FrameStage.UpdateBones" />
	private static readonly List<Action> UpdateBonesFrameFunctions = new(16);

	/// <see cref="FrameStage.End" />
	private static readonly List<Action> EndFrameFunctions = new(16);

	public SandboxRuntimeExecutor(Scene scene)
		: base(scene)
	{
		Listen(Stage.StartUpdate, 10, StartUpdate, "Reactivity.StartUpdate");
		Listen(Stage.UpdateBones, 10, UpdateBones, "Reactivity.UpdateBones");
		Listen(Stage.PhysicsStep, 10, PhysicsStep, "Reactivity.PhysicsStep");
		Listen(Stage.Interpolation, 10, Interpolate, "Reactivity.Interpolation");
		Listen(Stage.FinishUpdate, 10, FinishUpdate, "Reactivity.FinishUpdate");
		Listen(Stage.StartFixedUpdate, 10, StartFixedUpdate, "Reactivity.StartFixedUpdate");
		Listen(Stage.FinishFixedUpdate, 10, FinishFixedUpdate, "Reactivity.FinishFixedUpdate");
	}

	private static List<Action> GetCallbacks(TickStage tickStage)
	{
		return tickStage switch
		{
			TickStage.Physics => PhysicsTickFunctions,
			TickStage.Start => StartTickFunctions,
			TickStage.End => EndTickFunctions,
			_ => throw new ArgumentOutOfRangeException(),
		};
	}

	private static List<Action> GetCallbacks(FrameStage frameStage)
	{
		return frameStage switch
		{
			FrameStage.Interpolation => InterpolationFrameFunctions,
			FrameStage.Start => StartFrameFunctions,
			FrameStage.UpdateBones => UpdateBonesFrameFunctions,
			FrameStage.End => EndFrameFunctions,
			_ => throw new ArgumentOutOfRangeException(),
		};
	}

	public static void AddTickFunction(TickStage stage, Action callback)
	{
		var callbacks = GetCallbacks(stage);

		if (!callbacks.Contains(callback))
		{
			callbacks.Add(callback);
		}
	}

	public static void RemoveTickFunction(TickStage stage, Action callback)
	{
		var callbacks = GetCallbacks(stage);
		callbacks.Remove(callback);
	}

	public static void AddFrameFunction(FrameStage stage, Action callback)
	{
		var callbacks = GetCallbacks(stage);

		if (!callbacks.Contains(callback))
		{
			callbacks.Add(callback);
		}
	}

	public static void RemoveFrameFunction(FrameStage stage, Action callback)
	{
		var callbacks = GetCallbacks(stage);
		callbacks.Remove(callback);
	}

	private static void StartUpdate()
	{
		if (StartFrameFunctions.Count == 0)
		{
			return;
		}

		foreach (var callback in StartFrameFunctions)
		{
			callback();
		}
	}

	private static void UpdateBones()
	{
		if (UpdateBonesFrameFunctions.Count == 0)
		{
			return;
		}

		foreach (var callback in UpdateBonesFrameFunctions)
		{
			callback();
		}
	}

	private static void PhysicsStep()
	{
		if (PhysicsTickFunctions.Count == 0)
		{
			return;
		}

		foreach (var callback in PhysicsTickFunctions)
		{
			callback();
		}
	}

	private static void Interpolate()
	{
		if (InterpolationFrameFunctions.Count == 0)
		{
			return;
		}

		foreach (var callback in InterpolationFrameFunctions)
		{
			callback();
		}
	}

	private static void FinishUpdate()
	{
		if (EndFrameFunctions.Count > 0)
		{
			foreach (var callback in EndFrameFunctions)
			{
				callback();
			}
		}

		if (Reactive.Runtime.IsFlushScheduled)
		{
			Reactive.Runtime.Flush();
		}
	}

	private static void StartFixedUpdate()
	{
		if (StartTickFunctions.Count == 0)
		{
			return;
		}

		foreach (var callback in StartTickFunctions)
		{
			callback();
		}
	}

	private static void FinishFixedUpdate()
	{
		if (EndTickFunctions.Count == 0)
		{
			return;
		}

		foreach (var callback in EndTickFunctions)
		{
			callback();
		}
	}
}
#endif
