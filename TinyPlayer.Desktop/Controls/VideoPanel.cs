using System.Windows.Forms.Integration;

namespace TinyPlayer.Desktop.Controls;

/// <summary>
/// A simple host — a WinForms Panel within WPF via WindowsFormsHost.
/// The Panel has a native HWND out of the box, just like in an original WinForms project.
/// Add the following to .csproj:
///   <UseWindowsForms>true</UseWindowsForms>
/// </summary>
public class VideoPanel : WindowsFormsHost
{
    public Panel Panel { get; } = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Black };

    public VideoPanel()
    {
        Child = Panel;
    }
}
