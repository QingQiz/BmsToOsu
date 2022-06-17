using CommandLine;

namespace BmsToOsu;

public class Option
{
    [Option('i', "input", Required = true, HelpText = "Path of input folder containing folders of BMS charts.")]
    public string InputPath { get; set; } = null!;

    [Option('o', "output", Required = true, HelpText = "Path to output the converted files to.")]
    public string OutPath { get; set; } = null!;
    
    [Option('n', Required = false, Default = 0, HelpText = "Maximum number to parse")]
    public int Number { get; set; }
}