using TinyPlayer.Core.Events;
using TinyPlayer.Core.Models;

namespace TinyPlayer.Tests.Core;

public class ModelsAndEventsTests
{
    [Fact]
    public void MetadataModel_HasExpectedDefaults()
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
    public void StreamsAnalysedEventArgs_ExposesCtorValues()
    {
        // Arrange
        var metadata = new MetadataModel { NumOfAudioStreams = 2 };

        // Act
        var args = new StreamsAnalysedEventArgs(metadata, "stream-info");

        // Assert
        Assert.Same(metadata, args.Metadata);
        Assert.Equal("stream-info", args.StreamInfo);
    }

    [Fact]
    public void StreamItem_AllowsSettingProperties()
    {
        // Arrange & Act
        var item = new StreamItem { Index = 3, Title = "Audio 3" };

        // Assert
        Assert.Equal(3, item.Index);
        Assert.Equal("Audio 3", item.Title);
    }
}
