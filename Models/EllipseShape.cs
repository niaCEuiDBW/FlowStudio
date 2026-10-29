namespace FlowStudio.Models;

public class EllipseShape : Shape
{
    public override void Draw(Graphics graphics)
    {
        using var brush = new SolidBrush(FillColor);
        using var pen = new Pen(StrokeColor, StrokeWidth);
        graphics.FillEllipse(brush, Bounds);
        graphics.DrawEllipse(pen, Bounds);
        DrawCenteredText(graphics, new RectangleF(X + 12, Y + 6, Math.Max(1, Width - 24), Math.Max(1, Height - 12)));
        DrawSelectionFrame(graphics);
    }

    public override bool ContainsPoint(Point point)
    {
        float radiusX = Width / 2f;
        float radiusY = Height / 2f;
        float centerX = X + radiusX;
        float centerY = Y + radiusY;
        float normalizedX = (point.X - centerX) / radiusX;
        float normalizedY = (point.Y - centerY) / radiusY;
        return normalizedX * normalizedX + normalizedY * normalizedY <= 1f;
    }

    public override Point GetConnectionPoint(Point toward)
    {
        float radiusX = Width / 2f;
        float radiusY = Height / 2f;
        float centerX = X + radiusX;
        float centerY = Y + radiusY;
        float deltaX = toward.X - centerX;
        float deltaY = toward.Y - centerY;

        if (Math.Abs(deltaX) < 0.001f && Math.Abs(deltaY) < 0.001f)
        {
            return Center;
        }

        float denominator = (deltaX * deltaX) / (radiusX * radiusX) + (deltaY * deltaY) / (radiusY * radiusY);
        if (denominator <= 0.0001f)
        {
            return Center;
        }

        float scale = 1f / MathF.Sqrt(denominator);
        return new Point((int)Math.Round(centerX + deltaX * scale), (int)Math.Round(centerY + deltaY * scale));
    }
}
