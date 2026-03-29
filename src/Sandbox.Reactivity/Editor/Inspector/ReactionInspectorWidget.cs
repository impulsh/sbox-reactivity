#if DEBUG

using Sandbox.Reactivity.Internals;

namespace Sandbox.Reactivity.Editor.Inspector;

// ReSharper disable once UnusedType.Global
[Inspector(typeof(Effect))]
[Inspector(typeof(Derived<>))]
internal sealed class ReactionInspectorWidget : InspectorWidget
{
	private readonly string _displayName;

	private readonly IReaction _reaction;

	private readonly Layout _rootLayout;

	public ReactionInspectorWidget(SerializedObject so)
		: base(so)
	{
		_reaction = so.Targets.OfType<IReaction>().First();
		_displayName = _reaction.GetType().ToRichText();

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

		if (_reaction is Effect effect)
		{
			effect.OnDisposed += Rebuild;
		}
	}

	private void TryOpenImplementationCode()
	{
		if (_reaction.Location is { } location)
		{
			if (location.LastIndexOf(':') is var separatorIndex && separatorIndex != -1)
			{
				CodeEditor.OpenFile(location[..separatorIndex],
					int.TryParse(location[(separatorIndex + 1)..], out var line) ? line : 0);
			}
			else
			{
				CodeEditor.OpenFile(location);
			}
		}
	}

	private void OnHeaderContextMenu()
	{
		var menu = new ContextMenu(this);

		menu.AddOption(new Option
		{
			Icon = "code",
			Text = "Jump to implementation",
			Enabled = _reaction.Location != null,
			Triggered = TryOpenImplementationCode,
		});

		menu.OpenAtCursor();
	}

	public override void OnDestroyed()
	{
		if (_reaction is Effect effect)
		{
			effect.OnDisposed -= Rebuild;
		}

		base.OnDestroyed();
	}

	private void Rebuild()
	{
		var isDisposed = _reaction is Effect { IsDisposed: true };
		var isEffectRoot = _reaction is Effect { ShouldTrackDependencies: false };

		_rootLayout.Clear(true);

		_rootLayout.Children =
		[
			new Widget
			{
				Enabled = !isDisposed,
				TextColor = isDisposed ? Theme.TextControl.WithAlpha(0.6f) : Theme.TextControl,
				MouseRightClick = OnHeaderContextMenu,
				Layout = new Row
				{
					Alignment = TextFlag.LeftCenter,
					Margin = 8,
					Spacing = 12,
					Children =
					[
						new Label
						{
							Text = _reaction.Icon ?? "link",
							FontFamily = "Material Icons",
							FontSize = "16pt",
						},
						new Widget
						{
							Layout = new Column
							{
								Alignment = TextFlag.Left,
								Children =
								[
									new Label
									{
										Text = _reaction.Name ?? "Reaction",
										FontSize = "13pt",
										FontWeight = 500,
									},
									new Widget
									{
										Layout = new Row
										{
											Alignment = TextFlag.LeftCenter,
											Spacing = 4,
											Children =
											[
												isEffectRoot
													? new Label
													{
														FontFamily = "Material Icons",
														FontSize = "14px",
														TextColor = Theme.TextLight,
														Text = "anchor",
													}
													: null,
												new Label
												{
													Text = _reaction.GetType().ToRichText(),
												},
												isDisposed
													? new Label
													{
														Text = "(disposed)",
														FontStyle = "italic",
													}
													: null,
											],
										},
									},
									_reaction.Location is { } location
										? new Label
										{
											Text = location,
											TextColor = Theme.Primary.Desaturate(0.3f),
											TextDecoration = "underline",
											Cursor = CursorShape.Finger,
											MouseClick = TryOpenImplementationCode,
										}
										: null,
								],
							},
						},
						new Widget
						{
							HorizontalSizeMode = SizeMode.Flexible,
						},
						new IconButton("more_horiz")
						{
							Background = Color.Transparent,
							FixedSize = 20,
							IconSize = 14,
							Foreground = Theme.TextLight,
							OnClick = OnHeaderContextMenu,
						},
					],
				},
			},
		];

		if (_reaction.Parent is { })
		{
			_rootLayout.Children =
			[
				ControlSheet.CreateRow(new SerializedReactiveParentProperty(_reaction)), new Separator(4),
			];
		}

		if (_reaction is not Effect { ShouldTrackDependencies: false })
		{
			ControlSheet sheet;

			_rootLayout.Children =
			[
				new InspectorHeader
				{
					Icon = "hub",
					Title = "Dependencies",
					Enabled = !isDisposed,
					IsCollapsable = false,
					ContextMenu =
						menu => Settings<ReactionInspectorWidget>.PopulateMenu(menu,
							property => property.Where(x => x.Group == "Dependencies"),
							Rebuild),
					ToolTip =
						$"The reactive values that were read during this {_displayName}'s last run. If any dependency changes, this {_displayName} will run again.",
					AutoBuild = true,
				},
				new Widget
				{
					Layout = sheet = new ControlSheet(),
				},
			];

			foreach (var producer in _reaction.Dependencies)
			{
				sheet.AddControl<ReactionDependencyControlWidget>(
					new SerializedReactionDependencyProperty(producer, _reaction));
			}
		}

		_rootLayout.AddStretchCell();
	}
}

#endif
