using System.Drawing;

namespace WeReadStealth
{
    internal sealed class UiTheme
    {
        internal static readonly UiTheme Light = new UiTheme(
            Color.FromArgb(247, 248, 250),
            Color.FromArgb(31, 41, 55),
            Color.FromArgb(224, 228, 234),
            Color.FromArgb(47, 111, 237),
            Color.FromArgb(220, 38, 38));

        internal static readonly UiTheme Dark = new UiTheme(
            Color.FromArgb(32, 33, 36),
            Color.FromArgb(244, 244, 245),
            Color.FromArgb(62, 64, 69),
            Color.FromArgb(92, 149, 255),
            Color.FromArgb(248, 113, 113));

        private UiTheme(Color background, Color foreground, Color border, Color accent, Color danger)
        {
            Background = background;
            Foreground = foreground;
            Border = border;
            Accent = accent;
            Danger = danger;
        }

        internal Color Background { get; }
        internal Color Foreground { get; }
        internal Color Border { get; }
        internal Color Accent { get; }
        internal Color Danger { get; }
    }
}
