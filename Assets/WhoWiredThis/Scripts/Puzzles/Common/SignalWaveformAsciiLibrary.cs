namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Fixed 3-line ASCII drawings of the Signal target waveforms, indexed like the WAVE/MODE
    /// controls and ResultVisualWave_5State subjects: 0=Flat, 1=Sine, 2=Square/Pulse,
    /// 3=Triangle/Saw, 4=Noise. Deterministic (noise is a hardcoded pattern) and safe for the
    /// diagnostic Body_TMP: every line is at most 30 chars and contains no '&lt;' or '&gt;'
    /// (rich text is enabled on the display).
    /// </summary>
    public static class SignalWaveformAsciiLibrary
    {
        public const int LineCount = 3;

        private static readonly string[][] Drawings =
        {
            // 0 — Flat line
            new[]
            {
                @"  +                |               ",
                @"  0  --------------+---------------",
                @"  -                |               "
            },
            // 1 — Sine
            new[]
            {
                @"  +    _     _     _     _     _   ",
                @"  0  _/_\___/_\___/|\___/_\___/_\__",
                @"  -  /   \_/   \_/ | \_/   \_/   \_"
            },
            // 2 — Square / pulse
            new[]
            {
                @"  +    __    __    __    __    __  ",
                @"  0   |  |  |  |  |  |  |  |  |  | ",
                @"  -  _|  |__|  |__|  |__|  |__|  |_"
            },
            // 3 — Triangle / saw
            new[]
            {
                @"  +  \    /\    /\    /\    /\    /",
                @"  0  -\--/--\--/-+\--/--\--/--\--/-",
                @"  -    \/    \/  | \/    \/    \/  "
            },
            // 4 — Noise (fixed deterministic pattern)
            new[]
            {
                @"  +  |  /\ |   \ | | /  \ | /| \  /",
                @"  0  /\/--\/-\-/\+-\/-\-/-\/-|-/\/-",
                @"  -    |      \  |  \  /     |  /  "
            }
        };

        /// <summary>Number of known waveform drawings (5).</summary>
        public static int WaveformCount => Drawings.Length;

        /// <summary>
        /// Returns the fixed 3-line drawing for a waveform index. Out-of-range indices
        /// fall back to the flat line so the diagnostic never renders garbage.
        /// </summary>
        public static string[] GetLines(int waveformIndex)
        {
            if (waveformIndex < 0 || waveformIndex >= Drawings.Length)
            {
                waveformIndex = 0;
            }

            return Drawings[waveformIndex];
        }
    }
}
