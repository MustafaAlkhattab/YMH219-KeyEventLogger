namespace KeyEventLogger;

internal static class Ui
{
    public static readonly Color Background = ColorTranslator.FromHtml("#141C18");
    public static readonly Color Text = ColorTranslator.FromHtml("#EDF5E6");
    public static readonly Color Surface = ColorTranslator.FromHtml("#202C24");
    public static readonly Color Accent = ColorTranslator.FromHtml("#C6EA83");
    public static readonly Color OnAccent = ColorTranslator.FromHtml("#1A2813");
    public static readonly Color Muted = ColorTranslator.FromHtml("#AFC2AE");
    public static readonly Color Border = ColorTranslator.FromHtml("#364A3B");
    public static readonly Color Tint = ColorTranslator.FromHtml("#334638");

    public static void Window(Form form, string title, int width, int height, int minWidth, int minHeight)
    {
        form.Text = title;
        using (var resource = typeof(Ui).Assembly.GetManifestResourceStream("WindowIcon.ico"))
        {
            if (resource is not null)
            {
                var icon = new Icon(resource);
                form.Icon = icon;
                form.Disposed += (_, _) => icon.Dispose();
            }
        }
        form.Font = new("Segoe UI", 10f);
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.ClientSize = new(width, height);
        form.MinimumSize = new(minWidth, minHeight);
        form.StartPosition = FormStartPosition.CenterScreen;
        form.BackColor = Background;
        form.ForeColor = Text;
    }

    public static Label Label(string value, float size = 10, bool bold = false, Color? ink = null) => new()
    {
        Text = value, AutoSize = true, Font = new("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = ink ?? Text, Margin = new(0, 0, 0, 3), UseMnemonic = false
    };

    public static Button Button(string value, bool primary = false)
    {
        var button = new PaletteButton
        {
            Text = value, Height = 36, AutoSize = true, MinimumSize = new(68, 34),
            Padding = new(6, 0, 6, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            BackColor = primary ? Accent : Surface, ForeColor = primary ? OnAccent : Text,
            Margin = new(0, 2, 8, 5), UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = primary ? Accent : Border;
        return button;
    }

    private sealed class PaletteButton : Button
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Enabled) { base.OnPaint(e); return; }
            e.Graphics.Clear(Surface);
            using var border = new Pen(Border);
            e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    public static FlowLayoutPanel Bar(params Control[] controls)
    {
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = true,
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        flow.Controls.AddRange(controls);
        return flow;
    }

    public static TableLayoutPanel Stack(int padding, params (Control Control, float Percent)[] rows)
    {
        var stack = new TableLayoutPanel
        {
            ColumnCount = 1, Dock = DockStyle.Fill, Padding = new(padding), Margin = Padding.Empty,
            RowCount = rows.Length
        };
        stack.ColumnStyles.Add(new(SizeType.Percent, 100));
        for (int row = 0; row < rows.Length; row++)
        {
            var (control, percent) = rows[row];
            stack.RowStyles.Add(percent == 0 ? new(SizeType.AutoSize) : new(SizeType.Percent, percent));
            stack.Controls.Add(control, 0, row);
        }
        return stack;
    }

    public static void Input(Control control, string name)
    {
        control.AccessibleName = name;
        control.BackColor = Surface;
        control.ForeColor = Text;
    }
}
