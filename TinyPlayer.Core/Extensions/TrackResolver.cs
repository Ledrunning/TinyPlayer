using System.Globalization;

namespace TinyPlayer.Core.Extensions;

public static class TrackResolver
{
    public static string IsoToLanguageName(this string code, string? codec, uint bitrate, int index, string prefix)
    {
        string? name = null;

        if (!string.IsNullOrEmpty(code) && code != "und")
        {
            name = new CultureInfo(code).EnglishName;
        }

        if (name != null)
        {
            return name;
        }

        // Fallback — кодек + битрейт
        if (!string.IsNullOrEmpty(codec))
        {
            return bitrate > 0
                ? $"{codec} {bitrate / 1000}kbps"
                : codec;
        }

        return $"{prefix} {index}";
    }
}