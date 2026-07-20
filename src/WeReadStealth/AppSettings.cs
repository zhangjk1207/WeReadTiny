using System;
using System.Runtime.Serialization;

namespace WeReadStealth
{
    [DataContract]
    public sealed class AppSettings
    {
        public const int CurrentVersion = 2;
        public const int MinimumWidth = 300;
        public const int MinimumHeight = 400;

        [DataMember(Order = 1)]
        public int? X { get; set; }

        [DataMember(Order = 2)]
        public int? Y { get; set; }

        [DataMember(Order = 3)]
        public int Width { get; set; }

        [DataMember(Order = 4)]
        public int Height { get; set; }

        [DataMember(Order = 5)]
        public double Opacity { get; set; }

        [DataMember(Order = 6)]
        public bool AlwaysOnTop { get; set; }

        [DataMember(Order = 7)]
        public string Theme { get; set; }

        [DataMember(Order = 8)]
        public int Version { get; set; }

        [DataMember(Order = 9)]
        public bool AutoHideToolbar { get; set; }

        public static AppSettings CreateDefault()
        {
            return new AppSettings
            {
                Width = 400,
                Height = 600,
                Opacity = 1.0,
                AlwaysOnTop = true,
                Theme = "light",
                Version = CurrentVersion,
                AutoHideToolbar = true
            };
        }

        public void Normalize()
        {
            if (Version < CurrentVersion)
            {
                AutoHideToolbar = true;
            }

            Version = CurrentVersion;
            Width = Math.Max(MinimumWidth, Width);
            Height = Math.Max(MinimumHeight, Height);
            Opacity = Math.Max(0.3, Math.Min(1.0, Opacity));

            if (!string.Equals(Theme, "dark", StringComparison.OrdinalIgnoreCase))
            {
                Theme = "light";
            }
            else
            {
                Theme = "dark";
            }
        }

        public AppSettings Copy()
        {
            return new AppSettings
            {
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                Opacity = Opacity,
                AlwaysOnTop = AlwaysOnTop,
                Theme = Theme,
                Version = Version,
                AutoHideToolbar = AutoHideToolbar
            };
        }
    }
}
