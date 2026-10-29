using System.Drawing.Drawing2D;

namespace FlowStudio.Models;

public class ConnectionLineShape : Shape
{
    public Guid SourceShapeId { get; set; }
    public Guid TargetShapeId { get; set; }
    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [System.ComponentModel.Browsable(false)]
    public override int DrawLayer => 0;

    [System.Text.Json.Serialization.JsonIgnore]
    [System.ComponentModel.Browsable(false)]
    public override bool CanMove => false;

    [System.Text.Json.Serialization.JsonIgnore]
    [System.ComponentModel.Browsable(false)]
    public override bool CanResize => false;

    [System.Text.Json.Serialization.JsonIgnore]
    [System.ComponentModel.Browsable(false)]
    public override bool CanConnect => false;

    public override void Draw(Graphics graphics)
    {
        using var arrow = new AdjustableArrowCap(5, 7, true);
        using var pen = new Pen(StrokeColor, StrokeWidth)
        {
            CustomEndCap = arrow,
            StartCap = LineCap.Round,
            EndCap = LineCap.Custom
        };
        graphics.DrawLine(pen, StartPoint, EndPoint);

        if (IsSelected)
        {
            using var markerBrush = new SolidBrush(Color.White);
            using var markerPen = new Pen(SystemColors.Highlight, 1.5f);
            foreach (Point point in new[] { StartPoint, EndPoint })
            {
                Rectangle marker = new(point.X - 4, point.Y - 4, 8, 8);
                graphics.FillEllipse(markerBrush, marker);
                graphics.DrawEllipse(markerPen, marker);
            }
        }
    }

    public override bool ContainsPoint(Point point)
    {
        float dx = EndPoint.X - StartPoint.X;
        float dy = EndPoint.Y - StartPoint.Y;
        float lengthSquared = dx * dx + dy * dy;

        if (lengthSquared < 0.001f)
        {
            return DistanceSquared(point, StartPoint) <= 64f;
        }

        float t = ((point.X - StartPoint.X) * dx + (point.Y - StartPoint.Y) * dy) / lengthSquared;
        t = Math.Clamp(t, 0f, 1f);
        float nearestX = StartPoint.X + t * dx;
        float nearestY = StartPoint.Y + t * dy;
        float distanceX = point.X - nearestX;
        float distanceY = point.Y - nearestY;
        float threshold = Math.Max(6f, StrokeWidth + 4f);
        return distanceX * distanceX + distanceY * distanceY <= threshold * threshold;
    }

    public override void UpdateConnections(IReadOnlyList<Shape> shapes)
    {
        Shape? source = shapes.FirstOrDefault(shape => shape.Id == SourceShapeId && shape.CanConnect);
        Shape? target = shapes.FirstOrDefault(shape => shape.Id == TargetShapeId && shape.CanConnect);

        if (source is null || target is null || source.Id == target.Id)
        {
            return;
        }

        StartPoint = source.GetConnectionPoint(target.Center);
        EndPoint = target.GetConnectionPoint(source.Center);
    }

    public override bool References(Guid id) => SourceShapeId == id || TargetShapeId == id;

    public override Point GetConnectionPoint(Point toward) => StartPoint;

    public override void Move(int deltaX, int deltaY)
    {
    }

    public override void Resize(ResizeHandle handle, Rectangle original, Point point)
    {
    }

    private static float DistanceSquared(Point first, Point second)
    {
        float dx = first.X - second.X;
        float dy = first.Y - second.Y;
        return dx * dx + dy * dy;
    }
}
