using CommandLine;

namespace BmsToOsu;

public class Option
{
    [Option('i', "input", Required = true, HelpText = "folder containing bms charts")]
    public string InputPath { get; set; } = null!;

    [Option('o', "output", Required = true, HelpText = "output folder/filename. e.g. 114514.osz")]
    public string OutPath { get; set; } = null!;
    
    [Option("no-zip", Required = false, Default = false, HelpText = "whether to zip output folder to .osz")]
    public bool NoZip { get; set; }

    [Option("no-copy", Required = false, Default = false, HelpText = "whether to copy sound/image/video files into the output folder")]
    public bool NoCopy { get; set; }

    [Option("no-remove", Required = false, Default = false, HelpText = "whether to remove the output folder after zipping it to .osz")]
    public bool NoRemove { get; set; }
}