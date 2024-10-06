using Gst;
using GLib;
using TinyPlayer.Core.Enums;
using Value = GLib.Value;

namespace TinyPlayer.Core;

public class VideoPlayer
{
    private readonly Element _playbin;

    private void SetFlags()
    {
        var flags = (uint)_playbin["flags"];

        flags = (uint)(AvFlagsType.Audio | AvFlagsType.Video | AvFlagsType.SubText);
        _playbin.SetProperty("flags", new Value(flags));
    }
}