using System.Threading.Tasks;
using static Sandbox.Reactivity.Reactive;

namespace Sandbox.Reactivity.Tests;

public class Api
{
	[Test]
	public async Task State_IsCreatedFromFactory()
	{
		var state = State(0);
		await Assert.That(state).IsTypeOf<State<int>>();
	}

	[Test]
	public async Task State_CanBeAbstracted()
	{
		var state = State(0);
		await Assert.That(state).IsAssignableTo<IState<int>>();
	}

	[Test]
	public async Task Derived_IsCreatedFromFactory()
	{
		var derived = Derived(() => 0);
		await Assert.That(derived).IsTypeOf<Derived<int>>();
	}

	[Test]
	public async Task Derived_CanBeAbstracted()
	{
		var derived = Derived(() => 0);
		await Assert.That(derived).IsAssignableTo<IState<int>>();
	}
}
