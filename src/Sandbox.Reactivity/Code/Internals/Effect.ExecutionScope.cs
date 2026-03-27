namespace Sandbox.Reactivity.Internals;

internal partial class Effect
{
	/// <summary>
	/// Sets the given effect as the runtime's currently executing effect, and restores it when disposed. This does not
	/// actually run the effect function itself.
	/// </summary>
	internal readonly ref struct ExecutionScope : IDisposable
	{
		private readonly Effect _effect;

		private readonly Effect? _previousEffect = Reactive.Runtime.CurrentEffect;

		private readonly IReaction? _previousReaction = Reactive.Runtime.CurrentReaction;

		private readonly bool _previousIsUntracking = Reactive.Runtime.IsUntracking;

		internal ExecutionScope(Effect effect)
		{
			_effect = effect;

			Reactive.Runtime.CurrentEffect = effect;
			Reactive.Runtime.CurrentReaction = effect.ShouldTrackDependencies ? effect : null;
			Reactive.Runtime.IsUntracking = !effect.ShouldTrackDependencies;

			// mark as up to date before running so that any dependency changes that occur during execution will
			// properly schedule this effect to run at the end of the current flush operation
			effect.State = ReactionState.UpToDate;
#if DEBUG && SANDBOX
			if (!effect._hasEverRun)
			{
				effect.Name ??= "Effect";

				switch (effect.Parent)
				{
					case Effect parentEffect:
						parentEffect.OnChildEffectCreated?.Invoke(effect);
						break;
					default:
						OnEffectRootCreated?.Invoke(effect);
						break;
				}
			}
#endif
		}

		public void Dispose()
		{
			Reactive.Runtime.CurrentEffect = _previousEffect;
			Reactive.Runtime.CurrentReaction = _previousReaction;
			Reactive.Runtime.IsUntracking = _previousIsUntracking;

			_effect.ReadVersion = Reactive.Runtime.Version;
#if DEBUG && SANDBOX
			if (_effect._hasEverRun)
			{
				_effect.OnRerun?.Invoke();
			}
			else
			{
				_effect._hasEverRun = true;
			}
#endif
		}
	}
}
