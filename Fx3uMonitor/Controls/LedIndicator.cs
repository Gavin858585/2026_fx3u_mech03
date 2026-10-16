using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Fx3uMonitor.Controls;

/// <summary>
/// 工控級高質感 LED 狀態指示燈控制項
/// </summary>
[DefaultEvent(nameof(Click))]
public class LedIndicator : Control
{
    private bool _isOn = false;
    private Color _onColor = Color.FromArgb(0, 230, 118);       // 亮綠色
    private Color _offColor = Color.FromArgb(64, 75, 85);       // 暗灰藍
    private Color _glowColor = Color.FromArgb(80, 0, 230, 118); // 光暈顏色
    private int _ledSize = 22;

    public LedIndicator()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.Transparent;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        ForeColor = Color.FromArgb(220, 225, 230);
        Size = new Size(70, 30);
    }

    [Category("LED 屬性")]
    [Description("指示燈是否點亮")]
    [DefaultValue(false)]
    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (_isOn != value)
            {
                _isOn = value;
                Invalidate();
            }
        }
    }

    [Category("LED 屬性")]
    [Description("點亮時之 LED 主色調")]
    public Color OnColor
    {
        get => _onColor;
        set
        {
            _onColor = value;
            _glowColor = Color.FromArgb(80, value.R, value.G, value.B);
            Invalidate();
        }
    }

    [Category("LED 屬性")]
    [Description("熄滅時之 LED 顏色")]
    public Color OffColor
    {
        get => _offColor;
        set
        {
            _offColor = value;
            Invalidate();
        }
    }

    [Category("LED 屬性")]
    [Description("LED 圓形直徑大小")]
    [DefaultValue(22)]
    public int LedSize
    {
        get => _ledSize;
        set
        {
            _ledSize = Math.Max(10, Math.Min(value, Math.Min(Width, Height)));
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        // 計算 LED 位置 (垂直居中靠左)
        int ledY = (Height - _ledSize) / 2;
        int ledX = 3;
        var ledRect = new Rectangle(ledX, ledY, _ledSize, _ledSize);

        // 1. 繪製外圈陰影 / 光暈 (若點亮時)
        if (_isOn)
        {
            var glowRect = Rectangle.Inflate(ledRect, 3, 3);
            using var glowBrush = new PathGradientBrush(GetCirclePath(glowRect))
            {
                CenterColor = _glowColor,
                SurroundColors = new[] { Color.Transparent }
            };
            g.FillEllipse(glowBrush, glowRect);
        }

        // 2. 繪製外金屬邊框 / 底座
        using (var borderPen = new Pen(Color.FromArgb(90, 100, 115), 1.5f))
        {
            g.DrawEllipse(borderPen, ledRect);
        }

        // 3. 繪製 LED 核心本體 (漸層球體立體感)
        var coreRect = Rectangle.Inflate(ledRect, -1, -1);
        Color baseColor = _isOn ? _onColor : _offColor;
        Color centerHighlight = _isOn ? Color.FromArgb(255, 255, 255) : ControlPaint.Light(baseColor, 0.4f);

        using (var path = GetCirclePath(coreRect))
        using (var brush = new PathGradientBrush(path))
        {
            brush.CenterColor = centerHighlight;
            brush.CenterPoint = new PointF(coreRect.X + coreRect.Width * 0.35f, coreRect.Y + coreRect.Height * 0.35f);
            brush.SurroundColors = new[] { baseColor };
            g.FillPath(brush, path);
        }

        // 4. 繪製玻璃高光 (頂部反光橢圓)
        if (_isOn)
        {
            var highlightRect = new Rectangle(
                coreRect.X + coreRect.Width / 4,
                coreRect.Y + 2,
                coreRect.Width / 2,
                coreRect.Height / 3);

            using var hlBrush = new LinearGradientBrush(
                highlightRect,
                Color.FromArgb(180, 255, 255, 255),
                Color.FromArgb(20, 255, 255, 255),
                LinearGradientMode.Vertical);

            g.FillEllipse(hlBrush, highlightRect);
        }

        // 5. 繪製標籤文字 (Text)
        if (!string.IsNullOrEmpty(Text))
        {
            int textX = ledX + _ledSize + 6;
            var textRect = new Rectangle(textX, 0, Width - textX, Height);
            TextRenderer.DrawText(
                g,
                Text,
                Font,
                textRect,
                ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }

    private static GraphicsPath GetCirclePath(Rectangle rect)
    {
        var path = new GraphicsPath();
        path.AddEllipse(rect);
        return path;
    }
}
