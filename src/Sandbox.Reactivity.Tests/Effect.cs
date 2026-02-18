using System.Threading.Tasks;
using FakeItEasy;
using static Sandbox.Reactivity.Reactive;

namespace Sandbox.Reactivity.Tests;

public class Effect
{
	[Test]
	public async Task RequiresEffectRoot()
	{
		await Assert.That(() => { Effect(() => { }); }).Throws<InvalidOperationException>();
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.EffectScopes))]
	public void CallsImmediatelyWhenCreated(CreateEffectDelegate createEffect)
	{
		A.FakeEffect(out var effect);
		EffectRoot(() => { createEffect(effect); });

		A.CallTo(effect).MustHaveHappenedOnceExactly();
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.EffectScopes))]
	public void DisposesWithRoot(CreateEffectDelegate createEffect)
	{
		A.FakeEffect(out var effect, out var teardown);
		var root = EffectRoot(() => { createEffect(effect); });

		A.CallTo(effect).MustHaveHappenedOnceExactly();
		A.CallTo(teardown).MustNotHaveHappened();

		root.Dispose();
		A.CallTo(effect).MustHaveHappenedOnceExactly();
		A.CallTo(teardown).MustHaveHappenedOnceExactly();
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.EffectScopes))]
	public void Run_OccursWhenStateChanges(CreateEffectDelegate createEffect)
	{
		var state = State(1);

		A.FakeEffect(() => { _ = state.Value; }, out var effect);
		EffectRoot(() => { createEffect(effect); });

		A.CallTo(effect).MustHaveHappenedOnceExactly();

		state.Value = 2;
		Flush();

		A.CallTo(effect).MustHaveHappenedTwiceExactly();
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.EffectScopes))]
	public void Run_OccursWhenDerivedChanges(CreateEffectDelegate createEffect)
	{
		var state = State(1);
		var doubled = Derived(() => state.Value * 2);

		A.FakeEffect(() => { _ = doubled.Value; }, out var effect);
		EffectRoot(() => { createEffect(effect); });

		A.CallTo(effect).MustHaveHappenedOnceExactly();

		state.Value = 2;
		Flush();

		A.CallTo(effect).MustHaveHappenedTwiceExactly();
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.EffectScopes))]
	public void Run_BatchesDependencyChanges(CreateEffectDelegate createEffect)
	{
		var state1 = State(1);
		var state2 = State("hello");
		var doubled = Derived(() => state1.Value * 2);

		A.FakeEffect(() =>
			{
				_ = state2.Value;
				_ = doubled.Value;
			},
			out var effect);
		EffectRoot(() => { createEffect(effect); });

		A.CallTo(effect).MustHaveHappenedOnceExactly();

		state1.Value = 2;
		state2.Value = "world";
		Flush();

		A.CallTo(effect).MustHaveHappenedTwiceExactly();
	}

	[Test]
	public void Run_CausesNestedEffectsToRerun()
	{
		var parentState = State(1);
		var childState = State("hello");

		A.FakeEffect(() =>
			{
				_ = parentState.Value;
				return () => { };
			},
			out var parentEffect,
			out var parentTeardown);

		A.FakeEffect(() =>
			{
				_ = childState.Value;
				return () => { };
			},
			out var childEffect,
			out var childTeardown);

		EffectRoot(() =>
		{
			Effect(() =>
			{
				var teardown = parentEffect();
				Effect(childEffect);

				return teardown;
			});
		});

		A.CallTo(parentEffect).MustHaveHappenedOnceExactly();
		A.CallTo(parentTeardown).MustNotHaveHappened();

		A.CallTo(childEffect).MustHaveHappenedOnceExactly();
		A.CallTo(childTeardown).MustNotHaveHappened();

		parentState.Value = 2;
		Flush();

		A.CallTo(parentEffect).MustHaveHappenedTwiceExactly();
		A.CallTo(parentTeardown).MustHaveHappenedOnceExactly();

		A.CallTo(childEffect).MustHaveHappenedTwiceExactly();
		A.CallTo(childTeardown).MustHaveHappenedOnceExactly();
	}

	[Test]
	public void NestedEffect_DependencyDoesNotRerunParent()
	{
		var parentState = State(1);
		var childState = State("hello");

		A.FakeEffect(() =>
			{
				_ = parentState.Value;
				return () => { };
			},
			out var parentEffect,
			out var parentTeardown);

		A.FakeEffect(() =>
			{
				_ = childState.Value;
				return () => { };
			},
			out var childEffect,
			out var childTeardown);

		EffectRoot(() =>
		{
			Effect(() =>
			{
				var teardown = parentEffect();
				Effect(childEffect);

				return teardown;
			});
		});

		A.CallTo(parentEffect).MustHaveHappenedOnceExactly();
		A.CallTo(parentTeardown).MustNotHaveHappened();

		A.CallTo(childEffect).MustHaveHappenedOnceExactly();
		A.CallTo(childTeardown).MustNotHaveHappened();

		childState.Value = "world";
		Flush();

		A.CallTo(parentEffect).MustHaveHappenedOnceExactly();
		A.CallTo(parentTeardown).MustNotHaveHappened();

		A.CallTo(childEffect).MustHaveHappenedTwiceExactly();
		A.CallTo(childTeardown).MustHaveHappenedOnceExactly();
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.EffectScopes))]
	public void Teardown_CalledWhenRootIsDisposed(CreateEffectDelegate createEffect)
	{
		A.FakeEffect(out var effect, out var teardown);
		var root = EffectRoot(() => { Effect(effect); });

		A.CallTo(effect).MustHaveHappenedOnceExactly();
		A.CallTo(teardown).MustNotHaveHappened();

		root.Dispose();
		A.CallTo(effect).MustHaveHappenedOnceExactly();
		A.CallTo(teardown).MustHaveHappenedOnceExactly();
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.EffectScopes))]
	public void Teardown_RunsWhenDependencyChanges(CreateEffectDelegate createEffect)
	{
		var state1 = State(1);
		var state2 = State("hello");
		var doubled = Derived(() => state1.Value * 2);

		A.FakeEffect(() =>
			{
				_ = state2.Value;
				_ = doubled.Value;

				return () => { };
			},
			out var effect,
			out var teardown);

		EffectRoot(() => { createEffect(effect); });

		A.CallTo(teardown).MustNotHaveHappened();

		state1.Value = 2;
		state2.Value = "world";
		Flush();

		A.CallTo(teardown).MustHaveHappenedOnceExactly();
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.EffectScopes))]
	public async Task Teardown_State_HasPreviouslyReadValue(CreateEffectDelegate createEffect)
	{
		var state = State(1);
		int? valueDuringTeardown = null;

		A.FakeEffect(() =>
			{
				_ = state.Value;

				return () => valueDuringTeardown = state.Value;
			},
			out var effect,
			out _);

		EffectRoot(() => { createEffect(effect); });

		await Assert.That(valueDuringTeardown).IsNull();

		state.Value = 2;
		Flush();

		await Assert.That(valueDuringTeardown).IsEqualTo(1);

		state.Value = 3;
		Flush();

		await Assert.That(valueDuringTeardown).IsEqualTo(2);
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.EffectScopes))]
	public async Task Teardown_Derived_HasPreviouslyReadValue(CreateEffectDelegate createEffect)
	{
		var state = State(1);
		var doubled = Derived(() => state.Value * 2);

		int? valueDuringTeardown = null;

		A.FakeEffect(() =>
			{
				_ = doubled.Value;

				return () => valueDuringTeardown = doubled.Value;
			},
			out var effect,
			out _);

		EffectRoot(() => { createEffect(effect); });

		await Assert.That(valueDuringTeardown).IsNull();

		state.Value = 2;
		Flush();

		await Assert.That(valueDuringTeardown).IsEqualTo(2);

		state.Value = 3;
		Flush();

		await Assert.That(valueDuringTeardown).IsEqualTo(4);
	}

	// state ---- doubled ----> effect
	//        \__ tripled __/
	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.EffectScopes))]
	public async Task Teardown_DiamondDerived_HasPreviouslyReadValue(CreateEffectDelegate createEffect)
	{
		var state = State(1);
		var doubled = Derived(() => state.Value * 2);
		var tripled = Derived(() => state.Value * 3);

		int? doubledDuringTeardown = null;
		int? tripledDuringTeardown = null;

		A.FakeEffect(() =>
			{
				_ = doubled.Value;
				_ = tripled.Value;

				return () =>
				{
					doubledDuringTeardown = doubled.Value;
					tripledDuringTeardown = tripled.Value;
				};
			},
			out var effect,
			out _);

		EffectRoot(() => { createEffect(effect); });

		await Assert.That(doubledDuringTeardown).IsNull();
		await Assert.That(tripledDuringTeardown).IsNull();

		state.Value = 2;
		Flush();

		await Assert.That(doubledDuringTeardown).IsEqualTo(2);
		await Assert.That(tripledDuringTeardown).IsEqualTo(3);

		state.Value = 3;
		Flush();

		await Assert.That(doubledDuringTeardown).IsEqualTo(4);
		await Assert.That(tripledDuringTeardown).IsEqualTo(6);
	}

	[Test]
	[MethodDataSource(typeof(DataSources), nameof(DataSources.EffectScopes))]
	public async Task Teardown_HasCurrentValueForUntrackedDependency(CreateEffectDelegate createEffect)
	{
		var state1 = State(1);
		var state2 = State("hello");
		string? valueDuringTeardown = null;

		A.FakeEffect(() =>
			{
				_ = state1.Value;

				return () => valueDuringTeardown = state2.Value;
			},
			out var effect,
			out _);

		EffectRoot(() => { createEffect(effect); });

		await Assert.That(valueDuringTeardown).IsNull();

		state1.Value = 2;
		Flush();

		await Assert.That(valueDuringTeardown).IsEqualTo("hello");

		state1.Value = 3;
		state2.Value = "world";
		Flush();

		await Assert.That(valueDuringTeardown).IsEqualTo("world");
	}
}
