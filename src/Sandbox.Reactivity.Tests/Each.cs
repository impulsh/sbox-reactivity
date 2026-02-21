using System.Threading.Tasks;
using FakeItEasy;
using static Sandbox.Reactivity.Reactive;

namespace Sandbox.Reactivity.Tests;

public class Each
{
	private static async Task AssertHasNoDisposeCalls<T>(List<(T Value, Action Dispose)> calls, int? callCount = null)
	{
		if (callCount is { } count)
		{
			await Assert.That(calls).Count().IsEqualTo(count);
		}

		foreach (var (_, dispose) in calls)
		{
			A.CallTo(dispose).MustNotHaveHappened();
		}
	}

	[Test]
	public void RunsForEveryInitialItem()
	{
		var items = State<string[]>(["hello", "world"]);
		var callback = A.FakeEach<string>();

		EffectRoot(() => { Each(() => items.Value, callback); });
		A.CallTo(() => callback("hello", 0)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("world", 1)).MustHaveHappenedOnceExactly();
	}

	[Test]
	public void RunsForAddedItems()
	{
		var items = State<string[]>(["hello"]);
		var callback = A.FakeEach<string>();

		EffectRoot(() => { Each(() => items.Value, callback); });
		A.CallTo(() => callback("hello", 0)).MustHaveHappenedOnceExactly();

		items.Value = [..items.Value, "world"];
		Flush();

		A.CallTo(() => callback("hello", 0)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("world", 1)).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task RunsForSwappedItems()
	{
		var items = State<string[]>(["hello", "world", "!"]);
		var callback = A.FakeEach<string>(out var calls);

		EffectRoot(() => { Each(() => items.Value, callback); });

		await Assert.That(calls).Count().IsEqualTo(3);
		A.CallTo(() => callback("hello", 0)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("world", 1)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("!", 2)).MustHaveHappenedOnceExactly();

		items.Value = [items.Value[0], items.Value[2], items.Value[1]];
		Flush();

		await Assert.That(calls).Count().IsEqualTo(5);
		A.CallTo(() => callback("hello", 0)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("world", 1)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("!", 2)).MustHaveHappenedOnceExactly();

		A.CallTo(() => callback("world", 2)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("!", 1)).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task DoesNotRunForSwappedEqualItems()
	{
		var items = State<string[]>(["hello", "world", "world"]);
		var callback = A.FakeEach<string>(out var calls);

		EffectRoot(() => { Each(() => items.Value, callback); });

		await Assert.That(calls).Count().IsEqualTo(3);
		A.CallTo(() => callback("hello", 0)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("world", 1)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("world", 2)).MustHaveHappenedOnceExactly();

		items.Value = [items.Value[0], items.Value[2], items.Value[1]];
		Flush();

		await Assert.That(calls).Count().IsEqualTo(3);
		A.CallTo(() => callback("hello", 0)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("world", 1)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("world", 2)).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task AllowsDuplicateValues()
	{
		var items = State<string[]>(["hello"]);
		var callback = A.FakeEach<string>(out var calls);

		EffectRoot(() => { Each(() => items.Value, callback); });
		A.CallTo(() => callback("hello", 0)).MustHaveHappenedOnceExactly();

		items.Value = [..items.Value, "hello"];
		Flush();

		await Assert.That(calls).Count().IsEqualTo(2);
		A.CallTo(() => callback("hello", 0)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("hello", 1)).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task DoesNotTrackDependencies()
	{
		var state = State(0);
		var items = State<string[]>(["hello", "world"]);
		var callback = A.FakeEach<string>(out var calls);

		EffectRoot(() =>
		{
			Each(() => items.Value,
				(item, i) =>
				{
					_ = state.Value;
					return callback(item, i);
				});
		});

		await AssertHasNoDisposeCalls(calls, 2);
		A.CallTo(() => callback("hello", 0)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("world", 1)).MustHaveHappenedOnceExactly();

		state.Value = 1;
		Flush();

		await AssertHasNoDisposeCalls(calls, 2);
		A.CallTo(() => callback("hello", 0)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("world", 1)).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task Teardown_DoesNotCallForInitialItems()
	{
		var items = State<string[]>(["hello", "world"]);
		var callback = A.FakeEach<string>(out var calls);

		EffectRoot(() => { Each(() => items.Value, callback); });

		await AssertHasNoDisposeCalls(calls, 2);
	}

	[Test]
	public async Task Teardown_CalledForRemovedItems()
	{
		var items = State<string[]>(["hello", "world"]);
		var callback = A.FakeEach<string>(out var calls);

		EffectRoot(() => { Each(() => items.Value, callback); });

		await AssertHasNoDisposeCalls(calls, 2);

		items.Value = items.Value[..^1];
		Flush();

		await Assert.That(calls).Count().IsEqualTo(2);
		A.CallTo(calls[0].Dispose).MustNotHaveHappened();
		A.CallTo(calls[1].Dispose).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task Teardown_CalledForSwappedItems()
	{
		var items = State<string[]>(["hello", "world", "!"]);
		var callback = A.FakeEach<string>(out var calls);

		EffectRoot(() => { Each(() => items.Value, callback); });

		await AssertHasNoDisposeCalls(calls, 3);

		items.Value = [items.Value[0], items.Value[2], items.Value[1]];
		Flush();

		await Assert.That(calls).Count().IsEqualTo(5);
		A.CallTo(calls[0].Dispose).MustNotHaveHappened();
		A.CallTo(calls[1].Dispose).MustHaveHappenedOnceExactly();
		A.CallTo(calls[2].Dispose).MustHaveHappenedOnceExactly();
		A.CallTo(calls[3].Dispose).MustNotHaveHappened();
		A.CallTo(calls[4].Dispose).MustNotHaveHappened();
	}

	[Test]
	public async Task Teardown_DoesNotCallForSwappedEqualItems()
	{
		var items = State<string[]>(["hello", "world", "world"]);
		var callback = A.FakeEach<string>(out var calls);

		EffectRoot(() => { Each(() => items.Value, callback); });

		await AssertHasNoDisposeCalls(calls, 3);

		items.Value = [items.Value[0], items.Value[2], items.Value[1]];
		Flush();

		await AssertHasNoDisposeCalls(calls, 3);
	}

	[Test]
	public async Task Teardown_DoesNotRepeatForDuplicates()
	{
		var items = State<string[]>(["hello", "hello"]);
		var callback = A.FakeEach<string>(out var calls);

		EffectRoot(() => { Each(() => items.Value, callback); });
		await AssertHasNoDisposeCalls(calls, 2);

		items.Value = items.Value[..^1];
		Flush();

		await Assert.That(calls).Count().IsEqualTo(2);
		A.CallTo(calls[0].Dispose).MustNotHaveHappened();
		A.CallTo(calls[1].Dispose).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task Teardown_CalledWhenRootIsDisposed()
	{
		var items = State<string[]>(["hello", "world"]);
		var callback = A.FakeEach<string>(out var calls);

		var root = EffectRoot(() => { Each(() => items.Value, callback); });
		await AssertHasNoDisposeCalls(calls, 2);

		root.Dispose();

		await Assert.That(calls).Count().IsEqualTo(2);
		A.CallTo(() => callback("hello", 0)).MustHaveHappenedOnceExactly();
		A.CallTo(() => callback("world", 1)).MustHaveHappenedOnceExactly();
		A.CallTo(calls[0].Dispose).MustHaveHappenedOnceExactly();
		A.CallTo(calls[1].Dispose).MustHaveHappenedOnceExactly();
	}
}
