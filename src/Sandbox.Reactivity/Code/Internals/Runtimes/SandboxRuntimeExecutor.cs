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
	public SandboxRuntimeExecutor(Scene scene)
		: base(scene)
	{
		Listen(Stage.FinishUpdate, 10, Tick, "Reactivity Flush");
	}

	private static void Tick()
	{
		if (Reactive.Runtime.IsFlushScheduled)
		{
			Reactive.Runtime.Flush();
		}
	}
}
#endif
