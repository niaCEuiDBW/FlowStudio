using System.Drawing.Drawing2D;

namespace FlowStudio.Models;

public class RhombusShape : Shape
{
    private Point[] GetVertices()
    {
        return new[]
        {
            new Point(X + Width / 2, Y),
            new Point(X + Width, Y + Height / 2),
            new Point(X + Width / 2, Y + Height),
            new Point(X, Y + Height / 2)
        };
    }

    public override void Draw(Graphics graphics)
    {
        using var brush = new SolidBrush(FillColor);
        using var pen = new Pen(StrokeColor, StrokeWidth);
        Point[] vertices = GetVertices();
        graphics.FillPolygon(brush, vertices);
        graphics.DrawPolygon(pen, vertices);
        DrawCenteredText(graphics, new RectangleF(X + Width * 0.2f, Y + Height * 0.18f, Width * 0.6f, Height * 0.64f));
        DrawSelectionFrame(graphics);
    }

    public override bool ContainsPoint(Point point)
    {
        using var path = new GraphicsPath();
        path.AddPolygon(GetVertices());
        return path.IsVisible(point);
    }

    public override Point GetConnectionPoint(Point toward)
    {
        float centerX = X + Width / 2f;
        float centerY = Y + Height / 2f;
        float deltaX = toward.X - centerX;
        float deltaY = toward.Y - centerY;
        float denominator = Math.Abs(deltaX) / (Width / 2f) + Math.Abs(deltaY) / (Height / 2f);

        if (denominator < 0.001f)
        {
            return Center;
        }

        float scale = 1f / denominator;
        return new Point((int)Math.Round(centerX + deltaX * scale), (int)Math.Round(centerY + deltaY * scale));
    }
}
