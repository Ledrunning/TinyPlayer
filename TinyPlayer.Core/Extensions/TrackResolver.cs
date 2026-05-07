using System.Globalization;
using TinyPlayer.Core.Models;

namespace TinyPlayer.Core.Extensions;

public static class TrackResolver
{
    public static string IsoToLanguageName(this string code, MetadataLanguage metadata)
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

        // Fallback codec + bitrate
        if (!string.IsNullOrEmpty(metadata.Codec))
        {
            return metadata.Bitrate > 0
                ? $"{metadata.Codec} {metadata.Bitrate / 1000}kbps"
                : metadata.Codec;
        }

        return $"{metadata.Prefix} {metadata.Index}";
    }
}