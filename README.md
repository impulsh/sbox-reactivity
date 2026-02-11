# sbox-reactivity

A reactivity system for [s&box](https://sbox.game/).

## Quick Start

- Add `global using static Sandbox.Reactivity.Reactive;` to your `Assembly.cs`.
- Subclass `ReactiveComponent` to enable usage of reactive properties

```csharp
public class MyComponent : ReactiveComponent
{
	// a simple reactive property that can be used with effects
	[Reactive]
	public int MyProperty { get; set; } = 1;

	// a derived property stores and reuses the result of its computation
	// until one of its dependencies change
	[Reactive]
	[Derived(nameof(_isEven))]
	public bool IsEven { get; } = false;

	private bool _isEven()
	{
		return MyProperty % 2 == 0;
	}

	protected override void OnActivate()
	{
		// an effect will re-run every time a reactive property that's
		// read inside of it changes its value
		Effect(() =>
		{
			Log.Info($"is even: {IsEven}");

			// you can optionally return a function to run when the
			// effect is about to re-run due to a dependency changing,
			// or if it's being disposed. reactive components dispose
			// their effects when they're disabled
			return () =>
			{
				Log.Info($"changing from: {IsEven}");
			};
		});
	}
}
```
