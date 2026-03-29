#if DEBUG

using Sandbox.Reactivity.Internals.Runtimes;
using Margin = Sandbox.UI.Margin;

namespace Sandbox.Reactivity.Editor.Inspector;

// ReSharper disable once UnusedType.Global
[Inspector(typeof(InfiniteLoopException))]
internal sealed class InfiniteLoopInspectorWidget : InspectorWidget
{
	private readonly InfiniteLoopException _exception;

	private readonly Layout _rootLayout;

	static InfiniteLoopInspectorWidget()
	{
		Runtime.OnFlushInfiniteLoop += e => Log.Error($"{e} - click to inspect");
	}

	public InfiniteLoopInspectorWidget(SerializedObject so)
		: base(so)
	{
		_exception = so.Targets.OfType<InfiniteLoopException>().First();

		Layout = new Column
		{
			Children =
			[
				new ScrollArea(this)
				{
					Canvas = new Widget
					{
						HorizontalSizeMode = SizeMode.Flexible,
						Layout = _rootLayout = new Column(),
					},
				},
			],
		};

		Rebuild();
	}

	[EditorEvent.Hotload]
	private void Rebuild()
	{
		_rootLayout.Clear(true);

		_rootLayout.Children =
		[
			new Label
			{
				Margin = 8,
				WordWrap = true,
				TextColor = Theme.TextControl.WithAlpha(0.7f),
				Text =
					"An infinite loop occurred while flushing effects at the end of a fixed update.<br/><br/>This usually means an effect is both reading and writing to the same reactive value. Inspect your effects for any unintended dependencies.",
			},
			new InspectorHeader
			{
				Title = "Effect Runs",
				IsCollapsable = false,
				AutoBuild = true,
			},
		];

		if (_exception.EffectExecutions is { Count: > 0 })
		{
			foreach (var (effect, executions) in _exception.EffectExecutions.OrderByDescending(x => x.Value))
			{
				var color = executions switch
				{
					> 900 => Color.Red.Desaturate(0.33f),
					> 500 => Color.Orange,
					> 100 => Color.Yellow,
					_ => Theme.TextControl,
				};
				var controlWidget = ControlWidget.Create(new SerializedReactiveObjectProperty(effect));
				controlWidget.ToolTip = null;
				controlWidget.TextColor = Color.White;

				_rootLayout.Add(new Widget
				{
					Cursor = CursorShape.Finger,
					ToolTip =
						$"This effect ran <span style='font-weight: 600; color: {color.ToString(false, true)}'>{executions:N0}</span> time{(executions != 1 ? "s" : "")} during a reactivity flush.",
					MouseClick = () => EditorUtility.InspectorObject = effect,
					MouseRightClick = () =>
					{
						var menu = new ContextMenu();
						controlWidget.OnLabelContextMenu(menu);

						menu.OpenAtCursor();
					},
					Layout = new Row
					{
						Margin = new Margin(6, 4, 6, 0),
						Spacing = 4,
						Children =
						[
							new Label
							{
								Text = $"x{executions:N0}",
								FixedWidth = 60f,
								TextColor = color,
							},
							controlWidget,
						],
					},
				});
			}
		}

		_rootLayout.AddStretchCell();
	}
}

#endif
