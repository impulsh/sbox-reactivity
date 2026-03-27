#if DEBUG

using System.Reflection;
using System.Text;
using Sandbox.Internal;
using Sandbox.Reactivity.Internals;

namespace Sandbox.Reactivity.Editor.Inspector;

/// <summary>
/// A property that corresponds to an <see cref="IProducer" /> that an <see cref="IReaction" /> depends on. It uses the
/// name/location of the property the producer was accessed from (where possible).
/// </summary>
internal class SerializedReactionDependencyProperty : SerializedProperty
{
	private readonly IReaction _reaction;

	private readonly string? _sourceFile;

	private readonly int _sourceLine;

	private readonly PropertyInfo _valueProperty;

	public SerializedReactionDependencyProperty(IProducer producer, IReaction reaction)
	{
		if (producer.GetType() is not { GenericTypeArguments: [var valueType] } producerType)
		{
			throw new InvalidOperationException("Could not determine producer type");
		}

		if (producerType.GetProperty("Value", valueType) is not { } valueProperty)
		{
			throw new InvalidOperationException("Could not find producer value property");
		}

		_reaction = reaction;
		_valueProperty = valueProperty;
		Producer = producer;
		PropertyType = valueType;

		if (reaction is Effect effect)
		{
			void OnEffectDispose()
			{
				NoteChanged();

				effect.OnDisposed -= OnEffectDispose;
			}

			effect.OnDisposed += OnEffectDispose;
		}

		if (producer.Container is { SourceFile: { } propertyFile, SourceLine: var propertyLine })
		{
			_sourceFile = propertyFile;
			_sourceLine = propertyLine;
		}
		else if (producer.Location is { } location)
		{
			var separatorIndex = location.LastIndexOf(':');

			if (separatorIndex != -1)
			{
				_sourceFile = location[..separatorIndex];
				_sourceLine = int.TryParse(location[(separatorIndex + 1)..], out var line) ? line : 0;
			}
			else
			{
				_sourceFile = location;
			}
		}
	}

	// needed since we can't get a concrete generic type definition from a type library to call SetValue on that will
	// do this internally. conversion is required for things like enums because the control widget passes around an
	// int64 instead of the actual underlying type
	private static MethodInfo TryConvertMethod =>
		field ??= typeof(GlobalSystemNamespace).Assembly.GetType("Sandbox.Translation")
				?.GetMethod("TryConvert",
					BindingFlags.Static | BindingFlags.NonPublic,
					[typeof(object), typeof(Type), typeof(object).MakeByRefType()]) ??
			throw new MemberAccessException("Could not find Sandbox.Translation.TryConvert");

	public IProducer Producer { get; }

	public override Type PropertyType { get; }

	public override bool IsProperty => true;

	public override bool IsEditable => _reaction is not Effect { IsDisposed: true };

	public override bool IsValid => _reaction is not Effect { IsDisposed: true } && base.IsValid;

	public override string Name => Producer.Name ?? "Reactive Object";

	public override string DisplayName => Producer.Container?.Title ?? Producer.Name ?? "Reactive Object";

	public override string Description
	{
		get
		{
			var description = "";
			var containerProperty = "";
			var fileLocation = "";

			if (Producer.Container is { } property)
			{
				description = property.Description;
				containerProperty =
					$" {property.TypeDescription.TargetType.ToRichText()}.<span style='color: #9CDCFE; font-weight: 600;'>{property.Name}</span>";
			}

			if (_sourceFile != null)
			{
				var line = _sourceLine != 0 ? $":{_sourceLine}" : "";
				fileLocation =
					$"<br/><span style='color: {Theme.Primary.Desaturate(0.3f).Hex};'>{_sourceFile}{line}</span>";
			}

			StringBuilder result = new();

			if (!string.IsNullOrWhiteSpace(containerProperty) || !string.IsNullOrWhiteSpace(_sourceFile))
			{
				result.Append("Declared in:");
			}

			result.Append(containerProperty);
			result.Append(fileLocation);

			if (!string.IsNullOrWhiteSpace(description))
			{
				result.Append("<br/><br/>");
				result.Append(description);
			}

			return result.ToString();
		}
	}

	public override string SourceFile => _sourceFile!;

	public override int SourceLine => _sourceLine;

	public override void SetValue<T>(T value)
	{
		object?[] parameters = [value, _valueProperty.PropertyType, null];

		if (TryConvertMethod.Invoke(null, parameters) is false)
		{
			return;
		}

		NotePreChange();
		_valueProperty.SetValue(Producer, parameters[2]);
		NoteChanged();
	}

	public override T GetValue<T>(T defaultValue = default!)
	{
		return ValueToType(Producer.NonReactiveValue, defaultValue);
	}
}

#endif
