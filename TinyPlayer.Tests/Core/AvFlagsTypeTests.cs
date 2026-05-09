using TinyPlayer.Core.Enums;

namespace TinyPlayer.Tests.Core;

public class AvFlagsTypeTests
{
    [Fact]
    public void EnableAllFlags_ContainsAllCoreFlags()
    {
        // Arrage & Act
        var all = AvFlagsType.EnableAllFlags;

        // Assert
        Assert.True(all.HasFlag(AvFlagsType.Video));
        Assert.True(all.HasFlag(AvFlagsType.Audio));
        Assert.True(all.HasFlag(AvFlagsType.SubText));
    }

    [Fact]
    public void DisableSubtitles_DoesNotContainSubtitleFlag()
    {
        // Arrange & Act
        var value = AvFlagsType.DisableSubtitles;

        // Assert
        Assert.True(value.HasFlag(AvFlagsType.Video));
        Assert.True(value.HasFlag(AvFlagsType.Audio));
        Assert.False(value.HasFlag(AvFlagsType.SubText));
    }

    [Fact]
    public void DisableAudio_DoesNotContainAudioFlag()
    {
        // Arrange & Act
        var value = AvFlagsType.DisableAudio;

        // Assert
        Assert.True(value.HasFlag(AvFlagsType.Video));
        Assert.True(value.HasFlag(AvFlagsType.SubText));
        Assert.False(value.HasFlag(AvFlagsType.Audio));
    }
}
