using FakeItEasy;
using static Sandbox.Reactivity.Reactive;

namespace Sandbox.Reactivity.Tests;

public class Derived
{
	[Test]
	public async Task CorrectlyAssignsValue()
	{
		var state = State(1);
		var doubled = Derived(() => state.Value * 2);

		await Assert.That(doubled.Value).IsEqualTo(2);

		state.Value = 2;
		await Assert.That(doubled.Value).IsEqualTo(4);
	}

	[Test]
	public async Task ComputesLazily()
	{
		var state = State(1);
		var computeDoubled = A.FakeCompute(() => state.Value * 2);
		var doubled = Derived(computeDoubled);

		A.CallTo(computeDoubled).MustNotHaveHappened();

		state.Value = 2;
		A.CallTo(computeDoubled).MustNotHaveHappened();

		await Assert.That(doubled.Value).IsEqualTo(4);
		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task Chain_ComputesLazily()
	{
		var state = State(1);
		var computeDoubled = A.FakeCompute(() => state.Value * 2);
		var doubled = Derived(computeDoubled);
		var computeQuadrupled = A.FakeCompute(() => doubled.Value * 2);
		var quadrupled = Derived(computeQuadrupled);

		A.CallTo(computeDoubled).MustNotHaveHappened();
		A.CallTo(computeQuadrupled).MustNotHaveHappened();

		state.Value = 2;
		A.CallTo(computeDoubled).MustNotHaveHappened();
		A.CallTo(computeQuadrupled).MustNotHaveHappened();

		await Assert.That(doubled.Value).IsEqualTo(4);
		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();
		A.CallTo(computeQuadrupled).MustNotHaveHappened();

		await Assert.That(quadrupled.Value).IsEqualTo(8);
		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();
		A.CallTo(computeQuadrupled).MustHaveHappenedOnceExactly();
	}

	[Test]
	public void ComputingCausesEffectToRun()
	{
		var state = State(1);
		var computeDoubled = A.FakeCompute(() => state.Value * 2);
		var doubled = Derived(computeDoubled);

		A.FakeEffect(() => { _ = doubled.Value; }, out var effect);
		EffectRoot(() => { Effect(effect); });

		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();
		A.CallTo(effect).MustHaveHappenedOnceExactly();

		state.Value = 2;
		Flush();

		A.CallTo(computeDoubled).MustHaveHappenedTwiceExactly();
		A.CallTo(effect).MustHaveHappenedTwiceExactly();
	}

	[Test]
	public void Chain_ComputingCausesEffectToRun()
	{
		var state = State(1);
		var computeDoubled = A.FakeCompute(() => state.Value * 2);
		var doubled = Derived(computeDoubled);
		var computeQuadrupled = A.FakeCompute(() => doubled.Value * 2);
		var quadrupled = Derived(computeQuadrupled);

		A.FakeEffect(() => { _ = quadrupled.Value; }, out var effect);
		EffectRoot(() => { Effect(effect); });

		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();
		A.CallTo(computeQuadrupled).MustHaveHappenedOnceExactly();
		A.CallTo(effect).MustHaveHappenedOnceExactly();

		state.Value = 2;
		Flush();

		A.CallTo(computeDoubled).MustHaveHappenedTwiceExactly();
		A.CallTo(computeQuadrupled).MustHaveHappenedTwiceExactly();
		A.CallTo(effect).MustHaveHappenedTwiceExactly();
	}

	[Test]
	public void DoesNotRunEffectWhenComputingToSameValue()
	{
		var state = State(2);
		var computeIsEven = A.FakeCompute(() => state.Value % 2 == 0);
		var isEven = Derived(computeIsEven);

		A.FakeEffect(() => { _ = isEven.Value; }, out var effect);
		EffectRoot(() => { Effect(effect); });

		A.CallTo(computeIsEven).MustHaveHappenedOnceExactly();
		A.CallTo(effect).MustHaveHappenedOnceExactly();

		state.Value = 4;
		Flush();

		A.CallTo(computeIsEven).MustHaveHappenedTwiceExactly();
		A.CallTo(effect).MustHaveHappenedOnceExactly();

		state.Value = 5;
		Flush();

		A.CallTo(computeIsEven).MustHaveHappened(3, Times.Exactly);
		A.CallTo(effect).MustHaveHappenedTwiceExactly();
	}

	[Test]
	public void DoesNotRecomputeAfterDisconnectingFromEffect()
	{
		var shouldReadState = State(true);
		var state = State(1);
		var computeDoubled = A.FakeCompute(() => state.Value * 2);
		var doubled = Derived(computeDoubled);

		EffectRoot(() =>
		{
			Effect(() =>
			{
				if (!shouldReadState.Value)
				{
					return;
				}

				_ = doubled.Value;
			});
		});

		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();

		shouldReadState.Value = false;
		Flush();
		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();

		state.Value = 2;
		Flush();
		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();
	}

	[Test]
	public void RecomputesAfterConnectingToEffect()
	{
		var shouldReadState = State(true);
		var state = State(1);
		var computeDoubled = A.FakeCompute(() => state.Value * 2);
		var doubled = Derived(computeDoubled);

		EffectRoot(() =>
		{
			Effect(() =>
			{
				if (!shouldReadState.Value)
				{
					return;
				}

				_ = doubled.Value;
			});
		});

		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();

		shouldReadState.Value = false;
		Flush();
		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();

		state.Value = 2;
		Flush();
		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();

		shouldReadState.Value = true;
		Flush();
		A.CallTo(computeDoubled).MustHaveHappenedTwiceExactly();
	}

	[Test]
	public async Task DisconnectedDoesNotRecomputeForConnectedDerivedDependency()
	{
		var state = State(1);
		var computeDoubled = A.FakeCompute(() => state.Value * 2);
		var doubled = Derived(computeDoubled);
		var computeQuadrupled = A.FakeCompute(() => doubled.Value * 2);
		var quadrupled = Derived(computeQuadrupled);

		A.FakeEffect(() => { _ = doubled.Value; }, out var effect);
		EffectRoot(() => { Effect(effect); });

		A.CallTo(computeQuadrupled).MustNotHaveHappened();

		state.Value = 2;
		Flush();

		A.CallTo(computeQuadrupled).MustNotHaveHappened();

		await Assert.That(quadrupled.Value).IsEqualTo(8);
	}

	[Test]
	public void Chain_DoesNotRunEffectWhenComputingToSameValue()
	{
		var state = State(2);
		var computeDoubled = A.FakeCompute(() => state.Value * 2);
		var doubled = Derived(computeDoubled);
		var computeIsDoubleOverTen = A.FakeCompute(() => doubled.Value > 10);
		var isDoubleOverTen = Derived(computeIsDoubleOverTen);

		A.FakeEffect(() => { _ = isDoubleOverTen.Value; }, out var effect);
		EffectRoot(() => { Effect(effect); });

		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();
		A.CallTo(computeIsDoubleOverTen).MustHaveHappenedOnceExactly();
		A.CallTo(effect).MustHaveHappenedOnceExactly();

		state.Value = 4;
		Flush();

		A.CallTo(computeDoubled).MustHaveHappenedTwiceExactly();
		A.CallTo(computeIsDoubleOverTen).MustHaveHappenedTwiceExactly();
		A.CallTo(effect).MustHaveHappenedOnceExactly();

		state.Value = 8;
		Flush();

		A.CallTo(computeDoubled).MustHaveHappened(3, Times.Exactly);
		A.CallTo(computeIsDoubleOverTen).MustHaveHappened(3, Times.Exactly);
		A.CallTo(effect).MustHaveHappenedTwiceExactly();
	}

	[Test]
	public async Task OverriddenValue_DoesNotRecomputeWhenAccessed()
	{
		var state = State(1);
		var computeDoubled = A.FakeCompute(() => state.Value * 2);
		var doubled = Derived(computeDoubled);

		doubled.Value = 1000;

		// derived will recompute if assigned while stale since it needs to track the dependencies for its reaction
		// in order to clear the overridden value if they change
		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();
		await Assert.That(doubled.Value).IsEqualTo(1000);

		// but it shouldn't call again if it's already tracked the dependencies
		await Assert.That(doubled.Value).IsEqualTo(1000);
		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task OverriddenValue_IsClearedWhenDependencyChanges()
	{
		var state = State(1);
		var computeDoubled = A.FakeCompute(() => state.Value * 2);
		var doubled = Derived(computeDoubled);

		await Assert.That(doubled.Value).IsEqualTo(2);
		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();

		doubled.Value = 1000;

		await Assert.That(doubled.Value).IsEqualTo(1000);
		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();

		state.Value = 2;

		await Assert.That(doubled.Value).IsEqualTo(4);
		A.CallTo(computeDoubled).MustHaveHappenedTwiceExactly();
	}

	[Test]
	public async Task OverriddenValue_BeforeComputing_IsClearedWhenDependencyChanges()
	{
		var state = State(1);
		var computeDoubled = A.FakeCompute(() => state.Value * 2);
		var doubled = Derived(computeDoubled);

		doubled.Value = 1000;

		await Assert.That(doubled.Value).IsEqualTo(1000);
		A.CallTo(computeDoubled).MustHaveHappenedOnceExactly();

		state.Value = 2;

		await Assert.That(doubled.Value).IsEqualTo(4);
		A.CallTo(computeDoubled).MustHaveHappenedTwiceExactly();
	}
}
