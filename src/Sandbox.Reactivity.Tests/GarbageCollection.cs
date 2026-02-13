using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static Sandbox.Reactivity.Reactive;

namespace Sandbox.Reactivity.Tests;

file static class WeakReferenceExtensions
{
	extension<T>(WeakReference<T> targetRef)
		where T : class
	{
		public T Target
		{
			get
			{
				if (!targetRef.TryGetTarget(out var target))
				{
					Assert.Fail("Object has been garbage collected");
				}

				return target!;
			}
		}
	}
}

[NotInParallel]
public class GarbageCollection
{
	private static void CollectGarbage()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference<T> AsWeakReference<T>(Func<T> callback)
		where T : class
	{
		return new WeakReference<T>(callback());
	}

	[StackTraceHidden]
	private static async Task AssertHasBeenGarbageCollected<T>(
		WeakReference<T> targetRef,
		[CallerArgumentExpression(nameof(targetRef))] string? expression = null
	)
		where T : class
	{
		await Assert.That(targetRef.TryGetTarget(out _))
			.IsFalse()
			.Because($"{expression ?? ""} object should be garbage collected");
	}

	[StackTraceHidden]
	private static async Task AssertHasNotBeenGarbageCollected<T>(
		WeakReference<T> targetRef,
		[CallerArgumentExpression(nameof(targetRef))] string? expression = null
	)
		where T : class
	{
		await Assert.That(targetRef.TryGetTarget(out _))
			.IsTrue()
			.Because($"{expression ?? ""} object should not be garbage collected");
	}

	[Test]
	public async Task Standalone_State()
	{
		var stateRef = AsWeakReference(() => State(1));

		await Assert.That(() => stateRef.Target.Value).IsEqualTo(1);

		CollectGarbage();
		await AssertHasBeenGarbageCollected(stateRef);
	}

	[Test]
	public async Task Standalone_Derived()
	{
		var stateRef = AsWeakReference(() => State(1));
		var derivedRef = AsWeakReference(() => Derived(() => stateRef.Target.Value * 2));

		await Assert.That(() => derivedRef.Target.Value).IsEqualTo(2);

		CollectGarbage();
		await AssertHasBeenGarbageCollected(stateRef);
		await AssertHasBeenGarbageCollected(derivedRef);
	}

	[Test]
	public async Task Standalone_Derived_Chain()
	{
		var stateRef = AsWeakReference(() => State(1));
		var derivedRef1 = AsWeakReference(() => Derived(() => stateRef.Target.Value * 2));
		var derivedRef2 = AsWeakReference(() => Derived(() => derivedRef1.Target.Value * 2));
		var derivedRef3 = AsWeakReference(() => Derived(() => derivedRef2.Target.Value * 2));

		await Assert.That(() => derivedRef3.Target.Value).IsEqualTo(8);

		CollectGarbage();
		await AssertHasBeenGarbageCollected(stateRef);
		await AssertHasBeenGarbageCollected(derivedRef1);
		await AssertHasBeenGarbageCollected(derivedRef2);
		await AssertHasBeenGarbageCollected(derivedRef3);
	}

	[Test]
	public async Task AfterRootDispose_State()
	{
		var stateRef = AsWeakReference(() => State(1));
		var root = EffectRoot(() => { Effect(() => { _ = stateRef.Target.Value; }); });

		CollectGarbage();
		await AssertHasNotBeenGarbageCollected(stateRef);

		root.Dispose();

		CollectGarbage();
		await AssertHasBeenGarbageCollected(stateRef);
	}

	[Test]
	public async Task AfterRootDispose_Derived()
	{
		var stateRef = AsWeakReference(() => State(1));
		var derivedRef = AsWeakReference(() => Derived(() => stateRef.Target.Value * 2));
		var root = EffectRoot(() => { Effect(() => { _ = derivedRef.Target.Value; }); });

		CollectGarbage();
		await AssertHasNotBeenGarbageCollected(stateRef);
		await AssertHasNotBeenGarbageCollected(derivedRef);

		root.Dispose();

		CollectGarbage();
		await AssertHasBeenGarbageCollected(stateRef);
		await AssertHasBeenGarbageCollected(derivedRef);
	}

	[Test]
	public async Task AfterRootDispose_Derived_Chain()
	{
		var stateRef = AsWeakReference(() => State(1));
		var derivedRef1 = AsWeakReference(() => Derived(() => stateRef.Target.Value * 2));
		var derivedRef2 = AsWeakReference(() => Derived(() => derivedRef1.Target.Value * 2));
		var root = EffectRoot(() => { Effect(() => { _ = derivedRef2.Target.Value; }); });

		CollectGarbage();
		await AssertHasNotBeenGarbageCollected(stateRef);
		await AssertHasNotBeenGarbageCollected(derivedRef1);
		await AssertHasNotBeenGarbageCollected(derivedRef2);

		root.Dispose();

		CollectGarbage();
		await AssertHasBeenGarbageCollected(stateRef);
		await AssertHasBeenGarbageCollected(derivedRef1);
		await AssertHasBeenGarbageCollected(derivedRef2);
	}

	[Test]
	public async Task AfterDisconnection_State()
	{
		var shouldReadState = State(true);
		var stateRef = AsWeakReference(() => State(1));

		EffectRoot(() =>
		{
			Effect(() =>
			{
				if (!shouldReadState.Value)
				{
					return;
				}

				_ = stateRef.Target.Value;
			});
		});

		CollectGarbage();
		await AssertHasNotBeenGarbageCollected(stateRef);

		shouldReadState.Value = false;
		Flush();

		CollectGarbage();
		await AssertHasBeenGarbageCollected(stateRef);
	}

	[Test]
	public async Task AfterDisconnection_Derived()
	{
		var shouldReadState = State(true);
		var stateRef = AsWeakReference(() => State(1));
		var derivedRef = AsWeakReference(() => Derived(() => stateRef.Target.Value * 2));

		EffectRoot(() =>
		{
			Effect(() =>
			{
				if (!shouldReadState.Value)
				{
					return;
				}

				_ = derivedRef.Target.Value;
			});
		});

		CollectGarbage();
		await AssertHasNotBeenGarbageCollected(stateRef);
		await AssertHasNotBeenGarbageCollected(derivedRef);

		shouldReadState.Value = false;
		Flush();

		CollectGarbage();
		await AssertHasBeenGarbageCollected(stateRef);
		await AssertHasBeenGarbageCollected(derivedRef);
	}

	[Test]
	public async Task AfterDisconnection_Derived_Chain()
	{
		var shouldReadState = State(true);
		var stateRef = AsWeakReference(() => State(1));
		var derivedRef1 = AsWeakReference(() => Derived(() => stateRef.Target.Value * 2));
		var derivedRef2 = AsWeakReference(() => Derived(() => derivedRef1.Target.Value * 2));

		EffectRoot(() =>
		{
			Effect(() =>
			{
				if (!shouldReadState.Value)
				{
					return;
				}

				_ = derivedRef2.Target.Value;
			});
		});

		CollectGarbage();
		await AssertHasNotBeenGarbageCollected(stateRef);
		await AssertHasNotBeenGarbageCollected(derivedRef1);
		await AssertHasNotBeenGarbageCollected(derivedRef2);

		shouldReadState.Value = false;
		Flush();

		CollectGarbage();
		await AssertHasBeenGarbageCollected(stateRef);
		await AssertHasBeenGarbageCollected(derivedRef1);
		await AssertHasBeenGarbageCollected(derivedRef2);
	}
}
