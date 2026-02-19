using FakeItEasy;
using static Sandbox.Reactivity.Reactive;

namespace Sandbox.Reactivity.Tests;

public class Untrack
{
	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.UntrackScopes))]
	public void PreventsDependency_State(UntrackScopeDelegate untrack)
	{
		var state1 = State(1);
		var state2 = State(1);

		A.FakeEffect(() =>
			{
				untrack(() => { _ = state1.Value; });
				_ = state2.Value;
			},
			out var effect);

		EffectRoot(() => { Effect(effect); });

		A.CallTo(effect).MustHaveHappenedOnceExactly();

		state1.Value = 2;
		Flush();

		A.CallTo(effect).MustHaveHappenedOnceExactly();

		state2.Value = 2;
		Flush();

		A.CallTo(effect).MustHaveHappenedTwiceExactly();
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.UntrackScopes))]
	public void PreventsDependency_Derived(UntrackScopeDelegate untrack)
	{
		var state1 = State(1);
		var state2 = State(1);
		var doubled = Derived(() => state1.Value * 2);

		A.FakeEffect(() =>
			{
				untrack(() => _ = doubled.Value);
				_ = state2.Value;
			},
			out var effect);

		EffectRoot(() => { Effect(effect); });

		A.CallTo(effect).MustHaveHappenedOnceExactly();

		state1.Value = 2;
		Flush();

		A.CallTo(effect).MustHaveHappenedOnceExactly();

		state2.Value = 2;
		Flush();

		A.CallTo(effect).MustHaveHappenedTwiceExactly();
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.UntrackScopes))]
	public void TriggersReactivityForAssignment_State(UntrackScopeDelegate untrack)
	{
		var state = State(1);

		A.FakeEffect(() => { _ = state.Value; }, out var effect);
		EffectRoot(() => { Effect(effect); });

		A.CallTo(effect).MustHaveHappenedOnceExactly();

		untrack(() =>
		{
			state.Value = 2;
			Flush();
		});

		A.CallTo(effect).MustHaveHappenedTwiceExactly();
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.UntrackScopes))]
	public void TriggersReactivityForAssignment_Derived(UntrackScopeDelegate untrack)
	{
		var state = State(1);
		var doubled = Derived(() => state.Value * 2);

		A.FakeEffect(() => { _ = doubled.Value; }, out var effect);
		EffectRoot(() => { Effect(effect); });

		A.CallTo(effect).MustHaveHappenedOnceExactly();

		untrack(() =>
		{
			state.Value = 2;
			Flush();
		});

		A.CallTo(effect).MustHaveHappenedTwiceExactly();
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.UntrackScopes))]
	public void RestoresNestedReactivityScope_Effect(UntrackScopeDelegate untrack)
	{
		var parentState = State(1);
		var childState = State(1);

		A.FakeEffect(() => { _ = childState.Value; }, out var childEffect);

		A.FakeEffect(() =>
			{
				untrack(() =>
				{
					_ = childState.Value;
					Effect(childEffect);
				});

				_ = parentState.Value;
			},
			out var parentEffect);

		EffectRoot(() => { Effect(parentEffect); });

		A.CallTo(parentEffect).MustHaveHappenedOnceExactly();
		A.CallTo(childEffect).MustHaveHappenedOnceExactly();

		childState.Value = 2;
		Flush();

		A.CallTo(parentEffect).MustHaveHappenedOnceExactly();
		A.CallTo(childEffect).MustHaveHappenedTwiceExactly();
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.UntrackScopes))]
	public void RestoresNestedReactivityScope_DerivedComputation(UntrackScopeDelegate untrack)
	{
		var state = State(1);
		var computeDoubled = A.FakeCompute(() => state.Value * 2);
		var doubled = Derived(computeDoubled);

		A.CallTo(computeDoubled).MustNotHaveHappened();

		A.FakeEffect(() =>
			{
				untrack(() => { _ = doubled.Value; });

				_ = state.Value;
			},
			out var effect);

		EffectRoot(() => { Effect(effect); });

		A.CallTo(effect).MustHaveHappenedOnceExactly();
		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();

		state.Value = 2;
		Flush();

		A.CallTo(effect).MustHaveHappenedTwiceExactly();
		A.CallTo(computeDoubled).MustHaveHappenedTwiceExactly();
	}
}
