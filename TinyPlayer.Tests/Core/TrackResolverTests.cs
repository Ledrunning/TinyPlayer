using TinyPlayer.Core.Extensions;
using TinyPlayer.Core.Models;

namespace TinyPlayer.Tests.Core;

public class TrackResolverTests
{
    [Fact]
    public void IsoToLanguageName_ReturnsLanguageName_WhenIsoCodeIsValid()
    {
        // Arrange
        var metadata = new MetadataLanguage { Codec = "aac", Bitrate = 128000, Index = 0, Prefix = "Track" };

        // Act
        var result = "en".IsoToLanguageName(metadata);

        // Assert
        Assert.Equal("English", result);
    }

    [Fact]
    public void IsoToLanguageName_ReturnsCodecAndBitrate_WhenLanguageUnavailable()
    {
        // Arrange
        var metadata = new MetadataLanguage { Codec = "aac", Bitrate = 192000, Index = 1, Prefix = "Track" };

        // Act
        var result = "und".IsoToLanguageName(metadata);

        // Assert
        Assert.Equal("aac 192kbps", result);
    }

    [Fact]
    public void IsoToLanguageName_ReturnsCodecOnly_WhenBitrateIsZero()
    {
        // Arrange
        var metadata = new MetadataLanguage { Codec = "opus", Bitrate = 0, Index = 2, Prefix = "Track" };

        // Act
        var result = string.Empty.IsoToLanguageName(metadata);

        // Assert
        Assert.Equal("opus", result);
    }

    [Fact]
    public void IsoToLanguageName_ReturnsPrefixAndIndex_WhenNoLanguageAndNoCodec()
    {
        // Arrange
        var metadata = new MetadataLanguage { Codec = null, Bitrate = 0, Index = 4, Prefix = "Track" };

        // Act
        var result = string.Empty.IsoToLanguageName(metadata);

        // Assert
        Assert.Equal("Track 4", result);
    }
}
