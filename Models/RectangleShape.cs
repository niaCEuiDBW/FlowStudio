namespace FlowStudio.Models;

public class RectangleShape : Shape
{
    public override void Draw(Graphics graphics)
    {
        using var brush = new SolidBrush(FillColor);
        using var pen = new Pen(StrokeColor, StrokeWidth);
        graphics.FillRectangle(brush, Bounds);
        graphics.DrawRectangle(pen, Bounds);
        DrawCenteredText(graphics, new RectangleF(X + 7, Y + 5, Math.Max(1, Width - 14), Math.Max(1, Height - 10)));
        DrawSelectionFrame(graphics);
    }

    public override bool ContainsPoint(Point point) => Bounds.Contains(point);
}
