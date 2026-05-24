using TinyPlayer.Core.Enums;

namespace TinyPlayer.Tests.Core;

public class AvFlagsTypeTests
{
    [Fact]
    public void EnableAllFlags_ContainsAllCoreFlags()
    {
        // Arrage & Act
        var all = AvFlagTypes.EnableAllFlags;

        // Assert
        Assert.True(all.HasFlag(AvFlagTypes.Video));
        Assert.True(all.HasFlag(AvFlagTypes.Audio));
        Assert.True(all.HasFlag(AvFlagTypes.SubText));
    }

    [Fact]
    public void DisableSubtitles_DoesNotContainSubtitleFlag()
    {
        // Arrange & Act
        var value = AvFlagTypes.DisableSubtitles;

        // Assert
        Assert.True(value.HasFlag(AvFlagTypes.Video));
        Assert.True(value.HasFlag(AvFlagTypes.Audio));
        Assert.False(value.HasFlag(AvFlagTypes.SubText));
    }

    [Fact]
    public void DisableAudio_DoesNotContainAudioFlag()
    {
        // Arrange & Act
        var value = AvFlagTypes.DisableAudio;

        // Assert
        Assert.True(value.HasFlag(AvFlagTypes.Video));
        Assert.True(value.HasFlag(AvFlagTypes.SubText));
        Assert.False(value.HasFlag(AvFlagTypes.Audio));
    }
}
