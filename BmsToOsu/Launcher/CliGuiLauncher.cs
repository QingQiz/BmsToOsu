using System.Diagnostics;
using System.Runtime.InteropServices;
using BmsToOsu.Utils;
using Terminal.Gui;
using OpenMode = Terminal.Gui.OpenDialog.OpenMode;
using Attribute = Terminal.Gui.Attribute;
using File = System.IO.File;

// ReSharper disable InvertIf

namespace BmsToOsu.Launcher;

public static class GuiLauncher
{
    #region Color Scheme

    private static readonly ColorScheme WinScheme = new()
    {
        Normal    = new Attribute(Color.Green, Color.Black),
        Focus     = new Attribute(Color.Red, Color.Black),
        HotFocus  = new Attribute(Color.BrightCyan, Color.Black),
        HotNormal = new Attribute(Color.Cyan, Color.Black),
        Disabled  = new Attribute(Color.Gray, Color.Black),
    };

    private static readonly ColorScheme BorderScheme = new()
    {
        Normal    = new Attribute(Color.Green, Color.Black),
        HotNormal = new Attribute(Color.Red, Color.Black),
    };

    private static readonly ColorScheme TextScheme = new()
    {
        Normal    = new Attribute(Color.White, Color.Black),
        Focus     = new Attribute(Color.Cyan, Color.Black),
        HotFocus  = new Attribute(Color.BrightCyan, Color.Black),
        HotNormal = new Attribute(Color.Cyan, Color.Black),
        Disabled  = new Attribute(Color.DarkGray, Color.Black),
    };

    private static readonly ColorScheme ButtonScheme = new()
    {
        Normal    = new Attribute(Color.White, Color.Black),
        Focus     = new Attribute(Color.Cyan, Color.Gray),
        HotFocus  = new Attribute(Color.Cyan, Color.Gray),
        HotNormal = new Attribute(Color.White, Color.Black),
        Disabled  = new Attribute(Color.DarkGray, Color.Black),
    };

    #endregion

    #region Fields

    private static string _inputFolder = "";
    private static string _outputFolder = "";

    private static bool _zip = true;
    private static bool _remove = true;
    private static bool _copy = true;
    private static bool _sv = true;

    private static bool _generateMp3;
    private static string _ffmpeg = "";
    private static string _maxThread = "10";

    private static Option? _option;

    #endregion

    #region Help Functions

    private static void OpenUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName               = "xdg-open",
                    Arguments              = url,
                    RedirectStandardError  = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow         = true,
                    UseShellExecute        = false
                }
            };
            process.Start();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
    }

    private static void Quit()
    {
        var n = MessageBox.Query(50, 7, "Quit", "Are you sure you want to quit BmsToOsu?", "Yes", "No");
        if (n == 0)
        {
            Application.Top.Running = false;
        }
    }

    private static void About()
    {
        var query = MessageBox.Query(50, 8, "About",
            $"\n{Const.Github}\n\n{Const.Copyright}", "Open In Browser", "Close");

        if (query == 0) OpenUrl(Const.Github);
    }

    private static string SelectDialog(string title, OpenMode openMode = OpenMode.Directory)
    {
        var d = new OpenDialog(title, "", openMode: openMode)
        {
            AllowsMultipleSelection = false
        };

        var history = Path.GetTempPath() + $"/{title}.bms2osu.temp";
        var exists  = File.Exists(history);

        if (exists)
        {
            var h = File.ReadAllText(history).Trim();
            d.DirectoryPath = Path.GetDirectoryName(h);
            d.FilePath      = Path.GetFileName(h);
        }

        Application.Run(d);

        if (d.Canceled) return "";

        File.WriteAllText(history, d.FilePaths[0]);
        return d.FilePaths[0];
    }

    private static void BuildOption()
    {
        if (string.IsNullOrEmpty(_inputFolder))
        {
            MessageBox.ErrorQuery("Error", "Input Folder is required.", "OK");
            return;
        }

        if (string.IsNullOrEmpty(_outputFolder))
        {
            MessageBox.ErrorQuery("Error", "Output Folder is required.", "OK");
            return;
        }

        _option = new Option
        {
            InputPath   = _inputFolder,
            OutPath     = _outputFolder,
            GenerateMp3 = _generateMp3,
            Ffmpeg      = _ffmpeg,
            MaxThreads  = int.Parse(_maxThread),
            NoCopy      = !_copy,
            NoRemove    = !_remove,
            NoSv        = !_sv,
            NoZip       = !_zip
        };

        Application.Top.Running = false;
    }

    #endregion

    public static void Launch()
    {
        Application.Init();

        var top = Application.Top;

        var menu = new MenuBar(new MenuBarItem[]
        {
            new("_Action", new MenuItem[]
            {
                new("_Start", "", BuildOption, null, null, Key.CtrlMask | Key.Enter),
                new("_Quit", "", Quit, null, null, Key.CtrlMask | Key.Q)
            }),
            new("About", "", About)
        });

        var statusBar = new StatusBar(new StatusItem[]
        {
            new(Key.CtrlMask | Key.Enter, "~Ctrl+Enter~ Start", BuildOption),
            new(Key.CtrlMask | Key.Q, "~Ctrl+Q~ Exit", Quit),
        });

        var win = new Window
        {
            X = 0,
            Y = 0,

            Width  = Dim.Fill(),
            Height = Dim.Fill()
        };

        win.ColorScheme = WinScheme;

        top.Add(menu, win, statusBar);

        win.Border.BorderStyle = BorderStyle.None;
        win.Text = @"
 ██████╗  ███╗   ███╗  ██████╗    ████████╗  █████╗      █████╗   ██████╗ ██╗   ██╗
 ██╔══██╗ ████╗ ████║ ██╔════╝    ╚══██╔══╝ ██╔══██╗    ██╔══██╗ ██╔════╝ ██║   ██║
 ██████╦╝ ██╔████╔██║ ╚█████╗        ██║    ██║  ██║    ██║  ██║ ╚█████╗  ██║   ██║
 ██╔══██╗ ██║╚██╔╝██║  ╚═══██╗       ██║    ██║  ██║    ██║  ██║  ╚═══██╗ ██║   ██║
 ██████╦╝ ██║ ╚═╝ ██║ ██████╔╝       ██║    ╚█████╔╝    ╚█████╔╝ ██████╔╝ ╚██████╔╝
 ╚═════╝  ╚═╝     ╚═╝ ╚═════╝        ╚═╝     ╚════╝      ╚════╝  ╚═════╝   ╚═════╝";

        var inputFolder  = new Label(1, 0, "Input Folder:");
        var outputFolder = new Label(1, 1, "Output Folder:");
        var selectInput = new Button("Select Folder")
        {
            X           = Pos.Right(outputFolder) + 1,
            Y           = Pos.Top(inputFolder),
            ColorScheme = ButtonScheme,
        };
        var selectOutput = new Button("Select Folder")
        {
            X           = Pos.Right(outputFolder) + 1,
            Y           = Pos.Top(outputFolder),
            ColorScheme = ButtonScheme,
        };
        var frameViewIo = new FrameView("Input / Output")
        {
            Width       = Dim.Fill(),
            Height      = 4,
            X           = 0,
            Y           = 8,
            ColorScheme = BorderScheme
        };
        var frameViewIoInner = new View
        {
            Width       = Dim.Fill(),
            Height      = Dim.Fill(),
            ColorScheme = TextScheme
        };
        frameViewIoInner.Add(inputFolder, outputFolder, selectInput, selectOutput);
        frameViewIo.Add(frameViewIoInner);

        var y = 0;
        var sv = new CheckBox(1, y++, "Enable SV", true)
        {
            ColorScheme = ButtonScheme
        };
        var zip = new CheckBox(1, y++, "Zip output folder to .osz", true)
        {
            ColorScheme = ButtonScheme
        };
        var remove = new CheckBox(3, y++, "Remove output folder after zipping", true)
        {
            ColorScheme = ButtonScheme
        };
        var copy = new CheckBox(1, y, "Copy resources to output folder", true)
        {
            ColorScheme = ButtonScheme
        };

        var frameViewConfig = new FrameView("Configuration")
        {
            Width       = Dim.Fill(),
            Height      = 6,
            X           = 0,
            Y           = 13,
            ColorScheme = BorderScheme
        };
        var frameViewConfigInner = new View
        {
            Width       = Dim.Fill(),
            Height      = Dim.Fill(),
            ColorScheme = TextScheme
        };
        frameViewConfigInner.Add(sv, zip, remove, copy);
        frameViewConfig.Add(frameViewConfigInner);

        y = 0;
        var generateMp3 = new CheckBox(1, y++, "Generate MP3")
        {
            ColorScheme = ButtonScheme
        };
        var ffmpegPathLabel = new Label(3, y++, "FFMpeg:")
        {
            Enabled = false
        };
        var maxThreadLabel = new Label(3, y, "Max threads:")
        {
            Enabled = false
        };
        var ffmpegPath = new Button("Select FFMpeg")
        {
            X           = Pos.Right(ffmpegPathLabel) + 1,
            Y           = Pos.Top(ffmpegPathLabel),
            ColorScheme = ButtonScheme,
            Enabled     = false,
        };
        var maxThread = new TextField
        {
            X       = Pos.Right(maxThreadLabel) + 1,
            Y       = Pos.Top(maxThreadLabel),
            Width   = 14,
            Text    = "10",
            Enabled = false
        };
        var frameViewMp3Inner = new View
        {
            Width       = Dim.Fill(),
            Height      = Dim.Fill(),
            ColorScheme = TextScheme
        };
        var frameViewMp3 = new FrameView("Mp3 Generator")
        {
            Width       = Dim.Fill(),
            Height      = 5,
            X           = 0,
            Y           = 20,
            ColorScheme = BorderScheme
        };
        frameViewMp3Inner.Add(generateMp3, ffmpegPathLabel, ffmpegPath, maxThreadLabel, maxThread);
        frameViewMp3.Add(frameViewMp3Inner);

        win.Add(frameViewIo, frameViewConfig, frameViewMp3);

        // Event Handler
        selectInput.Clicked += () =>
        {
            var selected = SelectDialog("Select Input Folder");

            if (!string.IsNullOrEmpty(selected))
            {
                selectInput.Text = selected;
                _inputFolder     = selected;
            }
        };
        selectOutput.Clicked += () =>
        {
            var selected = SelectDialog("Select Output Folder");

            if (!string.IsNullOrEmpty(selected))
            {
                selectOutput.Text = selected;
                _outputFolder     = selected;
            }
        };

        sv.Toggled     += _ => _sv     = sv.Checked;
        zip.Toggled    += _ => _zip    = remove.Enabled = zip.Checked;
        remove.Toggled += _ => _remove = remove.Checked;
        copy.Toggled   += _ => _copy   = copy.Checked;

        generateMp3.Toggled += _ =>
            _generateMp3 = ffmpegPath.Enabled = maxThread.Enabled = ffmpegPathLabel.Enabled = maxThreadLabel.Enabled = generateMp3.Checked;
        ffmpegPath.Clicked += () =>
        {
            var selected = SelectDialog("Select FFMpeg", OpenMode.File);

            if (!string.IsNullOrEmpty(selected))
            {
                ffmpegPath.Text = selected;
                _ffmpeg         = selected;
            }
        };
        maxThread.TextChanged += _ => _maxThread = maxThread.Text.ToString()!;

        Application.Run();
        Application.Shutdown();

        Console.Clear();
        if (_option != null)
        {
            CliArgsLauncher.Convert(_option);
        }
    }
}