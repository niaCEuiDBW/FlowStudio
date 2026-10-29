using System.Drawing.Drawing2D;

namespace FlowStudio.Models;

public class TerminatorShape : Shape
{
    private GraphicsPath CreatePath()
    {
        float radius = Math.Min(18f, Math.Min(Width, Height) / 3f);
        float diameter = radius * 2f;
        var path = new GraphicsPath();
        path.AddArc(X, Y, diameter, diameter, 180, 90);
        path.AddArc(X + Width - diameter, Y, diameter, diameter, 270, 90);
        path.AddArc(X + Width - diameter, Y + Height - diameter, diameter, diameter, 0, 90);
        path.AddArc(X, Y + Height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public override void Draw(Graphics graphics)
    {
        using var path = CreatePath();
        using var brush = new SolidBrush(FillColor);
        using var pen = new Pen(StrokeColor, StrokeWidth);
        graphics.FillPath(brush, path);
        graphics.DrawPath(pen, path);
        DrawCenteredText(graphics, new RectangleF(X + 10, Y + 5, Math.Max(1, Width - 20), Math.Max(1, Height - 10)));
        DrawSelectionFrame(graphics);
    }

    public override bool ContainsPoint(Point point)
    {
        using var path = CreatePath();
        return path.IsVisible(point);
    }
}
