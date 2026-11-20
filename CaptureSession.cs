using System.Text;

namespace KeyEventLogger;

internal sealed record KeyStroke(DateTimeOffset Time, Keys Key, Keys Modifiers, bool Released)
{
    public string Action => Released ? "Bırakma" : "Basma";
    public string ModifierText => Modifiers == Keys.None ? "—" : Modifiers.ToString();
}

internal sealed class CaptureSession
{
    public const int Capacity = 1000;
    private readonly Queue<KeyStroke> events = new();
    private readonly HashSet<Keys> held = [];
    public bool Active { get; private set; }
    public int Presses { get; private set; }
    public Keys? LastKey { get; private set; }

    public void Start() => Active = true;
    public void Stop() { Active = false; ResetHeld(); }
    public void ResetHeld() => held.Clear();
    public void Clear() { events.Clear(); Presses = 0; LastKey = null; }

    public KeyStroke? Press(Keys key, Keys modifiers)
    {
        if (!Active || !held.Add(key)) return null;
        Presses++;
        LastKey = key;
        return Append(key, modifiers, false);
    }

    public KeyStroke? Release(Keys key, Keys modifiers, bool showRelease)
    {
        if (!Active || !held.Remove(key) || !showRelease) return null;
        return Append(key, modifiers, true);
    }

    private KeyStroke Append(Keys key, Keys modifiers, bool released)
    {
        if (events.Count == Capacity) events.Dequeue();
        var stroke = new KeyStroke(DateTimeOffset.Now, key, modifiers, released);
        events.Enqueue(stroke);
        return stroke;
    }

    public void Save(string path)
    {
        using var output = new StreamWriter(path, append: false, new UTF8Encoding(false));
        output.WriteLine("Zaman\tTuş\tOlay\tDeğiştiriciler");
        foreach (var stroke in events)
            output.WriteLine($"{stroke.Time:yyyy-MM-dd HH:mm:ss.fff zzz}\t{stroke.Key}\t{stroke.Action}\t{stroke.ModifierText}");
    }
}
