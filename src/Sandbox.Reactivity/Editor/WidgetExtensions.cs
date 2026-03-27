#if DEBUG

// ReSharper disable UnusedMember.Global

using System.Runtime.CompilerServices;
using Sandbox.UI;

namespace Sandbox.Reactivity.Editor;

internal static class WidgetExtensions
{
	private static readonly ConditionalWeakTable<Widget, Dictionary<string, string>> AppliedProperties = [];

	extension(Widget widget)
	{
		public string FontFamily
		{
			set => widget.SetStyleProperty("font-family", $"\"{value}\"");
		}

		public string FontSize
		{
			set => widget.SetStyleProperty("font-size", value);
		}

		public int FontWeight
		{
			set => widget.SetStyleProperty("font-weight", value.ToString());
		}

		public string FontStyle
		{
			set => widget.SetStyleProperty("font-style", value);
		}

		public Color TextColor
		{
			set => widget.SetStyleProperty("color", value.ToString(false, true));
		}

		public string TextDecoration
		{
			set => widget.SetStyleProperty("text-decoration", value);
		}

		public Color BackgroundColor
		{
			set => widget.SetStyleProperty("background-color", value.ToString(false, true));
		}

		public float BorderRadius
		{
			set => widget.SetStyleProperty("border-radius", $"{value}px");
		}

		private void SetStyleProperty(string property, string value)
		{
			AppliedProperties.GetOrCreateValue(widget)[property] = value;
			widget.RefreshStyles();
		}

		private void RefreshStyles()
		{
			if (AppliedProperties.TryGetValue(widget, out var properties))
			{
				widget.SetStyles(string.Join(';', properties.Select(x => $"{x.Key}: {x.Value}")));
			}
		}
	}

	extension(Layout layout)
	{
		/// <summary>
		/// Adds the given widgets to this layout. Does not overwrite the current child widgets.
		/// </summary>
		public ReadOnlySpan<Widget?> Children
		{
			set
			{
				foreach (var widget in value)
				{
					if (widget == null)
					{
						continue;
					}

					layout.Add(widget);
				}
			}
		}
	}

	extension(InspectorHeader widget)
	{
		public bool AutoBuild
		{
			set
			{
				if (value)
				{
					widget.BuildUI();
				}
			}
		}
	}
}

internal abstract class WrappedLayout
{
	protected abstract Layout Layout { get; }

	public float Spacing
	{
		get => Layout.Spacing;
		set => Layout.Spacing = value;
	}

	public SizeConstraint SizeConstraint
	{
		get => Layout.SizeConstraint;
		set => Layout.SizeConstraint = value;
	}

	public Margin Margin
	{
		get => Layout.Margin;
		set => Layout.Margin = value;
	}

	public TextFlag Alignment
	{
		get => Layout.Alignment;
		set => Layout.Alignment = value;
	}

	public ReadOnlySpan<Widget?> Children
	{
		set
		{
			foreach (var widget in value)
			{
				if (widget == null)
				{
					continue;
				}

				Layout.Add(widget);
			}
		}
	}

	public static implicit operator Layout(WrappedLayout wrapped)
	{
		return wrapped.Layout;
	}
}

internal sealed class Row(bool reversed = false) : WrappedLayout
{
	protected override Layout Layout { get; } = Layout.Row(reversed);
}

internal sealed class Column(bool reversed = false) : WrappedLayout
{
	protected override Layout Layout { get; } = Layout.Column(reversed);
}

#endif
