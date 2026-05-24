using Gst;
using TinyPlayer.Core.Events;

namespace TinyPlayer.Core.Abstractions;

public interface IVideoPlayerCore
{
    event Action<long, long>? PositionChanged;
    event EventHandler<StreamsAnalysedEventArgs>? StreamsAnalysed;
    event Action<string>? ErrorOccurred;
    event Action? EndOfStream;
    event Action<State>? StateChanged;
    void Play();
    void Pause();
    void Stop();
    void SeekTo(int seconds);
    void SetAudioTrack(int index);
    void SetSubtitleTrack(int index);
    void SetSubtitlesEnabled(bool enabled);
    void SetAudioEnabled(bool enabled);
    void SetVolume(double value);
    void SetRenderSize(int w, int h);
}