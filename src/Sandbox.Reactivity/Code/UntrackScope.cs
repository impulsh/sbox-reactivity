namespace Sandbox.Reactivity;

/// <summary>
/// A disposable that disables reactivity tracking when constructed, and re-enables it when it's disposed.
/// </summary>
public readonly ref struct UntrackScope : IDisposable
{
	private readonly bool _previousIsUntracking;

	public UntrackScope()
	{
		_previousIsUntracking = Reactive.Runtime.IsUntracking;
		Reactive.Runtime.IsUntracking = true;
	}

	public void Dispose()
	{
		Reactive.Runtime.IsUntracking = _previousIsUntracking;
	}
}
