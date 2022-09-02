using CommandLine;

namespace BmsToOsu;

public class Option
{
    [Option('i', "input", Required = true, HelpText = "input folder, the program will recursively search for available BMS beatmaps from this folder, available BMS beatmaps: .bms/.bml/.bme/.bmx")]
    public string InputPath { get; set; } = null!;

    [Option('o', "output", Required = true, HelpText = "output folder (the output folder will maintain the same directory structure as the input folder)")]
    public string OutPath { get; set; } = null!;

    [Option("no-sv", Default = false, HelpText = "weather to include SV")]
    public bool NoSv { get; set; }

    [Option("no-zip", Required = false, Default = false, HelpText = "whether to zip output folder to .osz")]
    public bool NoZip { get; set; }

    [Option("no-copy", Required = false, Default = false, HelpText = "whether to copy sound/image/video files into the output folder")]
    public bool NoCopy { get; set; }

    [Option("no-remove", Required = false, Default = false, HelpText = "whether to remove the output folder after zipping it to .osz")]
    public bool NoRemove { get; set; }

    [Option("generate-mp3", Required = false, Default = false, HelpText = "generate complete song file from samples of bms")]
    public bool GenerateMp3 { get; set; }

    [Option("ffmpeg", Required = false, Default = "", HelpText = "path of ffmpeg (The program will look for ffmpeg in the PATH by default)")]
    public string Ffmpeg { get; set; } = "";

    [Option("max-threads", Default = 10, HelpText = "max number of ffmpeg threads")]
    public int MaxThreads { get; set; }
    
}