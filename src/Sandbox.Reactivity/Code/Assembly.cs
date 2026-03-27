global using System;
global using System.Collections.Generic;
using System.Runtime.CompilerServices;

// sbox renames assemblies for game compiles, so we need to add both the regular and game assembly names
[assembly: InternalsVisibleTo("Sandbox.Reactivity.editor")]
[assembly: InternalsVisibleTo("package.igor.reactivity.editor")]
