namespace BmsToOsu.Utils;

public static class PathExt
{
    public static string? FindSoundFile(string path, string soundName)
    {
        var name = Path.GetFileNameWithoutExtension(soundName);

        var ext = new[] { "wav", "mp3", "ogg", "3gp" }
            .FirstOrDefault(e => File.Exists(Path.Join(path, $"{name}.{e}")));

        return ext == null ? null : $"{name}.{ext}";
    }
}