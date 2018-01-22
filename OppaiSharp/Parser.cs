﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace OppaiSharp
{
    internal static class Parser
    {
        /// <summary>
        /// Calls Reset() on beatmap and parses a osu file into it.
        /// If beatmap is null, it will be initialized to a new Beatmap
        /// </summary>
        /// <returns><see cref="Beatmap"/></returns>
        public static Beatmap Read(StreamReader reader)
        {
            var bm = new Beatmap();

            string line, section = null;
            while ((line = reader.ReadLine()) != null) {
                //comments (according to lazer)
                if (line.StartsWith(" ") || line.StartsWith("_"))
                    continue;

                line = line.Trim();
                if (line.Length <= 0)
                    continue;

                //c++ style comments
                if (line.StartsWith("//"))
                    continue;

                //[SectionName]
                //don't continue here, the read methods will start reading at the next line
                if (line.StartsWith("["))
                    section = line.Substring(1, line.Length - 2);

                switch (section) {
                    case "Metadata":
                        foreach (var s in ReadSectionPairs(reader)) {
                            var val = s.Value;
                            switch (s.Key) {
                                case "Title":
                                    bm.Title = val;
                                    break;
                                case "TitleUnicode":
                                    bm.TitleUnicode = val;
                                    break;
                                case "Artist":
                                    bm.Artist = val;
                                    break;
                                case "ArtistUnicode":
                                    bm.ArtistUnicode = val;
                                    break;
                                case "Creator":
                                    bm.Creator = val;
                                    break;
                                case "Version":
                                    bm.Version = val;
                                    break;
                            }
                        }
                        break;
                    case "General":
                        foreach (var pair in ReadSectionPairs(reader))
                            if (pair.Key == "Mode")
                                bm.Mode = (GameMode)int.Parse(pair.Value);
                        break;
                    case "Difficulty":
                        bool arFound = false;
                        foreach (var s in ReadSectionPairs(reader)) {
                            var val = s.Value;
                            switch (s.Key) {
                                case "CircleSize":
                                    bm.CS = float.Parse(val);
                                    break;
                                case "OverallDifficulty":
                                    bm.OD = float.Parse(val);
                                    break;
                                case "ApproachRate":
                                    bm.AR = float.Parse(val);
                                    arFound = true;
                                    break;
                                case "HPDrainRate":
                                    bm.HP = float.Parse(val);
                                    break;
                                case "SliderMultiplier":
                                    bm.SliderVelocity = float.Parse(val);
                                    break;
                                case "SliderTickRate":
                                    bm.TickRate = float.Parse(val);
                                    break;
                            }
                        }
                        if (!arFound)
                            bm.OD = bm.AR;
                        break;
                    case "TimingPoints":
                        foreach (var ptLine in ReadSectionLines(reader)) {
                            string[] splitted = ptLine.Split(',');

                            if (splitted.Length > 8)
                                Warn("timing point with trailing values");

                            var t = new Timing {
                                Time = double.Parse(splitted[0]),
                                MsPerBeat = double.Parse(splitted[1])
                            };

                            if (splitted.Length >= 7)
                                t.Change = splitted[6].Trim() != "0";

                            bm.TimingPoints.Add(t);
                        }
                        break;
                    case "HitObjects":
                        foreach (var objLine in ReadSectionLines(reader)) {
                            string[] s = objLine.Split(',');

                            if (s.Length > 11)
                                Warn("object with trailing values");

                            var obj = new HitObject {
                                Time = double.Parse(s[2]),
                                Type = (HitObjectType)int.Parse(s[3])
                            };

                            if ((obj.Type & HitObjectType.Circle) != 0)
                            {
                                bm.CountCircles++;
                                obj.Data = new Circle {
                                    Position = new Vector2 {
                                        X = double.Parse(s[0]),
                                        Y = double.Parse(s[1])
                                    }
                                };
                            }
                            if ((obj.Type & HitObjectType.Spinner) != 0)
                            {
                                bm.CountSpinners++;
                            }
                            if ((obj.Type & HitObjectType.Slider) != 0)
                            {
                                bm.CountSliders++;
                                obj.Data = new Slider {
                                    Position = {
                                        X = double.Parse(s[0]),
                                        Y = double.Parse(s[1])
                                    },
                                    Repetitions = int.Parse(s[6]),
                                    Distance = double.Parse(s[7])
                                };
                            }

                            bm.Objects.Add(obj);
                        }
                        break;
                    default:
                        int fmtIndex = line.IndexOf("file format v", StringComparison.Ordinal);
                        if (fmtIndex < 0)
                            continue;

                        bm.FormatVersion = int.Parse(line.Substring(fmtIndex + "file format v".Length));
                        break;
                }
            }
            return bm;
        }

        //IEnumerable<KeyValuePair<string, string>>
        private static Dictionary<string, string> ReadSectionPairs(StreamReader sr)
        {
            var dic = new Dictionary<string, string>();

            string line;
            while (!string.IsNullOrEmpty(line = sr.ReadLine().Trim()))
            {
                int i = line.IndexOf(':');

                if (i == -1)
                    throw new Exception("Invalid key/value line: " + line);

                string key = line.Substring(0, i);
                string value = line.Substring(i + 1);

                dic.Add(key.TrimEnd(), value.TrimStart());
            }

            return dic;
        }

        private static List<string> ReadSectionLines(StreamReader sr)
        {
            var list = new List<string>();

            string line;
            while (!string.IsNullOrEmpty(line = sr.ReadLine()?.Trim()))
                list.Add(line);

            return list;
        }

        [Conditional("DEBUG")]
        private static void Warn(string fmt, params object[] args)
        {
            Debug.WriteLine(string.Format("W: " + fmt, args));
        }
    }
}
