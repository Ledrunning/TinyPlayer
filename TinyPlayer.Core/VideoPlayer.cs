using GLib;
using Gst;
using TinyPlayer.Core.Enums;
using Application = Gst.Application;
using ObjectManager = GtkSharp.GstreamerSharp.ObjectManager;
using Thread = System.Threading.Thread;
using Value = GLib.Value;

namespace TinyPlayer.Core;

// Use D3DImage !
public class VideoPlayer
{
    //private readonly Element _playbin;
    //private D3DImage d3dImage;
    //private Thread mainGlibThread;
    //private MainLoop mainLoop;

    //private Element playbin;

    //public VideoPlayer()
    //{
    //    InitializeComponent();

    //    // Инициализация GStreamer
    //    Application.Init();
    //    ObjectManager.Initialize();

    //    // Настройка D3DImage
    //    d3dImage = new D3DImage();
    //    VideoImage.Source = d3dImage;

    //    // Инициализация конвейера GStreamer
    //    InitGStreamerPipeline();
    //}

    //private void InitGStreamerPipeline()
    //{
    //    // Создаем основной цикл GStreamer
    //    mainLoop = new MainLoop();
    //    mainGlibThread = new Thread(mainLoop.Run);
    //    mainGlibThread.Start();

    //    // Создаем playbin
    //    playbin = ElementFactory.Make("playbin");
    //    if (playbin == null)
    //    {
    //        Console.WriteLine("Не удалось создать playbin.");
    //        return;
    //    }

    //    // Устанавливаем URI видеофайла
    //    playbin["uri"] = @"file:///E:/ARSIS/TestVideos/costarica.mp4";

    //    // Создаем d3dvideosink
    //    var d3dSink = ElementFactory.Make("d3dvideosink", "video_sink");
    //    d3dSink.SetProperty("handle-shared", true);

    //    // Подключаем обработчик события обновления текстуры
    //    d3dSink["new-surface"] = new SignalCallback(OnNewSurface);

    //    // Связываем d3dvideosink с playbin
    //    playbin["video-sink"] = d3dSink;

    //    // Устанавливаем начальные флаги
    //    SetFlags();

    //    // Устанавливаем состояние воспроизведения
    //    var ret = playbin.SetState(State.Playing);
    //    if (ret == StateChangeReturn.Failure)
    //    {
    //        Console.WriteLine("Не удалось установить состояние воспроизведения.");
    //    }
    //}

    //private void SetFlags()
    //{
    //    var flags = (uint)playbin["flags"];
    //    flags |= (uint)(AvFlagsType.Video | AvFlagsType.Audio | AvFlagsType.SubText);
    //    playbin.SetProperty("flags", new Value(flags));
    //}

    //private void OnNewSurface(IntPtr sharedTextureHandle)
    //{
    //    // Обновляем D3DImage при изменении текстуры
    //    //Dispatcher.Invoke(() => { InitializeD3DImage(sharedTextureHandle); });
    //}

    //private void InitializeD3DImage(IntPtr sharedTextureHandle)
    //{
    //    d3dImage.Lock();
    //    //d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, sharedTextureHandle);
    //    d3dImage.Unlock();
    //    //d3dImage.AddDirtyRect(new Int32Rect(0, 0, d3dImage.PixelWidth, d3dImage.PixelHeight));
    //}

    //protected override void OnClosed(EventArgs e)
    //{
    //    //base.OnClosed(e);

    //    // Завершаем GStreamer при закрытии окна
    //    playbin.SetState(State.Ready);
    //    playbin.SetState(State.Null);
    //    mainLoop.Quit();

    //    playbin.Dispose();
    //    mainGlibThread.Join();
    //}
}

