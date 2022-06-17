using System.Data.Common;
using System.Globalization;
using BmsToOsu.Utils;
using log4net;

namespace BmsToOsu.Entity;

public class LocalTrackData
{
    public readonly double MeasureScale = 1;
    public readonly List<LocalBpmChange> BpmChanges = new();
    public readonly List<LocalStop> Stops = new();

    public LocalTrackData(
        int trackNo, IEnumerable<Line> lines, IReadOnlyDictionary<string, double> bpmChangeIndex,
        IReadOnlyDictionary<string, double> stopIndex)
    {
        var logger = LogManager.GetLogger(GetType())!;

        foreach (var line in lines)
        {
            switch (line.Channel)
            {
                case "02":
                {
                    if (double.TryParse(line.Message, out var i))
                    {
                        if (i <= 0)
                        {
                            logger.Error(
                                $"* Measure scale is negative or 0. cannot continue parsing (Track: {trackNo})");
                            throw new InvalidDataException();
                        }

                        MeasureScale = i;
                    }
                    else
                    {
                        logger.Error($"* Measure scale is invalid. cannot continue parsing (Track: {trackNo})");
                        throw new InvalidDataException();
                    }

                    continue;
                }
                case "03":
                case "08":
                {
                    if (line.Message.Length == 0)
                    {
                        continue;
                    }

                    for (var i = 0; i < line.Message.Length; i += 2)
                    {
                        var val = line.Message[i..(i + 2)];

                        if (val == "00") continue;

                        double bpm;

                        if (line.Channel == "03")
                        {
                            if (int.TryParse(val, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var bpmP))
                            {
                                bpm = bpmP;
                            }
                            else
                                continue;
                        }
                        else
                        {
                            if (bpmChangeIndex.ContainsKey(val))
                            {
                                bpm = bpmChangeIndex[val];
                            }
                            else
                                continue;
                        }

                        BpmChanges.Add(new LocalBpmChange
                        {
                            Position   = Timing.GetPosition(i, line.Message.Length),
                            Bpm        = bpm,
                            IsNegative = bpm < 0
                        });
                    }

                    continue;
                }
                case "09":
                {
                    if (line.Message.Length == 0)
                    {
                        continue;
                    }

                    for (var i = 0; i < line.Message.Length; i += 2)
                    {
                        var val = line.Message[i..(i + 2)];

                        if (stopIndex.ContainsKey(val))
                        {
                            Stops.Add(new LocalStop
                            {
                                Position = Timing.GetPosition(i, line.Message.Length),
                                Duration = stopIndex[val]
                            });
                        }
                    }

                    continue;
                }
            }
        }

        BpmChanges = BpmChanges.OrderBy(c => c.Position).ToList();
        Stops      = Stops.OrderBy(s => s.Position).ToList();
    }
}