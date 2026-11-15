using Swealium.Graphics;
namespace Swealium.UI;

/// <summary>A retained holder for DrawingContext.DrawText. HTML is formatted text, not a web view.</summary>
public class Text : Widget
{
    private string content = "";
    private TextStyle style = new();
    private TextOptions options = new();
    private TextMarkup markup;
    public string Content { get => content; set { ArgumentNullException.ThrowIfNull(value); content = value; InvalidateLayout(); } }
    public TextStyle Style { get => style; set { ArgumentNullException.ThrowIfNull(value); value.Validate(); style = value; InvalidateLayout(); } }
    public TextOptions Options { get => options; set { ArgumentNullException.ThrowIfNull(value); value.Validate(); options = value; InvalidateLayout(); } }
    public TextMarkup Markup { get => markup; set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); markup = value; InvalidateLayout(); } }
    protected override Size MeasureContent(Size available) { using var layout = new TextLayout(Content, Style, Options, available.Width, Markup); return layout.Size; }
    public override void Draw(DrawingContext context) => context.DrawText(Content, ContentBounds, Style, Options, Markup);
}
