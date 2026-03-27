#if DEBUG

namespace Sandbox.Reactivity.Editor;

[CodeGenerator(CodeGeneratorFlags.WrapPropertyGet | CodeGeneratorFlags.Static,
	"Sandbox.Reactivity.Editor.SettingAttribute.Get")]
[CodeGenerator(CodeGeneratorFlags.WrapPropertySet | CodeGeneratorFlags.Static,
	"Sandbox.Reactivity.Editor.SettingAttribute.Set")]
[AttributeUsage(AttributeTargets.Property)]
internal class SettingAttribute : Attribute
{
	private static readonly Dictionary<string, object?> Cached = [];

	private string? _key;

	private string GetKey(int memberIdent)
	{
		if (_key == null)
		{
			var property = EditorTypeLibrary.GetMemberByIdent(memberIdent);
			_key = $"{property.DeclaringType.FullName}.{property.Name}";
		}

		return _key;
	}

	// ReSharper disable once UnusedMember.Global
	internal static T Get<T>(in WrappedPropertyGet<T> wrapped)
	{
		if (wrapped.GetAttribute<SettingAttribute>() is not { } setting)
		{
			throw new InvalidOperationException();
		}

		var key = setting.GetKey(wrapped.MemberIdent);
		return Cached.TryGetValue(key, out var value) ? (T)value! : ProjectCookie.Get(key, wrapped.Value);
	}

	// ReSharper disable once UnusedMember.Global
	internal static void Set<T>(in WrappedPropertySet<T> wrapped)
	{
		if (wrapped.GetAttribute<SettingAttribute>() is not { } setting)
		{
			throw new InvalidOperationException();
		}

		var key = setting.GetKey(wrapped.MemberIdent);

		Cached[key] = wrapped.Value;
		ProjectCookie.Set(key, wrapped.Value);
	}
}

internal static class Settings<T>
	where T : notnull
{
	private static IEnumerable<PropertyDescription> All =>
		EditorTypeLibrary.GetType<T>().Properties.Where(x => x.IsStatic && x.HasAttribute<SettingAttribute>());

	public static void PopulateMenu(
		global::Editor.Menu menu,
		Func<IEnumerable<PropertyDescription>, IEnumerable<PropertyDescription>>? filter = null,
		Action? onSettingChanged = null
	)
	{
		var settings = filter?.Invoke(All) ?? All;
		var groups = settings.GroupBy(x => x.GetDisplayInfo().Group);
		var first = true;

		foreach (var group in groups)
		{
			var properties = group.ToList();

			if (properties.Count == 0)
			{
				continue;
			}

			if (first)
			{
				first = false;
			}

			menu.AddSeparator();

			foreach (var property in properties)
			{
				if (property.GetValue(null) is not bool value)
				{
					continue;
				}

				var display = property.GetDisplayInfo();
				var option = new Option
				{
					Text = display.Name ?? property.Name,
					Icon = display.Icon,
					StatusTip = display.Description,
					ToolTip = display.Description,
					Checkable = true,
					Checked = value,
					Toggled = newValue => property.SetValue(null, newValue),
					Triggered = onSettingChanged,
				};

				menu.AddOption(option);
			}
		}
	}

	public static void OpenContextMenu(
		Widget? parent,
		Func<IEnumerable<PropertyDescription>, IEnumerable<PropertyDescription>>? filter = null
	)
	{
		var menu = new ContextMenu(parent);
		PopulateMenu(menu, filter);

		menu.OpenAtCursor();
	}
}

#endif
