using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace VibecoreHub.Desktop.Modules;

internal sealed class DropTargetAdorner : Adorner
{
    public DropTargetAdorner(UIElement adornedElement) : base(adornedElement) => IsHitTestVisible = false;

    protected override void OnRender(DrawingContext drawingContext)
    {
        var size = AdornedElement.RenderSize;
        var accent = (Brush)Application.Current.FindResource("AccentBrush");
        var pen = new Pen(accent, 3) { LineJoin = PenLineJoin.Round };
        drawingContext.DrawRoundedRectangle(null, pen, new Rect(2, 2, Math.Max(0, size.Width - 4), Math.Max(0, size.Height - 4)), 13, 13);
    }
}
