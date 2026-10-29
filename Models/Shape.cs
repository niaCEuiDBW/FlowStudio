using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Text.Json.Serialization;
using FlowStudio.Serialization;

namespace FlowStudio.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(RectangleShape), "rectangle")]
[JsonDerivedType(typeof(EllipseShape), "ellipse")]
[JsonDerivedType(typeof(RhombusShape), "rhombus")]
[JsonDerivedType(typeof(TextBlockShape), "text-block")]
[JsonDerivedType(typeof(TerminatorShape), "terminator")]
[JsonDerivedType(typeof(ConnectionLineShape), "connection-line")]
public abstract class Shape : IDrawable, ICloneable
{
    private int _x;
    private int _y;
    private int _width = 140;
    private int _height = 80;
    private float _strokeWidth = 2f;
    private float _fontSize = 10f;
    private string _text = string.Empty;
    private Color _strokeColor = Color.Black;
    private Color _fillColor = Color.LightBlue;
    private Color _textColor = Color.Black;

    [Browsable(false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Category("Положение")]
    [Description("Координата X левого верхнего угла.")]
    public int X
    {
        get => _x;
        set => _x = Math.Clamp(value, -100000, 100000);
    }

    [Category("Положение")]
    [Description("Координата Y левого верхнего угла.")]
    public int Y
    {
        get => _y;
        set => _y = Math.Clamp(value, -100000, 100000);
    }

    [Category("Размеры")]
    public int Width
    {
        get => _width;
        set => _width = Math.Clamp(value, 20, 10000);
    }

    [Category("Размеры")]
    public int Height
    {
        get => _height;
        set => _height = Math.Clamp(value, 20, 10000);
    }

    [Category("Внешний вид")]
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color StrokeColor
    {
        get => _strokeColor;
        set => _strokeColor = value;
    }

    [Category("Внешний вид")]
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color FillColor
    {
        get => _fillColor;
        set => _fillColor = value;
    }

    [Category("Внешний вид")]
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color TextColor
    {
        get => _textColor;
        set => _textColor = value;
    }

    [Category("Внешний вид")]
    public float StrokeWidth
    {
        get => _strokeWidth;
        set => _strokeWidth = float.IsFinite(value) ? Math.Clamp(value, 1f, 20f) : 2f;
    }

    [Category("Текст")]
    public string Text
    {
        get => _text;
        set
        {
            string normalized = value ?? string.Empty;
            _text = normalized.Length > 500 ? normalized[..500] : normalized;
        }
    }

    [Category("Текст")]
    public float FontSize
    {
        get => _fontSize;
        set => _fontSize = float.IsFinite(value) ? Math.Clamp(value, 8f, 48f) : 10f;
    }

    [JsonIgnore]
    [Browsable(false)]
    public bool IsSelected { get; set; }

    [JsonIgnore]
    [Browsable(false)]
    public Rectangle Bounds => new(X, Y, Width, Height);

    [JsonIgnore]
    [Browsable(false)]
    public Point Center => new(X + Width / 2, Y + Height / 2);

    [JsonIgnore]
    [Browsable(false)]
    public virtual int DrawLayer => 1;

    [JsonIgnore]
    [Browsable(false)]
    public virtual bool CanMove => true;

    [JsonIgnore]
    [Browsable(false)]
    public virtual bool CanResize => true;

    [JsonIgnore]
    [Browsable(false)]
    public virtual bool CanConnect => true;

    public abstract void Draw(Graphics graphics);

    public abstract bool ContainsPoint(Point point);

    public virtual void Move(int deltaX, int deltaY)
    {
        X += deltaX;
        Y += deltaY;
    }

    public virtual void UpdateConnections(IReadOnlyList<Shape> shapes)
    {
    }

    public virtual bool References(Guid id) => false;

    public virtual Point GetConnectionPoint(Point toward)
    {
        float centerX = X + Width / 2f;
        float centerY = Y + Height / 2f;
        float deltaX = toward.X - centerX;
        float deltaY = toward.Y - centerY;

        if (Math.Abs(deltaX) < 0.001f && Math.Abs(deltaY) < 0.001f)
        {
            return Center;
        }

        float scaleX = Math.Abs(deltaX) < 0.001f ? float.PositiveInfinity : Width / 2f / Math.Abs(deltaX);
        float scaleY = Math.Abs(deltaY) < 0.001f ? float.PositiveInfinity : Height / 2f / Math.Abs(deltaY);
        float scale = Math.Min(scaleX, scaleY);

        return new Point((int)Math.Round(centerX + deltaX * scale), (int)Math.Round(centerY + deltaY * scale));
    }

    public virtual ResizeHandle GetResizeHandleAt(Point point)
    {
        if (!CanResize)
        {
            return ResizeHandle.None;
        }

        foreach ((ResizeHandle handle, Point position) in GetResizeHandles())
        {
            if (new Rectangle(position.X - 6, position.Y - 6, 12, 12).Contains(point))
            {
                return handle;
            }
        }

        return ResizeHandle.None;
    }

    public virtual void Resize(ResizeHandle handle, Rectangle original, Point point)
    {
        if (!CanResize || handle == ResizeHandle.None)
        {
            return;
        }

        const int minimum = 20;
        int left = original.Left;
        int top = original.Top;
        int right = original.Right;
        int bottom = original.Bottom;

        switch (handle)
        {
            case ResizeHandle.NorthWest:
                left = Math.Min(point.X, right - minimum);
                top = Math.Min(point.Y, bottom - minimum);
                break;
            case ResizeHandle.North:
                top = Math.Min(point.Y, bottom - minimum);
                break;
            case ResizeHandle.NorthEast:
                right = Math.Max(point.X, left + minimum);
                top = Math.Min(point.Y, bottom - minimum);
                break;
            case ResizeHandle.East:
                right = Math.Max(point.X, left + minimum);
                break;
            case ResizeHandle.SouthEast:
                right = Math.Max(point.X, left + minimum);
                bottom = Math.Max(point.Y, top + minimum);
                break;
            case ResizeHandle.South:
                bottom = Math.Max(point.Y, top + minimum);
                break;
            case ResizeHandle.SouthWest:
                left = Math.Min(point.X, right - minimum);
                bottom = Math.Max(point.Y, top + minimum);
                break;
            case ResizeHandle.West:
                left = Math.Min(point.X, right - minimum);
                break;
        }

        X = left;
        Y = top;
        Width = right - left;
        Height = bottom - top;
    }

    protected void DrawSelectionFrame(Graphics graphics)
    {
        if (!IsSelected)
        {
            return;
        }

        using var framePen = new Pen(SystemColors.Highlight, 1.5f) { DashStyle = DashStyle.Dash };
        using var handlePen = new Pen(SystemColors.Highlight, 1.5f);
        using var handleBrush = new SolidBrush(Color.White);
        graphics.DrawRectangle(framePen, X - 4, Y - 4, Width + 8, Height + 8);

        foreach (var handle in GetResizeHandles())
        {
            Point position = handle.Point;
            Rectangle marker = new(position.X - 4, position.Y - 4, 8, 8);
            graphics.FillRectangle(handleBrush, marker);
            graphics.DrawRectangle(handlePen, marker);
        }
    }

    protected void DrawCenteredText(Graphics graphics, RectangleF area)
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            return;
        }

        using var font = new Font(FontFamily.GenericSansSerif, FontSize, FontStyle.Regular, GraphicsUnit.Point);
        using var brush = new SolidBrush(TextColor);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisWord,
            FormatFlags = StringFormatFlags.LineLimit
        };

        graphics.DrawString(Text, font, brush, area, format);
    }

    protected virtual IEnumerable<(ResizeHandle Handle, Point Point)> GetResizeHandles()
    {
        yield return (ResizeHandle.NorthWest, new Point(X, Y));
        yield return (ResizeHandle.North, new Point(X + Width / 2, Y));
        yield return (ResizeHandle.NorthEast, new Point(X + Width, Y));
        yield return (ResizeHandle.East, new Point(X + Width, Y + Height / 2));
        yield return (ResizeHandle.SouthEast, new Point(X + Width, Y + Height));
        yield return (ResizeHandle.South, new Point(X + Width / 2, Y + Height));
        yield return (ResizeHandle.SouthWest, new Point(X, Y + Height));
        yield return (ResizeHandle.West, new Point(X, Y + Height / 2));
    }

    public virtual Shape Clone()
    {
        var clone = (Shape)MemberwiseClone();
        clone.Id = Guid.NewGuid();
        clone.IsSelected = false;
        return clone;
    }

    object ICloneable.Clone() => Clone();
}
