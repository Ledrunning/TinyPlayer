using TinyPlayer.Core.Models;

namespace TinyPlayer.Core.Events;

public class StreamsAnalysedEventArgs(MetadataModel metadata, string info) : EventArgs
{
    public MetadataModel Metadata { get; } = metadata;
    public string StreamInfo { get; } = info;
}