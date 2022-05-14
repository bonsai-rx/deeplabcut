using System;
using System.Drawing;

namespace Bonsai.DeepLabCut.Design
{
    static class ColorPalette
    {
        static readonly Color[] BrightPastelPalette = new[]
{
            ColorTranslator.FromHtml("#418CF0"),
            ColorTranslator.FromHtml("#FCB441"),
            ColorTranslator.FromHtml("#E0400A"),
            ColorTranslator.FromHtml("#056492"),
            ColorTranslator.FromHtml("#BFBFBF"),
            ColorTranslator.FromHtml("#1A3B69"),
            ColorTranslator.FromHtml("#FFE382"),
            ColorTranslator.FromHtml("#129CDD"),
            ColorTranslator.FromHtml("#CA6B4B"),
            ColorTranslator.FromHtml("#005CDB"),
            ColorTranslator.FromHtml("#F3D288"),
            ColorTranslator.FromHtml("#506381"),
            ColorTranslator.FromHtml("#F1B9A8"),
            ColorTranslator.FromHtml("#E0830A"),
            ColorTranslator.FromHtml("#7893BE")
        };

        public static Color GetColor(int colorIndex)
        {
            if (colorIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(colorIndex));
            }

            return BrightPastelPalette[colorIndex % BrightPastelPalette.Length];
        }
    }
}
