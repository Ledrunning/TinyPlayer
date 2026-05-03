namespace TinyPlayer.Core.Models;

public class MetadataModel
{
    public int NumOfVideoStreams { get; set; }
    public int NumOfAudioStreams { get; set; }
    public int NumOfSubtitles { get; set; }

    // Currently active tracks (from the playbin at the time of analysis)
    public int CurrentAudioIndex { get; set; } = 0;
    public int CurrentSubtitleIndex { get; set; } = -1; // -1 = no subs

    // Pre-defined lists with names — create combo boxes directly from here
    public List<StreamItem> AudioTracks { get; } = [];
    public List<StreamItem> SubtitleTracks { get; } = [];
}