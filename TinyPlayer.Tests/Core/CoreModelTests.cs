using TinyPlayer.Core.Enums;
using TinyPlayer.Core.Events;
using TinyPlayer.Core.Models;

namespace TinyPlayer.Tests.Core;

public class CoreModelTests
{
    [Fact]
    public void MetadataModel_Defaults_AreInitialized()
    {
        // Arrange & Act
        var model = new MetadataModel();

        // Assert
        Assert.Equal(0, model.CurrentAudioIndex);
        Assert.Equal(-1, model.CurrentSubtitleIndex);
        Assert.Empty(model.AudioTracks);
        Assert.Empty(model.SubtitleTracks);
    }

    [Fact]
    public void StreamsAnalysedEventArgs_StoresProvidedValues()
    {
        // Arrange
        var metadata = new MetadataModel { NumOfAudioStreams = 2 };
        var info = "Audio 0: English";

        // Act
        var args = new StreamsAnalysedEventArgs(metadata, info);

        // Assert
        Assert.Same(metadata, args.Metadata);
        Assert.Equal(info, args.StreamInfo);
    }

    [Fact]
    public void AvFlagsType_EnableAllFlags_ContainsVideoAudioAndSubtitle()
    {
        // Arrange & Act
        var flags = AvFlagsType.EnableAllFlags;

        // Assert
        Assert.True(flags.HasFlag(AvFlagsType.Video));
        Assert.True(flags.HasFlag(AvFlagsType.Audio));
        Assert.True(flags.HasFlag(AvFlagsType.SubText));
    }
}
