using Gst;
using Gst.Video;
namespace TinyPlayer.Core
{
    internal static class VideoSinkFactory
    {
        public static bool TryCreateAndAttach(Element playbin, IntPtr hwnd)
        {
            var sink = ElementFactory.Make("d3dvideosink", "video_sink");
            if (sink == null)
            {
                Console.WriteLine("[GStreamerCore] d3dvideosink not found");
                return false;
            }

            playbin["video-sink"] = sink;

            var bus = playbin.Bus;
            bus.EnableSyncMessageEmission();
            bus.SyncMessage += (o, args) =>
            {
                var msg = (Gst.Message)args.Args[0];

                // Check manually the name of the message structure
                if (msg.Type != MessageType.Element)
                {
                    return;
                }

                if (msg.Structure == null)
                {
                    return;
                }

                if (msg.Structure.Name != "prepare-window-handle")
                {
                    return;
                }

                var adapter = new VideoOverlayAdapter(msg.Src.Handle);
                adapter.WindowHandle = hwnd;
                adapter.HandleEvents(true);
                Console.WriteLine("[GStreamerCore] HWND attached via SyncMessage"); // TODO add logger
            };

            return true;
        }
    }
}

