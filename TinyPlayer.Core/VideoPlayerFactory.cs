using TinyPlayer.Core.Abstractions;

namespace TinyPlayer.Core;

public class VideoPlayerFactory : IVideoPlayerFactory
{
    public VideoPlayerCore Create(string uri, IntPtr hwnd) => new(uri, hwnd);
}