using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using YARG.Core.Chart;

namespace YARG.Core.Engine.Drums
{
    /// <summary>
    /// A fruit fly's cached drum take: the strikes a fly-brain simulation played for one song,
    /// stored as a JSON list of {"t": seconds, "pad": name}.
    /// </summary>
    public sealed class FlyTake
    {
        public readonly struct Strike
        {
            public readonly double Time;
            public readonly string Pad;

            public Strike(double time, string pad)
            {
                Time = time;
                Pad = pad;
            }
        }

        // The fly's pads, in the order of fly-brain's connectome.PADS.
        public static readonly string[] PadNames = { "kick", "red", "yellow", "blue", "green" };

        public IReadOnlyList<Strike> Strikes { get; }

        private FlyTake(List<Strike> strikes)
        {
            Strikes = strikes;
        }

        private sealed class StrikeJson
        {
            [JsonProperty("t")]
            public double T;

            [JsonProperty("pad")]
            public string Pad = "";
        }

        public static FlyTake Parse(string json)
        {
            var raw = JsonConvert.DeserializeObject<List<StrikeJson>>(json)
                ?? throw new InvalidDataException("Fly take is empty.");

            var strikes = new List<Strike>(raw.Count);
            foreach (var s in raw)
            {
                if (Array.IndexOf(PadNames, s.Pad) < 0)
                {
                    throw new InvalidDataException(
                        $"Fly take has unknown pad '{s.Pad}'; expected one of {string.Join(", ", PadNames)}.");
                }

                strikes.Add(new Strike(s.T, s.Pad));
            }

            return new FlyTake(strikes.OrderBy(s => s.Time).ToList());
        }

        public static FlyTake Load(string path) => Parse(File.ReadAllText(path));

        // A fly with no take on file plays nothing, so it misses every note rather than passing as a perfect bot.
        public static FlyTake Empty => new FlyTake(new List<Strike>());

        /// <summary>
        /// The chart pad a fly pad name strikes in the given drum mode.
        /// </summary>
        public static int ToChartPad(DrumsEngineParameters.DrumMode mode, string pad)
        {
            if (mode == DrumsEngineParameters.DrumMode.FiveLane)
            {
                return pad switch
                {
                    "kick"   => (int) FiveLaneDrumPad.Kick,
                    "red"    => (int) FiveLaneDrumPad.Red,
                    "yellow" => (int) FiveLaneDrumPad.Yellow,
                    "blue"   => (int) FiveLaneDrumPad.Blue,
                    "green"  => (int) FiveLaneDrumPad.Green,
                    _        => throw new ArgumentOutOfRangeException(nameof(pad), pad, null),
                };
            }

            return pad switch
            {
                "kick"   => (int) FourLaneDrumPad.Kick,
                "red"    => (int) FourLaneDrumPad.RedDrum,
                "yellow" => (int) FourLaneDrumPad.YellowDrum,
                "blue"   => (int) FourLaneDrumPad.BlueDrum,
                "green"  => (int) FourLaneDrumPad.GreenDrum,
                _        => throw new ArgumentOutOfRangeException(nameof(pad), pad, null),
            };
        }
    }
}
