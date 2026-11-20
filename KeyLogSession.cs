using System.Text;
namespace KeyEventLogger;

public sealed record KeyLogEntry(DateTime Time, string Event, Keys Key, Keys Modifiers)
{
    public override string ToString() => $"{Time:yyyy-MM-dd HH:mm:ss.fff}\t{Event,-7}\t{Key,-16}\t{Modifiers}";
}

public sealed class KeyLogSession
{
    public bool IsRecording { get; private set; }
    public List<KeyLogEntry> Entries { get; } = [];
    public void Start() => IsRecording = true;
    public void Stop() => IsRecording = false;
    public KeyLogEntry? Record(string kind, KeyEventArgs e)
    {
        if (!IsRecording) return null;
        var entry = new KeyLogEntry(DateTime.Now, kind, e.KeyCode, e.Modifiers);
        Entries.Add(entry); return entry;
    }
    public void Clear() => Entries.Clear();
    public void Save(string path) => File.WriteAllLines(path,
        new[] { "Zaman\tOlay\tTuş\tDeğiştiriciler" }.Concat(Entries.Select(x => x.ToString())), new UTF8Encoding(false));
}
