#if DEBUG

using Sandbox.Reactivity.Internals;
using Sandbox.UI;

namespace Sandbox.Reactivity.Editor.Debugger;

// ReSharper disable once ClassNeverInstantiated.Global
[Dock("Editor", "Reactivity Debugger", "local_fire_department")]
internal sealed partial class DebuggerWidget : Widget
{
	private readonly TreeView _tree;

	public DebuggerWidget(Widget? parent)
		: base(parent)
	{
		Layout = new Column
		{
			Spacing = 2,
			Children =
			[
				new Widget
				{
					Layout = new Row
					{
						Spacing = 2,
						Children =
						[
							// TODO: search bar
							new Widget
							{
								HorizontalSizeMode = SizeMode.Flexible,
							},
							new Widget
							{
								BackgroundColor = Theme.ControlBackground,
								BorderRadius = Theme.ControlRadius,
								Layout = new Row
								{
									Children =
									[
										new ToolButton("Settings", "more_vert", this)
										{
											MouseLeftPress = () => Settings<DebuggerWidget>.OpenContextMenu(this),
										},
									],
								},
							},
						],
					},
				},
				new Widget
				{
					BackgroundColor = Theme.ControlBackground,
					BorderRadius = Theme.ControlRadius,
					Layout = new Column
					{
						Children =
						[
							_tree = new TreeView
							{
								Margin = new Margin(8, 0),
								SelectionOverride = () => EditorUtility.InspectorObject,
							},
						],
					},
				},
			],
		};

		Effect.OnEffectRootCreated += OnEffectRootCreated;
	}

	public override void OnDestroyed()
	{
		foreach (var item in _tree.Items) // .Items makes a copy
		{
			if (item is EffectTreeNode node)
			{
				node.Dispose();
			}
		}

		_tree.Clear();

		Effect.OnEffectRootCreated -= OnEffectRootCreated;
	}

	private void OnEffectRootCreated(Effect root)
	{
		if (root.IsDisposed)
		{
			// just in case - effects from other scenes (e.g. prefab editors) might cause this to happen
			return;
		}

		if (!ShowUiEffects && root.Parent is IReactivePanel)
		{
			return;
		}

		if (!ShowGameplayEffects && root.Parent is (Component or GameObject) and not IReactivePanel)
		{
			return;
		}

		var node = new EffectTreeNode(root);
		_tree.AddItem(node);

		if (AutoExpand)
		{
			_tree.Open(node, true);
		}
	}
}

#endif
