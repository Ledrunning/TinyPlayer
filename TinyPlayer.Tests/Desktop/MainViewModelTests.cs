using System.Reflection;
using System.Windows;
using TinyPlayer.Core.Abstractions;
using TinyPlayer.Core.Models;
using TinyPlayer.Desktop.ViewModel;

namespace TinyPlayer.Tests.Desktop;

public class MainViewModelTests
{
    [Fact]
    public void ToggleFullscreenCommand_TogglesWindowStateAndVisibility()
    {
        // Arrange
        var vm = new MainViewModel(new NoOpFactory());

        // Act & Assert
        vm.ToggleFullscreenCommand.Execute(null);

        Assert.Equal(WindowState.Maximized, vm.WindowState);
        Assert.Equal(WindowStyle.None, vm.WindowStyle);
        Assert.Equal(Visibility.Collapsed, vm.TitleBarVisibility);
        Assert.Equal(Visibility.Collapsed, vm.ControlsVisibility);

        vm.ToggleFullscreenCommand.Execute(null);

        Assert.Equal(WindowState.Normal, vm.WindowState);
        Assert.Equal(WindowStyle.SingleBorderWindow, vm.WindowStyle);
        Assert.Equal(Visibility.Visible, vm.TitleBarVisibility);
        Assert.Equal(Visibility.Visible, vm.ControlsVisibility);
    }

    [Fact]
    public void CommandsThatRequireCore_AreDisabled_WhenCoreNotLoaded()
    {
        // Arrange
        var vm = new MainViewModel(new NoOpFactory());

        // Act & Assert
        Assert.False(vm.TogglePlayPauseCommand.CanExecute(null));
        Assert.False(vm.ToggleMuteCommand.CanExecute(null));
        Assert.False(vm.StopCommand.CanExecute(null));
    }

    [Fact]
    public void OnVideoClick_MakesControlsVisible()
    {
        // Arrange
        var vm = new MainViewModel(new NoOpFactory())
        {
            IsControlsVisible = false,
            IsPlaying = true
        };

        // Act
        vm.OnVideoClick();

        // Assert
        Assert.True(vm.IsControlsVisible);
    }

    [Fact]
    public void BeginSeekAndEndSeek_DoNotThrow_AndKeepPosition()
    {
        // Arrange & Act
        var vm = new MainViewModel(new NoOpFactory())
        {
            Position = 42
        };

        // Act
        vm.BeginSeek();
        vm.EndSeek();

        // Assert
        Assert.Equal(42, vm.Position);
    }

    [Fact]
    public void SetVideoHandle_StoresHandleValue()
    {
        // Arrange & Act
        var vm = new MainViewModel(new NoOpFactory());
        var expected = new IntPtr(12345);

        vm.SetVideoHandle(expected);

        // Assert
        var field = typeof(MainViewModel).GetField("_hwnd", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        var actual = new IntPtr((nint)field.GetValue(vm)!);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ApplySubtitleFilter_WhenDisabled_ClearsSubtitleTracksAndSelection()
    {
        // Arrange & Act
        var vm = new MainViewModel(new NoOpFactory());
        vm.AllSubtitleTracks.Add(new StreamItem { Index = 1, Title = "English" });
        vm.SubtitleTracks.Add(new StreamItem { Index = 1, Title = "English" });
        vm.SelectedSubtitleTrack = vm.SubtitleTracks.First();
        vm.SubtitlesEnabled = false;

        // Act
        InvokePrivate(vm, "ApplySubtitleFilter");

        // Assert
        Assert.Empty(vm.SubtitleTracks);
        Assert.Null(vm.SelectedSubtitleTrack);
    }

    [Fact]
    public void ApplySubtitleFilter_WhenEnabled_RestoresTrackListAndSelection()
    {
        // Arrange & Act
        var vm = new MainViewModel(new NoOpFactory());
        vm.AllSubtitleTracks.Add(new StreamItem { Index = 3, Title = "English" });
        vm.AllSubtitleTracks.Add(new StreamItem { Index = 4, Title = "Deutsch" });
        vm.SubtitlesEnabled = true;
        vm.SelectedSubtitleTrack = new StreamItem { Index = 4, Title = "Deutsch" };

        // Act
        InvokePrivate(vm, "ApplySubtitleFilter");

        // Assert
        Assert.Equal(2, vm.SubtitleTracks.Count);
        Assert.NotNull(vm.SelectedSubtitleTrack);
        Assert.Equal(4, vm.SelectedSubtitleTrack?.Index);
    }

    [Fact]
    public void OnSubtitlesEnabledChanged_WhenStreamsInitialized_UpdatesFilteredTracks()
    {
        // Arrange & Act
        var vm = new MainViewModel(new NoOpFactory());
        vm.AllSubtitleTracks.Add(new StreamItem { Index = 8, Title = "Spanish" });
        SetPrivateField(vm, "_streamsInitialized", true);

        vm.SubtitlesEnabled = true;

        // Assert
        Assert.Single(vm.SubtitleTracks);
        Assert.Equal(8, vm.SubtitleTracks[0].Index);

        vm.SubtitlesEnabled = false;

        // Assert
        Assert.Empty(vm.SubtitleTracks);
        Assert.Null(vm.SelectedSubtitleTrack);
    }

    [Fact]
    public void FormatTime_ReturnsExpectedText()
    {
        // Arrange & Act
        var method = typeof(MainViewModel).GetMethod("FormatTime", BindingFlags.Static | BindingFlags.NonPublic);

        // Assert
        var result = (string)method?.Invoke(null, [61L, 125L])!;

        // Assert
        Assert.Equal("01:01 / 02:05", result);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(target, value);
    }

    private static object? InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        return method!.Invoke(target, args);
    }

    private sealed class NoOpFactory : IVideoPlayerFactory
    {
        public TinyPlayer.Core.VideoPlayerCore Create(string uri, IntPtr hwnd)
        {
            throw new NotSupportedException("Tests do not create the actual video core.");
        }
    }
}
