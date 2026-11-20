namespace KeyEventLogger;

public partial class Form1 : Form
{
    private readonly KeyLogSession session = new();
    private readonly RichTextBox log = new() { Dock = DockStyle.Fill, ReadOnly = true, Font = new("Consolas", 11), BackColor = Color.White, ForeColor = Color.FromArgb(36, 30, 60) };
    private readonly TextBox input = new() { Dock = DockStyle.Fill, PlaceholderText = "Buraya deneme metni yazın..." };
    private readonly Label status = new() { AutoSize = true, Text = "DURDU · 0 olay", Font = new("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(76, 29, 149) };
    private readonly Button start = new() { Text = "Başlat", AutoSize = true, Height = 40 };
    private readonly Button stop = new() { Text = "Durdur", AutoSize = true, Enabled = false, Height = 40 };
    public Form1()
    {
        InitializeComponent(); Controls.Clear(); AutoScaleMode = AutoScaleMode.None;
        Text = "Key Event Studio | YMH219"; ClientSize = new(920, 650); MinimumSize = new(740, 500); StartPosition = FormStartPosition.CenterScreen;
        Font = new("Segoe UI", 10); BackColor = Color.FromArgb(250, 245, 255); KeyPreview = true;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new(24), RowCount = 7, ColumnCount = 1 };
        foreach (var h in new[] { 52, 50, 48, 34, 48 }) layout.RowStyles.Add(new(SizeType.Absolute, h));
        layout.RowStyles.Add(new(SizeType.Percent, 100)); layout.RowStyles.Add(new(SizeType.Absolute, 35));
        layout.Controls.Add(new Label { Text = "KEY EVENT STUDIO", AutoSize = true, Font = new("Segoe UI", 22, FontStyle.Bold), ForeColor = Color.FromArgb(76, 29, 149) }, 0, 0);
        layout.Controls.Add(new Label { Text = "Yalnızca bu pencere aktifken tuş olaylarını gösterir.\nKaydet düğmesine basılmadan diske yazılmaz.", AutoSize = true }, 0, 1);
        var tools = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var clear = new Button { Text = "Temizle", AutoSize = true, Height = 40 };
        var save = new Button { Text = "TXT kaydet", AutoSize = true, Height = 40 };
        tools.Controls.AddRange([start, stop, clear, save]); layout.Controls.Add(tools, 0, 2);
        layout.Controls.Add(status, 0, 3); layout.Controls.Add(input, 0, 4); layout.Controls.Add(log, 0, 5);
        layout.Controls.Add(new Label { Text = "Mustafa Alkhatta · 250541607 | KeyDown / KeyUp", AutoSize = true }, 0, 6); Controls.Add(layout);
        start.Click += (_, _) => { session.Start(); SetStatus(); input.Focus(); };
        stop.Click += (_, _) => { session.Stop(); SetStatus(); };
        clear.Click += (_, _) => { session.Clear(); log.Clear(); input.Clear(); SetStatus(); };
        save.Click += (_, _) => SaveLog();
        KeyDown += (_, e) => Append("KeyDown", e); KeyUp += (_, e) => Append("KeyUp", e);
        Deactivate += (_, _) => { session.Stop(); SetStatus(); };
    }
    private void Append(string kind, KeyEventArgs e)
    {
        if (!ContainsFocus) return;
        var entry = session.Record(kind, e);
        if (entry is not null) { log.AppendText(entry + Environment.NewLine); SetStatus(); }
    }
    private void SetStatus()
    {
        start.Enabled = !session.IsRecording; stop.Enabled = session.IsRecording;
        status.Text = $"{(session.IsRecording ? "KAYIT AKTİF" : "DURDU")} · {session.Entries.Count} olay";
    }
    private void SaveLog()
    {
        session.Stop(); SetStatus();
        using var dialog = new SaveFileDialog { Filter = "Metin dosyası (*.txt)|*.txt", FileName = "tus-olaylari.txt", DefaultExt = "txt" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { session.Save(dialog.FileName); status.Text = $"Kaydedildi · {session.Entries.Count} olay"; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { MessageBox.Show(this, "Dosya kaydedilemedi: " + ex.Message, "Kayıt hatası", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
