using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Text.RegularExpressions;
using System.Globalization;
namespace Swealium.Graphics;

// A parser of formatted text only: no browsing context, scripts, stylesheets or resource loader.
internal static class HtmlText
{
    internal static IEnumerable<TextRun> Parse(string html, TextStyle style)
    {
        var document = new HtmlParser().ParseDocument(html);
        List<TextRun> runs = [];
        void Append(string text, TextStyle current)
        {
            if (text.Length != 0) runs.Add(new(text, current));
        }
        void Break(TextStyle current)
        {
            if (runs.Count > 0 && !runs[^1].Text.EndsWith('\n')) Append("\n", current);
        }
        void Visit(INode node, TextStyle current, bool pre, int depth = 0)
        {
            if (depth > 256) return;
            if (node is IText text)
            {
                string value = pre ? text.Data : Regex.Replace(text.Data, @"\s+", " ");
                if (!pre && (runs.Count == 0 || runs[^1].Text.EndsWith(' ') || runs[^1].Text.EndsWith('\n'))) value = value.TrimStart(' ');
                Append(value, current); return;
            }
            if (node is not IElement element) return;
            string tag = element.LocalName;
            if (tag is "script" or "style" or "iframe" or "object" or "noscript" or "svg" or "img") return;
            bool block = tag is "p" or "div" or "pre" or "li" or "blockquote" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6";
            if (block) Break(current);
            if (tag == "br") { Append("\n", current); return; }
            current = tag switch
            {
                "b" or "strong" => current with { FontWeight = 700 },
                "i" or "em" => current with { Italic = true },
                "u" => current with { Underline = true },
                "s" or "del" or "strike" => current with { StrikeThrough = true },
                "sub" => current with { Script = TextScript.Subscript },
                "sup" => current with { Script = TextScript.Superscript },
                "code" or "pre" => current with { FontFamily = "monospace" },
                "h1" => current with { FontSize = current.FontSize * 2, FontWeight = 700 },
                "h2" => current with { FontSize = current.FontSize * 1.5f, FontWeight = 700 },
                "h3" => current with { FontSize = current.FontSize * 1.2f, FontWeight = 700 },
                _ => current
            };
            if (element.GetAttribute("dir") is { } dir) current = current with { Direction = dir == "rtl" ? TextDirection.RightToLeft : TextDirection.LeftToRight };
            foreach (string declaration in (element.GetAttribute("style") ?? "").Split(';'))
            {
                var pair = declaration.Split(':', 2); if (pair.Length != 2) continue;
                string key = pair[0].Trim().ToLowerInvariant(), value = pair[1].Trim();
                if (key == "color" && TryColor(value, out var color)) current = current with { Foreground = color };
                if (key == "background-color" && TryColor(value, out color)) current = current with { Background = color };
                if (key == "font-family") current = current with { FontFamily = value.Split(',')[0].Trim(' ', '\'', '"') };
                if (key == "font-style") current = current with { Italic = value == "italic" || value == "oblique" };
                if (key == "font-weight")
                {
                    int weight = value == "bold" ? 700 : value == "normal" ? 400 : int.TryParse(value, out int n) ? n : current.FontWeight;
                    if (weight is >= 1 and <= 1000) current = current with { FontWeight = weight };
                }
                if (key == "text-decoration") current = current with { Underline = value.Contains("underline"), StrikeThrough = value.Contains("line-through") };
                if (key == "font-size")
                {
                    float factor = value.EndsWith("em") ? current.FontSize : value.EndsWith('%') ? current.FontSize / 100 : 1;
                    string number = value.EndsWith("px") || value.EndsWith("em") ? value[..^2] : value.TrimEnd('%');
                    if (float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out float size) && float.IsFinite(size * factor) && size > 0)
                        current = current with { FontSize = size * factor };
                }
            }
            if (tag == "li") Append("• ", current);
            foreach (var child in node.ChildNodes) Visit(child, current, pre || tag == "pre", depth + 1);
            if (block) Break(current);
        }
        foreach (var node in document.Body!.ChildNodes) Visit(node, style, false);
        if (runs.Count > 0) runs[^1] = runs[^1] with { Text = runs[^1].Text.TrimEnd('\n') };
        return runs;
    }
    private static bool TryColor(string value, out Color color)
    {
        if (value.StartsWith('#'))
        {
            string hex = value[1..];
            if (hex.Length is 3 or 4) hex = string.Concat(hex.Select(c => new string(c,2)));
            if (hex.Length == 6) hex += "ff";
            if (hex.Length == 8 && uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgba))
            { color = new((byte)(rgba>>24),(byte)(rgba>>16),(byte)(rgba>>8),(byte)rgba); return true; }
        }
        color = value.ToLowerInvariant() switch { "red" => new(255,0,0), "blue" => new(0,0,255), "green" => new(0,128,0), "white" => Color.White, "black" => Color.Black, "transparent" => Color.Transparent, _ => default };
        return value.ToLowerInvariant() is "red" or "blue" or "green" or "white" or "black" or "transparent";
    }
}
