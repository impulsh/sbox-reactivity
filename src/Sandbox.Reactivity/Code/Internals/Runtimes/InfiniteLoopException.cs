namespace Sandbox.Reactivity.Internals.Runtimes;

internal class InfiniteLoopException(Dictionary<Effect, int>? effectExecutions = null) : Exception(Message)
{
	private new const string Message = "An infinite loop occurred while flushing effects";

#if DEBUG
	public readonly IReadOnlyDictionary<Effect, int>? EffectExecutions =
		effectExecutions != null ? new Dictionary<Effect, int>(effectExecutions) : null;
#endif
}
