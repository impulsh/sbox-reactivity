#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Sandbox.Reactivity;

#if JETBRAINS_ANNOTATIONS
[PublicAPI]
#endif
public interface IState<T>
{
	T Value { get; set; }
}
