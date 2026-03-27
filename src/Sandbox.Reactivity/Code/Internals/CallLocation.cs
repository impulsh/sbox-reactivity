#if DEBUG && SANDBOX
using System.IO;
#endif

namespace Sandbox.Reactivity.Internals;

/// <summary>
/// Used to avoid littering every method with optional caller info attributes.
/// </summary>
internal readonly ref struct CallLocation
{
#if DEBUG
	private readonly string? _location;
#endif

	/// <summary>
	/// Captures the current stack trace and uses it to determine the location of the current call.
	/// </summary>
	/// <param name="skipFrames">How many stack frames to skip when determining the call location.</param>
	public CallLocation(int skipFrames)
	{
#if DEBUG
		skipFrames += 2; // skip call to Environment.StackTrace and this method

		var span = Environment.StackTrace.AsSpan();
		var i = 0;

		foreach (var range in span.Split('\n'))
		{
			if (i++ < skipFrames)
			{
				continue;
			}

			var line = span[range];
			var locationIndex = line.IndexOf(") in ");

			if (locationIndex != -1)
			{
				var location = line[(locationIndex + 5)..];
				var separatorIndex = location.LastIndexOf(":line ");

				if (separatorIndex != -1)
				{
					var path = location[..separatorIndex];
					var lineNumber = location[(separatorIndex + 6)..].Trim();

					_location = $"{path}:{lineNumber}";
				}
				else
				{
					_location = location.ToString();
				}
			}
			else
			{
				_location = line.ToString();
			}

#if SANDBOX
			var codePath = Project.Current.GetCodePath();

			if (_location.StartsWith(codePath, StringComparison.InvariantCultureIgnoreCase))
			{
				_location = Path.GetRelativePath(codePath, _location);
			}
#endif
			break;
		}
#endif
	}

	/// <summary>
	/// Uses the given method as the location of the current call.
	/// </summary>
	/// <param name="type">The type to get the method from.</param>
	/// <param name="methodName">The name of the method in the given type to use as the call location.</param>
	public CallLocation(Type type, string methodName)
	{
#if DEBUG && SANDBOX
		if (TypeLibrary.GetType(type)?.GetMethod(methodName) is { } method)
		{
			_location = $"{method.SourceFile}:{method.SourceLine}";
		}
#endif
	}

	public static implicit operator string?(CallLocation capture)
	{
#if DEBUG
		return capture._location;
#else
		return null;
#endif
	}
}
