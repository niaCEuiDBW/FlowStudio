namespace FlowStudio.Controls;

public sealed class CanvasPanel : Panel
{
    public CanvasPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        UpdateStyles();
        BackColor = Color.White;
        TabStop = true;
    }
}
