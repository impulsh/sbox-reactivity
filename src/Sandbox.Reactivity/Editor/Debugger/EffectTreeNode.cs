#if DEBUG

using System.IO;
using Sandbox.Reactivity.Internals;

namespace Sandbox.Reactivity.Editor.Debugger;

internal class EffectTreeNode : TreeNode<Effect>, IDisposable
{
	private const float FadeDurationSeconds = 0.5f;

	private readonly Effect _effect;

	private readonly TimeSince _timeSinceCreated = DebuggerWidget.HighlightNewEffects ? 0 : double.MaxValue;

	private TimeSince _timeSinceLastRun = double.MaxValue;

	public EffectTreeNode(Effect effect)
		: base(effect)
	{
		EditorEvent.Unregister(this);

		_effect = effect;

		effect.OnChildEffectCreated += OnChildEffectCreated;
		effect.OnRerun += OnEffectRerun;
		effect.OnDisposed += Dispose;
	}

	// hack since treeview items are only updated every 100 ms, enables/disables the EditorEvent.Frame method below
	private bool IsAnimating
	{
		set
		{
			if (value == field)
			{
				return;
			}

			if (value)
			{
				EditorEvent.Register(this);
			}
			else
			{
				EditorEvent.Unregister(this);
			}

			field = value;
		}
	}

	public void Dispose()
	{
		_effect.OnChildEffectCreated -= OnChildEffectCreated;
		_effect.OnRerun -= OnEffectRerun;
		_effect.OnDisposed -= Dispose;

		IsAnimating = false;

		if (Parent != null)
		{
			Parent.RemoveItem(this);
		}
		else if (TreeView != null)
		{
			// calling RemoveItem internally calls ResolveObject when removing it from its internal list which ends up
			// trying to remove the effect from the tree instead of this node. setting Value to null will make
			// ResolveObject default to the node itself, making it correctly remove from the tree
			Value = null!;
			TreeView.RemoveItem(this);
			Value = _effect;
		}
	}

	public override bool OnContextMenu()
	{
		var menu = new ContextMenu(TreeView);

		menu.AddOption(new Option
		{
			Text = "Find containing object in scene",
			Icon = "search",
			Enabled = _effect.Parent is Component { IsValid: true } or GameObject { IsValid: true },
			Triggered = () =>
			{
				switch (_effect.Parent)
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

		menu.OpenAtCursor();
		return false;
	}

	private void OnChildEffectCreated(Effect effect)
	{
		var node = new EffectTreeNode(effect);
		AddItem(node);

		TreeView?.Open(this, true);
	}

	private void OnEffectRerun()
	{
		if (!DebuggerWidget.HighlightEffectReruns)
		{
			return;
		}

		_timeSinceLastRun = 0;
		Dirty();
	}

	// ReSharper disable once UnusedMember.Local
	[EditorEvent.Frame]
	private void OnAnimate()
	{
		RebuildOnDirty();
	}

	public override void OnSelectionChanged(bool selected)
	{
		if (selected)
		{
			EditorUtility.InspectorObject = _effect;
		}
		else if (EditorUtility.InspectorObject == _effect)
		{
			EditorUtility.InspectorObject = null;
		}
	}

	public override void OnPaint(VirtualWidget item)
	{
		Paint.ClearPen();
		Paint.ClearBrush();
		Paint.SetDefaultFont();

		var rect = item.Rect;
		var fullRect = rect with
		{
			Left = 0,
			Right = TreeView?.Width ?? 0f,
		};

		var isEven = item.Row % 2 == 0;
		var textColor = Theme.TextControl;
		var backgroundColor = Color.Transparent;

		if (item.Selected)
		{
			backgroundColor = Theme.SelectedBackground;
		}
		else if (item.Hovered)
		{
			backgroundColor = Theme.SelectedBackground.WithAlpha(0.25f);
		}
		else if (isEven)
		{
			backgroundColor = Theme.SurfaceLightBackground.WithAlpha(0.1f);
		}

		if (_timeSinceCreated < FadeDurationSeconds)
		{
			var fraction = FadeDurationSeconds - _timeSinceCreated;
			IsAnimating = true;

			textColor = textColor.LerpTo(Color.Green, fraction);
			backgroundColor = backgroundColor.LerpTo(Color.Green.WithAlpha(0.6f), fraction);
		}
		else if (_timeSinceLastRun < FadeDurationSeconds)
		{
			var fraction = FadeDurationSeconds - _timeSinceLastRun;
			IsAnimating = true;

			textColor = textColor.LerpTo(Color.Orange, fraction);
			backgroundColor = backgroundColor.LerpTo(Color.Orange.WithAlpha(0.6f), fraction);
		}
		else
		{
			IsAnimating = false;
		}

		Paint.SetBrush(backgroundColor);
		Paint.DrawRect(fullRect);

		Paint.Pen = textColor.WithAlpha(0.6f);

		rect.Left += 4;
		rect.Left += Paint.DrawIcon(rect, _effect.Icon ?? "link", 16, TextFlag.LeftCenter).Width + 4;

		Paint.Pen = textColor;
		rect.Left += Paint.DrawText(rect, _effect.Name ?? "Effect", TextFlag.LeftCenter).Width + 4;

		if (_effect.Location is { } location && DebuggerWidget.ShowSourcePath)
		{
			rect.Right -= 4;

			var filename = Path.GetFileName(location);
			var text = Paint.GetElidedText(filename, rect.Width, ElideMode.Left, TextFlag.RightCenter);

			Paint.Pen = textColor.WithAlpha(0.4f);
			Paint.DrawText(rect, text, TextFlag.RightCenter);
		}
	}
}

#endif
