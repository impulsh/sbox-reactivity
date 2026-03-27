#if DEBUG

using Sandbox.Reactivity.Internals;
using Margin = Sandbox.UI.Margin;

namespace Sandbox.Reactivity.Editor.Inspector;

// ReSharper disable once UnusedType.Global
[CustomEditor(typeof(IReactiveObject))]
internal sealed class ReactiveObjectControlWidget : ControlWidget
{
	private readonly IReactiveObject _reactive;

	public ReactiveObjectControlWidget(SerializedProperty property)
		: base(property)
	{
		_reactive = property.GetValue<IReactiveObject>();

		ReadOnly = true;
		Cursor = CursorShape.Finger;

		Layout = new Row
		{
			Alignment = TextFlag.LeftCenter,
			Spacing = 8,
			Margin = new Margin(6, 0, 0, 0),
			Children =
			[
				new Label
				{
					FontFamily = "Material Icons",
					FontSize = "14px",
					TextColor = Theme.TextLight,
					Text = _reactive.Icon ?? "link",
				},
				new Label
				{
					Text = _reactive.Name,
					TextColor = Theme.TextLight,
				},
			],
		};
	}

	protected override void OnContextMenu(ContextMenuEvent e)
	{
	}

	public override void OnLabelContextMenu(ContextMenu menu)
	{
		menu.AddOption(new Option
		{
			Text = "Inspect",
			Icon = "manage_search",
			Triggered = () => EditorUtility.InspectorObject = _reactive,
		});

		menu.AddSeparator();
	}
}

#endif
