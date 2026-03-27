#if DEBUG

using Sandbox.Reactivity.Internals;

namespace Sandbox.Reactivity.Editor.Inspector;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class ReactionDependencyControlWidget : ControlWidget
{
	public ReactionDependencyControlWidget(SerializedReactionDependencyProperty property)
		: base(property)
	{
		var producerTypeText = property.Producer.GetType().ToRichText();

		Layout = new Row
		{
			Alignment = TextFlag.LeftCenter,
			Spacing = 4,
			Children =
			[
				new Widget
				{
					ToolTip = property.Producer is IReaction
						? $"<strong>{producerTypeText}</strong><br/><br/>This dependency computes its value from other reactive values."
						: $"<strong>{producerTypeText}</strong><br/><br/>This dependency stores a reactive value.",
					Layout = new Row
					{
						Children =
						[
							new Label
							{
								FontFamily = "Material Icons",
								FontSize = "16px",
								TextColor = Theme.TextLight,
								Text = property.Producer.Icon ?? "",
							},
						],
					},
				},
				Create(property),
			],
		};
	}

	protected override void OnPaint()
	{
	}

	public override void OnLabelContextMenu(ContextMenu menu)
	{
		var property = (SerializedReactionDependencyProperty)SerializedProperty;
		var parent = property.Producer.Parent;

		if (property.Producer is IReaction reaction)
		{
			menu.AddOption(new Option
			{
				Text = "Inspect",
				Icon = "manage_search",
				Triggered = () => EditorUtility.InspectorObject = reaction,
			});
		}

		menu.AddOption(new Option
		{
			Text = "Find declaring object in scene",
			Icon = "search",
			Enabled = parent is Component or GameObject,
			Triggered = () =>
			{
				switch (parent)
				{
					case Component component:
						EditorUtility.FindInScene(component);
						break;
					case GameObject go:
						EditorUtility.FindInScene(go);
						break;
				}
			},
		});

		menu.AddSeparator();
	}
}

#endif
