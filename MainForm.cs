using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.Json;
using FlowStudio.Controls;
using FlowStudio.Enums;
using FlowStudio.Models;

namespace FlowStudio;

public sealed class MainForm : Form
{
    private readonly List<Shape> _shapes = new();
    private readonly CanvasPanel _canvas = new();
    private readonly PropertyGrid _propertyGrid = new();
    private readonly Dictionary<ToolMode, ToolStripButton> _modeButtons = new();
    private readonly ToolStripStatusLabel _modeLabel = new("Режим: Выбор");
    private readonly ToolStripStatusLabel _coordinatesLabel = new("X: 0, Y: 0");
    private readonly ToolStripStatusLabel _countLabel = new("Фигур: 0");

    private ToolMode _currentMode = ToolMode.Select;
    private Shape? _selectedShape;
    private Shape? _connectionStart;
    private Point _lastMousePoint;
    private Point _connectionEnd;
    private bool _isDragging;
    private bool _isResizing;
    private bool _isConnecting;
    private ResizeHandle _resizeHandle;
    private Rectangle _resizeBounds;
    private Color _strokeColor = Color.Black;
    private Color _fillColor = Color.LightBlue;
    private float _strokeWidth = 2;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public MainForm()
    {
        Text = "FlowStudio — редактор блок-схем";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1100, 700);
        MinimumSize = new Size(850, 550);
        KeyPreview = true;

        MenuStrip menu = CreateMenu();
        ToolStrip tools = CreateTools();
        StatusStrip status = CreateStatusBar();

        SplitContainer workArea = new()
        {
            Size = new Size(1100, 600),
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 800,
            Panel1MinSize = 400,
            Panel2MinSize = 200,
            FixedPanel = FixedPanel.Panel2
        };

        _canvas.Dock = DockStyle.Fill;
        _canvas.BackColor = Color.White;
        _canvas.Margin = new Padding(0);
        _canvas.Paint += Canvas_Paint;
        _canvas.MouseDown += Canvas_MouseDown;
        _canvas.MouseMove += Canvas_MouseMove;
        _canvas.MouseUp += Canvas_MouseUp;
        workArea.Panel1.Controls.Add(_canvas);

        _propertyGrid.Dock = DockStyle.Fill;
        _propertyGrid.HelpVisible = false;
        _propertyGrid.PropertySort = PropertySort.Categorized;
        _propertyGrid.PropertyValueChanged += (_, _) =>
        {
            UpdateConnections();
            _canvas.Invalidate();
        };
        workArea.Panel2.Controls.Add(_propertyGrid);

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(menu, 0, 0);
        layout.Controls.Add(tools, 0, 1);
        layout.Controls.Add(workArea, 0, 2);
        layout.Controls.Add(status, 0, 3);
        Controls.Add(layout);
        MainMenuStrip = menu;

        KeyDown += MainForm_KeyDown;
        SetMode(ToolMode.Select);
        UpdateStatus();
    }

    private MenuStrip CreateMenu()
    {
        MenuStrip menu = new() { Dock = DockStyle.Fill };

        ToolStripMenuItem fileMenu = new("Файл");
        fileMenu.DropDownItems.Add("Новая схема", null, (_, _) => NewDocument());
        fileMenu.DropDownItems.Add("Открыть JSON...", null, (_, _) => LoadJson());
        fileMenu.DropDownItems.Add("Сохранить JSON...", null, (_, _) => SaveJson());
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("Экспорт в PNG...", null, (_, _) => ExportImage(ImageFormat.Png, "png"));
        fileMenu.DropDownItems.Add("Экспорт в JPG...", null, (_, _) => ExportImage(ImageFormat.Jpeg, "jpg"));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("Выход", null, (_, _) => Close());

        ToolStripMenuItem editMenu = new("Правка");
        editMenu.DropDownItems.Add("Удалить фигуру", null, (_, _) => DeleteSelected());
        editMenu.DropDownItems.Add("Дублировать фигуру", null, (_, _) => DuplicateSelected());
        editMenu.DropDownItems.Add("Очистить холст", null, (_, _) => ClearCanvas());

        ToolStripMenuItem helpMenu = new("Справка");
        helpMenu.DropDownItems.Add("Как пользоваться", null, (_, _) =>
            MessageBox.Show(this,
                "Выберите фигуру на панели инструментов и щёлкните по холсту.\n" +
                "Чтобы переместить фигуру, перетащите её мышью.\n" +
                "Размер меняется за квадратные маркеры после выделения.\n" +
                "Для стрелки выберите «Связь», затем перетащите от одного блока к другому.",
                "Справка", MessageBoxButtons.OK, MessageBoxIcon.Information));

        menu.Items.Add(fileMenu);
        menu.Items.Add(editMenu);
        menu.Items.Add(helpMenu);
        return menu;
    }

    private ToolStrip CreateTools()
    {
        ToolStrip tools = new()
        {
            Dock = DockStyle.Fill,
            GripStyle = ToolStripGripStyle.Hidden,
            ImageScalingSize = new Size(16, 16)
        };

        AddModeButton(tools, "Выбор", ToolMode.Select);
        AddModeButton(tools, "Прямоугольник", ToolMode.Rectangle);
        AddModeButton(tools, "Эллипс", ToolMode.Ellipse);
        AddModeButton(tools, "Ромб", ToolMode.Rhombus);
        AddModeButton(tools, "Текстовый блок", ToolMode.TextBlock);
        AddModeButton(tools, "Начало/конец", ToolMode.Terminator);
        AddModeButton(tools, "Связь", ToolMode.Line);

        tools.Items.Add(new ToolStripSeparator());
        tools.Items.Add(new ToolStripButton("Контур", null, (_, _) => ChooseColor(false)));
        tools.Items.Add(new ToolStripButton("Заливка", null, (_, _) => ChooseColor(true)));

        ToolStripComboBox thickness = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 70,
            ToolTipText = "Толщина линии"
        };
        thickness.Items.AddRange(new object[] { "1 px", "2 px", "3 px", "5 px", "8 px" });
        thickness.SelectedIndex = 1;
        thickness.SelectedIndexChanged += (_, _) =>
        {
            _strokeWidth = thickness.SelectedIndex switch
            {
                0 => 1,
                1 => 2,
                2 => 3,
                3 => 5,
                _ => 8
            };

            if (_selectedShape is not null)
            {
                _selectedShape.StrokeWidth = _strokeWidth;
                _propertyGrid.Refresh();
                _canvas.Invalidate();
            }
        };
        tools.Items.Add(new ToolStripLabel("Толщина:"));
        tools.Items.Add(thickness);
        tools.Items.Add(new ToolStripSeparator());
        tools.Items.Add(new ToolStripButton("Удалить", null, (_, _) => DeleteSelected()));
        tools.Items.Add(new ToolStripButton("Очистить", null, (_, _) => ClearCanvas()));
        return tools;
    }

    private void AddModeButton(ToolStrip tools, string text, ToolMode mode)
    {
        ToolStripButton button = new(text) { CheckOnClick = true };
        button.Click += (_, _) => SetMode(mode);
        _modeButtons.Add(mode, button);
        tools.Items.Add(button);
    }

    private StatusStrip CreateStatusBar()
    {
        StatusStrip status = new() { Dock = DockStyle.Fill, SizingGrip = true };
        status.Items.Add(_modeLabel);
        status.Items.Add(new ToolStripStatusLabel { Spring = true });
        status.Items.Add(_coordinatesLabel);
        status.Items.Add(new ToolStripStatusLabel("  "));
        status.Items.Add(_countLabel);
        return status;
    }

    private void SetMode(ToolMode mode)
    {
        _currentMode = mode;
        foreach (KeyValuePair<ToolMode, ToolStripButton> item in _modeButtons)
        {
            item.Value.Checked = item.Key == mode;
        }

        _isConnecting = false;
        _connectionStart = null;
        _modeLabel.Text = "Режим: " + ModeName(mode);
        _canvas.Cursor = mode == ToolMode.Line ? Cursors.Cross : Cursors.Default;
        _canvas.Invalidate();
    }

    private static string ModeName(ToolMode mode)
    {
        return mode switch
        {
            ToolMode.Select => "Выбор",
            ToolMode.Rectangle => "Прямоугольник",
            ToolMode.Ellipse => "Эллипс",
            ToolMode.Rhombus => "Ромб",
            ToolMode.TextBlock => "Текстовый блок",
            ToolMode.Terminator => "Начало/конец",
            ToolMode.Line => "Связь",
            _ => "Выбор"
        };
    }

    private void ChooseColor(bool fill)
    {
        using ColorDialog dialog = new()
        {
            FullOpen = true,
            Color = fill ? _fillColor : _strokeColor
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (fill)
        {
            _fillColor = dialog.Color;
            if (_selectedShape is not null)
            {
                _selectedShape.FillColor = dialog.Color;
            }
        }
        else
        {
            _strokeColor = dialog.Color;
            if (_selectedShape is not null)
            {
                _selectedShape.StrokeColor = dialog.Color;
            }
        }

        _propertyGrid.Refresh();
        _canvas.Invalidate();
    }

    private Shape? CreateShape(Point point)
    {
        Shape? shape = _currentMode switch
        {
            ToolMode.Rectangle => new RectangleShape { Text = "Действие" },
            ToolMode.Ellipse => new EllipseShape { Text = "Событие" },
            ToolMode.Rhombus => new RhombusShape { Text = "Условие?" },
            ToolMode.TextBlock => new TextBlockShape { Text = "Текст" },
            ToolMode.Terminator => new TerminatorShape { Text = "Начало/конец" },
            _ => null
        };

        if (shape is null)
        {
            return null;
        }

        shape.X = point.X - shape.Width / 2;
        shape.Y = point.Y - shape.Height / 2;
        shape.StrokeColor = _strokeColor;
        shape.FillColor = _fillColor;
        shape.StrokeWidth = _strokeWidth;
        return shape;
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Color.White);
        UpdateConnections();

        foreach (Shape shape in _shapes.OrderBy(item => item.DrawLayer))
        {
            shape.Draw(e.Graphics);
        }

        if (_isConnecting && _connectionStart is not null)
        {
            using AdjustableArrowCap arrow = new(5, 7, true);
            using Pen pen = new(_strokeColor, _strokeWidth) { CustomEndCap = arrow };
            Point start = _connectionStart.GetConnectionPoint(_connectionEnd);
            e.Graphics.DrawLine(pen, start, _connectionEnd);
        }
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        _canvas.Focus();
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (_currentMode == ToolMode.Line)
        {
            _connectionStart = FindConnectableAt(e.Location);
            if (_connectionStart is not null)
            {
                _connectionEnd = e.Location;
                _isConnecting = true;
                _canvas.Invalidate();
            }
            return;
        }

        if (_currentMode != ToolMode.Select)
        {
            Shape? shape = CreateShape(e.Location);
            if (shape is not null)
            {
                _shapes.Add(shape);
                SelectShape(shape);
                UpdateStatus();
                _canvas.Invalidate();
            }
            return;
        }

        if (_selectedShape is not null)
        {
            ResizeHandle handle = _selectedShape.GetResizeHandleAt(e.Location);
            if (handle != ResizeHandle.None)
            {
                _resizeHandle = handle;
                _resizeBounds = _selectedShape.Bounds;
                _isResizing = true;
                return;
            }
        }

        Shape? selected = FindShapeAt(e.Location);
        SelectShape(selected);
        if (selected is not null && selected.CanMove)
        {
            _lastMousePoint = e.Location;
            _isDragging = true;
        }

        UpdateStatus();
        _canvas.Invalidate();
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        _coordinatesLabel.Text = $"X: {e.X}, Y: {e.Y}";

        if (_isConnecting && _connectionStart is not null)
        {
            _connectionEnd = e.Location;
            _canvas.Invalidate();
            return;
        }

        if (_isResizing && _selectedShape is not null)
        {
            _selectedShape.Resize(_resizeHandle, _resizeBounds, e.Location);
            UpdateConnections();
            _propertyGrid.Refresh();
            _canvas.Invalidate();
            return;
        }

        if (_isDragging && _selectedShape is not null)
        {
            _selectedShape.Move(e.X - _lastMousePoint.X, e.Y - _lastMousePoint.Y);
            _lastMousePoint = e.Location;
            UpdateConnections();
            _propertyGrid.Refresh();
            _canvas.Invalidate();
            return;
        }

        ResizeHandle handle = _selectedShape?.GetResizeHandleAt(e.Location) ?? ResizeHandle.None;
        _canvas.Cursor = CursorForHandle(handle);
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        if (_isConnecting && _connectionStart is not null)
        {
            Shape? target = FindConnectableAt(e.Location);
            if (target is not null && target.Id != _connectionStart.Id)
            {
                ConnectionLineShape line = new()
                {
                    SourceShapeId = _connectionStart.Id,
                    TargetShapeId = target.Id,
                    StrokeColor = _strokeColor,
                    StrokeWidth = _strokeWidth
                };
                _shapes.Add(line);
                UpdateConnections();
            }
        }

        _isConnecting = false;
        _connectionStart = null;
        _isDragging = false;
        _isResizing = false;
        _resizeHandle = ResizeHandle.None;
        UpdateStatus();
        _canvas.Invalidate();
    }

    private Cursor CursorForHandle(ResizeHandle handle)
    {
        return handle switch
        {
            ResizeHandle.NorthWest or ResizeHandle.SouthEast => Cursors.SizeNWSE,
            ResizeHandle.NorthEast or ResizeHandle.SouthWest => Cursors.SizeNESW,
            ResizeHandle.North or ResizeHandle.South => Cursors.SizeNS,
            ResizeHandle.East or ResizeHandle.West => Cursors.SizeWE,
            _ => _currentMode == ToolMode.Line ? Cursors.Cross : Cursors.Default
        };
    }

    private Shape? FindShapeAt(Point point)
    {
        return _shapes
            .OrderByDescending(shape => shape.DrawLayer)
            .ThenByDescending(shape => _shapes.IndexOf(shape))
            .FirstOrDefault(shape => shape.ContainsPoint(point));
    }

    private Shape? FindConnectableAt(Point point)
    {
        return _shapes
            .Where(shape => shape.CanConnect)
            .OrderByDescending(shape => _shapes.IndexOf(shape))
            .FirstOrDefault(shape => shape.ContainsPoint(point));
    }

    private void SelectShape(Shape? shape)
    {
        foreach (Shape item in _shapes)
        {
            item.IsSelected = false;
        }

        _selectedShape = shape;
        if (_selectedShape is not null)
        {
            _selectedShape.IsSelected = true;
        }

        _propertyGrid.SelectedObject = _selectedShape;
        _propertyGrid.Refresh();
    }

    private void UpdateConnections()
    {
        foreach (Shape shape in _shapes)
        {
            shape.UpdateConnections(_shapes);
        }
    }

    private void DeleteSelected()
    {
        if (_selectedShape is null)
        {
            return;
        }

        Guid id = _selectedShape.Id;
        _shapes.Remove(_selectedShape);
        _shapes.RemoveAll(shape => shape.References(id));
        SelectShape(null);
        UpdateConnections();
        UpdateStatus();
        _canvas.Invalidate();
    }

    private void DuplicateSelected()
    {
        if (_selectedShape is null)
        {
            return;
        }

        Shape copy = _selectedShape.Clone();
        copy.Move(20, 20);
        _shapes.Add(copy);
        SelectShape(copy);
        UpdateConnections();
        UpdateStatus();
        _canvas.Invalidate();
    }

    private void ClearCanvas()
    {
        if (_shapes.Count == 0)
        {
            return;
        }

        DialogResult result = MessageBox.Show(this, "Удалить все фигуры?", "Очистить холст", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes)
        {
            return;
        }

        _shapes.Clear();
        SelectShape(null);
        UpdateStatus();
        _canvas.Invalidate();
    }

    private void NewDocument()
    {
        if (_shapes.Count > 0)
        {
            DialogResult result = MessageBox.Show(this, "Создать новую схему? Текущие элементы будут удалены.", "Новая схема", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                return;
            }
        }

        _shapes.Clear();
        SelectShape(null);
        SetMode(ToolMode.Select);
        UpdateStatus();
        _canvas.Invalidate();
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete)
        {
            DeleteSelected();
        }
        else if (e.Control && e.KeyCode == Keys.D)
        {
            DuplicateSelected();
        }
        else if (e.KeyCode == Keys.Escape)
        {
            _isConnecting = false;
            _connectionStart = null;
            _isDragging = false;
            _isResizing = false;
            _canvas.Invalidate();
        }
    }

    private void SaveJson()
    {
        using SaveFileDialog dialog = new()
        {
            Filter = "JSON схема (*.json)|*.json",
            DefaultExt = "json",
            AddExtension = true,
            FileName = "schema.json"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            string json = JsonSerializer.Serialize(_shapes, JsonOptions);
            File.WriteAllText(dialog.FileName, json);
            MessageBox.Show(this, "Схема сохранена.", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadJson()
    {
        using OpenFileDialog dialog = new()
        {
            Filter = "JSON схема (*.json)|*.json",
            DefaultExt = "json",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(dialog.FileName);
            List<Shape>? loaded = JsonSerializer.Deserialize<List<Shape>>(json, JsonOptions);
            if (loaded is null)
            {
                throw new JsonException("В файле нет схемы.");
            }

            _shapes.Clear();
            _shapes.AddRange(loaded);
            foreach (Shape shape in _shapes)
            {
                shape.IsSelected = false;
            }

            SelectShape(null);
            UpdateConnections();
            UpdateStatus();
            _canvas.Invalidate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportImage(ImageFormat format, string extension)
    {
        using SaveFileDialog dialog = new()
        {
            Filter = $"Изображение (*.{extension})|*.{extension}",
            DefaultExt = extension,
            AddExtension = true,
            FileName = $"schema.{extension}"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        List<Shape> selected = _shapes.Where(shape => shape.IsSelected).ToList();
        try
        {
            foreach (Shape shape in _shapes)
            {
                shape.IsSelected = false;
            }

            using Bitmap bitmap = new(Math.Max(1, _canvas.ClientSize.Width), Math.Max(1, _canvas.ClientSize.Height));
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.White);
                UpdateConnections();
                foreach (Shape shape in _shapes.OrderBy(item => item.DrawLayer))
                {
                    shape.Draw(graphics);
                }
            }

            bitmap.Save(dialog.FileName, format);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка экспорта", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            foreach (Shape shape in selected)
            {
                shape.IsSelected = true;
            }
            _canvas.Invalidate();
        }
    }

    private void UpdateStatus()
    {
        _modeLabel.Text = "Режим: " + ModeName(_currentMode);
        _countLabel.Text = "Фигур: " + _shapes.Count;
    }
}
