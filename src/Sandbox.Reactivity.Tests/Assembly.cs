// ReSharper disable CheckNamespace

using FakeItEasy;
using static Sandbox.Reactivity.Reactive;

#pragma warning disable CA1050
public delegate void CreateEffectDelegate(Func<Action?> callback);

public delegate void UntrackScopeDelegate(Action callback);
#pragma warning restore CA1050

internal static class DataSources
{
	private static readonly CreateEffectDelegate NestedEffectDelegate = callback =>
	{
		Effect(() => { Effect(callback); });
	};

	private static readonly UntrackScopeDelegate UntrackDisposableScopeDelegate = callback =>
	{
		using (Untrack())
		{
			callback();
		}
	};

	// false positive since we return a delegate instead of a Func<T>
#pragma warning disable TUnit0046
	public static IEnumerable<TestDataRow<CreateEffectDelegate>> EffectScopes()
	{
		yield return new TestDataRow<CreateEffectDelegate>(Effect, "In Root");
		yield return new TestDataRow<CreateEffectDelegate>(NestedEffectDelegate, "In Nested Effect");
	}

	public static IEnumerable<TestDataRow<UntrackScopeDelegate>> UntrackScopes()
	{
		yield return new TestDataRow<UntrackScopeDelegate>(Untrack, "With Function");
		yield return new TestDataRow<UntrackScopeDelegate>(UntrackDisposableScopeDelegate, "With Disposable Scope");
	}
#pragma warning restore TUnit0046
}

internal static class FakeExtensions
{
	extension(A)
	{
		public static Func<T> FakeCompute<T>(Func<T> compute)
		{
			return A.Fake<Func<T>>(x => x.Wrapping(compute));
		}

		public static void FakeEffect(out Func<Action?> effect)
		{
			effect = A.Fake<Func<Action?>>();
		}

		public static void FakeEffect(out Func<Action> effect, out Action teardown)
		{
			var fakeTeardown = A.Fake<Action>();
			var wrappedEffect = () => fakeTeardown;
			var fakeEffect = A.Fake<Func<Action>>(x => x.Wrapping(wrappedEffect));

			effect = fakeEffect;
			teardown = fakeTeardown;
		}

		public static void FakeEffect(Action callback, out Func<Action?> effect)
		{
			effect = A.Fake<Func<Action?>>(x => x.Wrapping(() =>
			{
				callback();
				return null;
			}));
		}

		public static void FakeEffect(Func<Action> callback, out Func<Action> effect, out Action teardown)
		{
			var fakeTeardown = A.Fake<Action>();
			var wrappedEffect = () =>
			{
				var result = callback();

				return () =>
				{
					fakeTeardown();
					result();
				};
			};
			var fakeEffect = A.Fake<Func<Action>>(x => x.Wrapping(wrappedEffect));

			effect = fakeEffect;
			teardown = fakeTeardown;
		}

		public static CollectionEachDelegate<T> FakeEach<T>(out List<(T Value, Action Dispose)> calls)
		{
			var fakeCallback = A.Fake<CollectionEachDelegate<T>>();
			var fakeCalls = new List<(T Value, Action Dispose)>();

			A.CallTo(() => fakeCallback(A<T>._, A<int>._))
				.ReturnsLazily(x =>
				{
					var dispose = A.Fake<Action>();
					fakeCalls.Add((x.GetArgument<T>(0), dispose)!);

					return dispose;
				});

			calls = fakeCalls;
			return fakeCallback;
		}

		public static CollectionEachDelegate<T> FakeEach<T>()
		{
			return A.FakeEach<T>(out _);
		}
	}
}
