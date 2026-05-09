namespace TinyPlayer.Core.Abstractions;

public interface IVideoPlayerFactory
{
    VideoPlayerCore Create(string uri, IntPtr hwnd);
}