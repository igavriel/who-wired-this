using System;

namespace WhoWiredThis.Puzzles.Diagnostics
{
    /// <summary>Encodes one 13-word matrix line: stick blocks carry signal, padding words are noise.</summary>
    internal static class MatrixLineEncoder
    {
        private const int Stick1Start = 0;
        private const int Stick2Start = 4;
        private const int ResultStart = 8;
        private const int W4 = 3;
        private const int W8 = 7;
        private const int W13 = 12;
        private const int WordCount = 13;
        private const string PaddingWord = "##";

        public static string EncodeLine(SymbolMatch s1, SymbolMatch s2, int sym1, int sym2, Random rng)
        {
            string[] words = new string[WordCount];
            for (int i = 0; i < WordCount; i++)
            {
                words[i] = NoiseWord(rng);
            }

            WriteStickBlock(words, Stick1Start, s1, rng, isStick1: true);
            WriteStickBlock(words, Stick2Start, s2, rng, isStick1: false);
            WriteResultBlock(words, s1, s2, rng);

            words[W4] = PaddingWord;
            words[W8] = PaddingWord;
            words[W13] = PaddingWord;

            return string.Join(" ", words);
        }

        private static void WriteStickBlock(string[] words, int start, SymbolMatch match, Random rng, bool isStick1)
        {
            for (int i = 0; i < 3; i++)
            {
                switch (match)
                {
                    case SymbolMatch.Exact:
                        words[start + i] = "00";
                        break;
                    case SymbolMatch.Present:
                        words[start + i] = isStick1 ? "01" : "20";
                        break;
                }
            }
        }

        private static void WriteResultBlock(string[] words, SymbolMatch s1, SymbolMatch s2, Random rng)
        {
            for (int i = 0; i < 4; i++)
            {
                if (s1 == SymbolMatch.Exact && s2 != SymbolMatch.Exact)
                {
                    words[ResultStart + i] = "0" + HexNibble(rng);
                }
                else if (s2 == SymbolMatch.Exact && s1 != SymbolMatch.Exact)
                {
                    words[ResultStart + i] = HexNibble(rng) + "0";
                }
                else if (s1 == SymbolMatch.Present && s2 == SymbolMatch.Present)
                {
                    words[ResultStart + i] = rng.Next(2) == 0 ? "12" : "21";
                }
            }
        }

        private static string NoiseWord(Random rng) => HexNibble(rng) + HexNibble(rng);

        private static string HexNibble(Random rng) => rng.Next(16).ToString("X1");
    }
}
