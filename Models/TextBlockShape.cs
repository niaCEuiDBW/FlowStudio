using System.Drawing.Drawing2D;

namespace FlowStudio.Models;

public class TextBlockShape : Shape
{
    private Point[] GetVertices()
    {
        int fold = Math.Min(18, Math.Min(Width / 3, Height / 3));
        return new[]
        {
            new Point(X, Y),
            new Point(X + Width - fold, Y),
            new Point(X + Width, Y + fold),
            new Point(X + Width, Y + Height),
            new Point(X, Y + Height)
        };
    }

    public override void Draw(Graphics graphics)
    {
        Point[] vertices = GetVertices();
        using var brush = new SolidBrush(FillColor);
        using var pen = new Pen(StrokeColor, StrokeWidth);
        graphics.FillPolygon(brush, vertices);
        graphics.DrawPolygon(pen, vertices);

        int fold = Math.Min(18, Math.Min(Width / 3, Height / 3));
        graphics.DrawLine(pen, X + Width - fold, Y, X + Width - fold, Y + fold);
        graphics.DrawLine(pen, X + Width - fold, Y + fold, X + Width, Y + fold);
        DrawCenteredText(graphics, new RectangleF(X + 7, Y + 5, Math.Max(1, Width - 14), Math.Max(1, Height - 10)));
        DrawSelectionFrame(graphics);
    }

    public override bool ContainsPoint(Point point)
    {
        using var path = new GraphicsPath();
        path.AddPolygon(GetVertices());
        return path.IsVisible(point);
    }
}
