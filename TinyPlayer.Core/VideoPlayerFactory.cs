using Microsoft.Extensions.Logging;
using TinyPlayer.Core.Abstractions;

namespace TinyPlayer.Core;

public class VideoPlayerFactory(ILogger<VideoPlayerCore> logger) : IVideoPlayerFactory
{
    private readonly ILogger<VideoPlayerCore> _logger = logger;

    public VideoPlayerCore Create(string uri, IntPtr hwnd) => new(uri, hwnd, _logger);  
}