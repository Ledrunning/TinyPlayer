namespace TinyPlayer.Core.Enums;

[Flags]
public enum AvFlagTypes : uint
{
    Video = 1 << 0,
    Audio = 1 << 1,
    SubText = 1 << 2,

    EnableAllFlags = Video | Audio | SubText,
    DisableSubtitles = Video | (Audio & ~SubText),
    DisableAudio = Video | (SubText & ~Audio)
}