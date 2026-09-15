using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace VibecoreHub.Desktop.Modules;

internal sealed class InsertionLineAdorner : Adorner
{
    private readonly bool _vertical;
    private readonly bool _after;

    public InsertionLineAdorner(UIElement adornedElement, bool vertical, bool after) : base(adornedElement)
    {
        _vertical = vertical;
        _after = after;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var size = AdornedElement.RenderSize;
        var accent = (Brush)Application.Current.FindResource("AccentBrush");
        var glow = new Pen(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), 7) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var line = new Pen(accent, 3) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        Point start;
        Point end;
        if (_vertical)
        {
            var x = _after ? Math.Max(2, size.Width - 2) : 2;
            start = new Point(x, 5);
            end = new Point(x, Math.Max(5, size.Height - 5));
        }
        else
        {
            var y = _after ? Math.Max(2, size.Height - 2) : 2;
            start = new Point(8, y);
            end = new Point(Math.Max(8, size.Width - 8), y);
        }
        drawingContext.DrawLine(glow, start, end);
        drawingContext.DrawLine(line, start, end);
    }
}
