using System.Drawing.Drawing2D;

namespace KeyEventLogger;

internal sealed class KeyWindow : Form
{
    private readonly CaptureSession session = new();
    private readonly Button start = Ui.Button("Başlat", true);
    private readonly Button stop = Ui.Button("Durdur");
    private readonly Button clear = Ui.Button("Temizle");
    private readonly Button save = Ui.Button("TXT kaydet");
    private readonly CheckBox releases = new() { Text = "Bırakma olayını da göster", AutoSize = true, Margin = new(8, 10, 0, 0) };
    private readonly Label state = Ui.Label("Kayıt kapalı", 11, true, Ui.Accent);
    private readonly Label count = Ui.Label("0 basma", 12, true);
    private readonly Label hint = Ui.Label("Kayıt başladığında tuş yakalama alanı odaklanır.", 10, false, Ui.Muted);
    private readonly KeyPad pad = new();
    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        EnableHeadersVisualStyles = false, BorderStyle = BorderStyle.None, ColumnHeadersHeight = 35,
        Margin = new(0, 8, 0, 4), AccessibleName = "Tuş olayları tablosu"
    };

    public KeyWindow()
    {
        Ui.Window(this, "Key Event Studio", 970, 685, 650, 535);
        grid.BackgroundColor = Ui.Surface;
        grid.GridColor = Ui.Border;
        grid.ColumnHeadersDefaultCellStyle = new() { BackColor = Ui.Border, ForeColor = Ui.Text, Font = new("Segoe UI", 10, FontStyle.Bold) };
        grid.DefaultCellStyle = new()
        {
            BackColor = Ui.Surface, ForeColor = Ui.Text, SelectionBackColor = Ui.Accent, SelectionForeColor = Ui.OnAccent,
            Padding = new(7, 0, 0, 0)
        };
        grid.AlternatingRowsDefaultCellStyle.BackColor = Ui.Background;
        grid.RowTemplate.Height = 31;
        foreach (var (text, width) in new[] { ("ZAMAN", 27), ("TUŞ", 21), ("OLAY", 21), ("DEĞİŞTİRİCİ", 31) })
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = text, FillWeight = width, MinimumWidth = 60, SortMode = DataGridViewColumnSortMode.NotSortable
            });
        count.Margin = new(10, 1, 0, 6);
        state.Margin = new(0, 1, 9, 6);
        var description = Ui.Label("Bu pencerede hangi tuşa ne zaman bastığınızı görün.", 11, false, Ui.Muted);
        description.Margin = new(0, 0, 0, 10);
        pad.Dock = DockStyle.Fill;
        pad.Margin = new(0, 8, 0, 0);
        Controls.Add(Ui.Stack(18,
            (Ui.Label("Key Event Studio", 27, true), 0),
            (description, 0),
            (Ui.Bar(start, stop, clear, save, releases), 0),
            (Ui.Bar(state, count), 0),
            (pad, 0), (grid, 100), (hint, 0)));

        start.Click += (_, _) => { session.Start(); pad.Focus(); hint.Text = "Bir tuşa basın. Basılı tutmak ikinci bir basma sayılmaz."; RefreshStatus(); };
        stop.Click += (_, _) => Stop("Kayıt durduruldu.");
        clear.Click += (_, _) => { session.Clear(); grid.Rows.Clear(); RefreshStatus(); hint.Text = "Olay tablosu temizlendi."; };
        save.Click += (_, _) => Save();
        releases.CheckedChanged += (_, _) => { if (session.Active) pad.Focus(); };
        pad.KeyDown += (_, e) =>
        {
            e.Handled = true;
            Append(session.Press(e.KeyCode, e.Modifiers));
        };
        pad.KeyUp += (_, e) => Append(session.Release(e.KeyCode, e.Modifiers, releases.Checked));
        pad.LostFocus += (_, _) => { session.ResetHeld(); RefreshStatus(); };
        pad.GotFocus += (_, _) => RefreshStatus();
        Deactivate += (_, _) =>
        {
            if (session.Active) Stop("Pencere odağını kaybetti. Devam etmek için Başlat'a basın.");
        };
        RefreshStatus();
    }

    private void Append(KeyStroke? stroke)
    {
        if (stroke is null) return;
        if (grid.Rows.Count == CaptureSession.Capacity) grid.Rows.RemoveAt(0);
        int row = grid.Rows.Add(stroke.Time.ToString("HH:mm:ss.fff"), stroke.Key.ToString(), stroke.Action, stroke.ModifierText);
        if (grid.DisplayedRowCount(false) > 0 && row >= grid.DisplayedRowCount(false))
            grid.FirstDisplayedScrollingRowIndex = row;
        grid.ClearSelection();
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        start.Enabled = !session.Active;
        stop.Enabled = session.Active;
        save.Enabled = clear.Enabled = grid.Rows.Count > 0;
        state.Text = session.Active ? (pad.Focused ? "●  Kayıt açık" : "Kayıt hazır · Alanı seçin") : "○  Kayıt kapalı";
        count.Text = $"{session.Presses} basma";
        pad.UpdateState(session.Active, session.LastKey?.ToString() ?? "—");
    }

    private void Stop(string message)
    {
        session.Stop();
        hint.Text = message;
        RefreshStatus();
    }

    private void Save()
    {
        Stop("Kayıt TXT olarak dışa aktarılabilir.");
        using var dialog = new SaveFileDialog { Filter = "Metin dosyası|*.txt", FileName = "tus-olaylari.txt", DefaultExt = "txt" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { session.Save(dialog.FileName); hint.Text = $"{grid.Rows.Count} olay kaydedildi."; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { hint.Text = "Dosya yazılamadı. " + ex.Message; }
    }
}

internal sealed class KeyPad : Control
{
    private bool active;
    private string key = "—";
    private readonly Font heading = new("Segoe UI", 11, FontStyle.Bold);
    private readonly Font text = new("Segoe UI", 10);
    private readonly Font keyFont = new("Consolas", 15, FontStyle.Bold);

    public KeyPad()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
        Height = 80;
        MinimumSize = new(0, 80);
        BackColor = Ui.Surface;
        Cursor = Cursors.IBeam;
        AccessibleName = "Tuş yakalama alanı";
    }
    public void UpdateState(bool recording, string lastKey)
    {
        active = recording;
        key = lastKey;
        Invalidate();
    }
    protected override bool IsInputKey(Keys data) =>
        (data & Keys.KeyCode) != Keys.Tab && (data & Keys.Alt) == 0 || base.IsInputKey(data);
    protected override void OnMouseDown(MouseEventArgs e) { Focus(); base.OnMouseDown(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var outline = new Pen(active && Focused ? Ui.Accent : Ui.Muted, active && Focused ? 2 : 1);
        using var textColor = new SolidBrush(Ui.Text);
        using var subtle = new SolidBrush(Ui.Muted);
        using var cap = new SolidBrush(Ui.Tint);
        using var capInk = new SolidBrush(Ui.OnAccent);
        g.DrawRectangle(outline, 1, 1, Width - 3, Height - 3);
        g.DrawString("TUŞ YAKALAMA ALANI", heading, textColor, 14, 13);
        g.DrawString(active ? "Yazmak için buraya odaklanın" : "Kayda başlamak için Başlat’ı seçin", text, subtle, 14, 41);
        var keySize = g.MeasureString(key, keyFont);
        var width = Math.Max(76, keySize.Width + 23);
        var rect = new RectangleF(Width - width - 15, 17, width, 44);
        g.FillRectangle(cap, rect);
        g.DrawString(key, keyFont, capInk, rect.X + (rect.Width - keySize.Width) / 2, 26);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { heading.Dispose(); text.Dispose(); keyFont.Dispose(); }
        base.Dispose(disposing);
    }
}
