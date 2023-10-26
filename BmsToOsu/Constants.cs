namespace BmsToOsu;

public static class Constants
{
    public const int MinSoundFileCount = 100;

    /// <summary>
    /// ffmpeg can open at most 1300 files
    /// </summary>
    public const int MaxFileCountFfmpegCanRead = 1300;

    public const int DefaultBpm = 130;

    /// <summary>
    /// in milliseconds
    /// </summary>
    public const int MaxSampleOffsetError = 5;
}