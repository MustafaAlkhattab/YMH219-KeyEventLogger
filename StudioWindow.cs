using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace KeyEventLogger;

internal sealed class KeyWindow : Form
{
    private readonly CaptureSession session = new();
    private readonly Button start = Ui.Button("Başlat", true);
    private readonly Button stop = Ui.Button("Durdur");
    private readonly Button clear = Ui.Button("Temizle");
    private readonly Button save = Ui.Button("TXT kaydet");
    private readonly CheckBox releases = new() { Text = "Bırakmayı da göster", AutoSize = true, Margin = new(6, 10, 0, 0) };
    private readonly Label state = Ui.Label("○  Kapalı", 15, true);
    private readonly Label count = Ui.Label("0", 19, true, Ui.Accent);
    private readonly Label hint = Ui.Label("Başlat'a basıp bu pencerede tuşları deneyin.", 10, false, Ui.Muted);
    private readonly TextBox sample = new()
    {
        Dock = DockStyle.Fill, Text = "", PlaceholderText = "Örneğin A, B veya J yazın…",
        AccessibleName = "Deneme metni", MaxLength = 160, BorderStyle = BorderStyle.FixedSingle
    };
    private readonly KeyPulse recent = new();
    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, EnableHeadersVisualStyles = false,
        BorderStyle = BorderStyle.None, ColumnHeadersHeight = 37, Margin = Padding.Empty,
        AccessibleName = "Tuş olayları tablosu"
    };

    public KeyWindow()
    {
        Ui.Window(this, "Key Event Studio", 980, 690, 670, 550);
        KeyPreview = true;
        Ui.Input(sample, "Deneme metni");
        sample.Font = new("Segoe UI", 12);
        sample.BackColor = Ui.Background;
        sample.Margin = new(0, 4, 0, 0);

        var summary = new TableLayoutPanel
        {
            ColumnCount = 3, RowCount = 1, Dock = DockStyle.Fill, Margin = new(0, 11, 0, 9), Height = 90
        };
        for (int i = 0; i < 3; i++) summary.ColumnStyles.Add(new(SizeType.Percent, 100f / 3));
        summary.Controls.Add(Card("KAYIT DURUMU", state, 0), 0, 0);
        summary.Controls.Add(Card("TUŞ BASMA SAYISI", count, 1), 1, 0);
        summary.Controls.Add(Card("SON TUŞ", recent, 2), 2, 0);

        var inputCard = Ui.Stack(12, (Ui.Label("Deneme alanı", 13, true), 0), (sample, 0),
            (Ui.Label("Tuş adları tabloya eklenir. Bu alandaki metin TXT dosyasına yazılmaz.", 9, false, Ui.Muted), 0));
        inputCard.BackColor = Ui.Surface;
        inputCard.Margin = new(0, 0, 0, 9);

        grid.BackgroundColor = Ui.Surface;
        grid.GridColor = Ui.Border;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersDefaultCellStyle = new()
        { BackColor = Ui.Border, ForeColor = Ui.Text, Font = new("Segoe UI", 9.5f, FontStyle.Bold), Padding = new(8, 0, 0, 0) };
        grid.DefaultCellStyle = new()
        { BackColor = Ui.Surface, ForeColor = Ui.Text, SelectionBackColor = Ui.Accent, SelectionForeColor = Ui.OnAccent, Padding = new(8, 0, 0, 0) };
        grid.AlternatingRowsDefaultCellStyle.BackColor = Ui.Background;
        grid.RowTemplate.Height = 31;
        foreach (var (title, width) in new[] { ("ZAMAN", 27), ("TUŞ", 20), ("OLAY", 22), ("DEĞİŞTİRİCİ", 31) })
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = title, FillWeight = width, MinimumWidth = 76, SortMode = DataGridViewColumnSortMode.NotSortable
            });
        var logCard = Ui.Stack(12, (Ui.Label("Olay günlüğü", 14, true), 0), (grid, 100));
        logCard.BackColor = Ui.Surface;
        logCard.Margin = new(0, 5, 0, 8);
        hint.Margin = new(1, 2, 0, 1);
        Controls.Add(Ui.Stack(18,
            (Ui.Label("Key Event Studio", 28, true), 0),
            (Ui.Label("Bu penceredeki klavye olaylarını inceleyin.", 10, false, Ui.Muted), 0),
            (summary, 0), (inputCard, 0), (Ui.Bar(start, stop, clear, save, releases), 0),
            (logCard, 100), (hint, 0)));

        start.Click += (_, _) =>
        {
            session.Start();
            hint.Text = "Kayıt açık. Basılı tutmak yeni satır eklemez.";
            RefreshStatus();
            BeginInvoke(() => { if (session.Active && ContainsFocus) sample.Focus(); });
        };
        stop.Click += (_, _) => Stop("Kayıt durduruldu.");
        clear.Click += (_, _) =>
        {
            session.Clear();
            grid.Rows.Clear();
            sample.Clear();
            recent.ShowKey("—", animate: false);
            hint.Text = "Günlük ve deneme metni temizlendi.";
            RefreshStatus();
            if (session.Active) sample.Focus();
        };
        releases.CheckedChanged += (_, _) => { if (session.Active) sample.Focus(); };
        sample.TextChanged += (_, _) => RefreshStatus();
        save.Click += (_, _) => Save();
        KeyDown += (_, e) =>
        {
            if (!session.Active || e.Alt || e.KeyCode == Keys.Tab) return;
            if (!sample.Focused) e.Handled = true;
            Append(session.Press(e.KeyCode, e.Modifiers));
        };
        KeyUp += (_, e) => Append(session.Release(e.KeyCode, e.Modifiers, releases.Checked));
        Deactivate += (_, _) =>
        {
            if (session.Active) Stop("Pencere odağı değişti. Başlat ile kayda devam edin.");
        };
        RefreshStatus();
    }

    private static Control Card(string title, Control value, int column)
    {
        var card = Ui.Stack(12, (Ui.Label(title, 9, true, Ui.Muted), 0), (value, 0));
        card.BackColor = Ui.Surface;
        card.Margin = new(column == 0 ? 0 : 5, 0, column == 2 ? 0 : 5, 0);
        return card;
    }

    private void Append(KeyStroke? stroke)
    {
        if (stroke is null) return;
        if (grid.Rows.Count == CaptureSession.Capacity) grid.Rows.RemoveAt(0);
        int row = grid.Rows.Add(stroke.Time.ToString("HH:mm:ss.fff"), stroke.Key.ToString(), stroke.Action, stroke.ModifierText);
        if (grid.DisplayedRowCount(false) > 0 && row >= grid.DisplayedRowCount(false))
            grid.FirstDisplayedScrollingRowIndex = row;
        grid.ClearSelection();
        if (!stroke.Released) recent.ShowKey(stroke.Key.ToString());
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        start.Enabled = !session.Active;
        stop.Enabled = session.Active;
        clear.Enabled = grid.Rows.Count > 0 || sample.TextLength > 0;
        save.Enabled = grid.Rows.Count > 0;
        state.Text = session.Active ? "●  Kayıt açık" : "○  Kapalı";
        state.ForeColor = session.Active ? Ui.Accent : Ui.Text;
        count.Text = session.Presses.ToString();
    }

    private void Stop(string message)
    {
        session.Stop();
        hint.Text = message;
        RefreshStatus();
    }

    private void Save()
    {
        Stop("Dışa aktarılacak dosyayı seçin.");
        using var dialog = new SaveFileDialog { Filter = "Metin dosyası|*.txt", FileName = "tus-olaylari.txt", DefaultExt = "txt" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { session.Save(dialog.FileName); hint.Text = $"{grid.Rows.Count} olay kaydedildi."; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { hint.Text = "Dosya yazılamadı. " + ex.Message; }
    }
}

internal sealed class KeyPulse : Control
{
    private readonly Font face = new("Consolas", 11f, FontStyle.Bold);
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly Stopwatch clock = new();
    private string key = "—";

    public KeyPulse()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Dock = DockStyle.Fill;
        BackColor = Ui.Surface;
        Height = 33;
        Margin = Padding.Empty;
        timer.Tick += (_, _) =>
        {
            if (clock.ElapsedMilliseconds >= 250) { timer.Stop(); clock.Reset(); }
            Invalidate();
        };
    }

    public void ShowKey(string value, bool animate = true)
    {
        key = value;
        AccessibleName = "Son tuş: " + value;
        if (animate && SystemInformation.IsMenuAnimationEnabled) { clock.Restart(); timer.Start(); }
        else { timer.Stop(); clock.Reset(); }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int width = Math.Min(ClientSize.Width - 3, Math.Max(55, TextRenderer.MeasureText(key, face).Width + 22));
        if (width < 6 || Height < 8) return;
        float pulse = clock.IsRunning ? 1f - Math.Clamp(clock.ElapsedMilliseconds / 250f, 0, 1) : 0;
        int y = 2 + (int)(pulse * 2);
        using var plate = new SolidBrush(pulse > .44f ? Ui.Accent : Ui.Tint);
        var bounds = new Rectangle(0, y, width, Height - y);
        g.FillRoundedRectangle(plate, bounds, new Size(6, 6));
        TextRenderer.DrawText(g, key, face, bounds, pulse > .44f ? Ui.OnAccent : Ui.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { face.Dispose(); timer.Dispose(); }
        base.Dispose(disposing);
    }
}
