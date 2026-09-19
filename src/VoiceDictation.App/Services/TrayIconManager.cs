using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VoiceDictation.App.Services;

public sealed class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _appIcon;

    public TrayIconManager(Action onShowCapsule, Action onOpenSettings, Action onExit)
    {
        _appIcon = CreateMicrophoneIcon();

        var contextMenu = new ContextMenuStrip();

        var showItem = new ToolStripMenuItem("Kapsülü Göster");
        showItem.Click += (_, _) => onShowCapsule();

        var settingsItem = new ToolStripMenuItem("Ayarlar...");
        settingsItem.Click += (_, _) => onOpenSettings();

        var separator = new ToolStripSeparator();

        var exitItem = new ToolStripMenuItem("Çıkış");
        exitItem.Click += (_, _) => onExit();

        contextMenu.Items.Add(showItem);
        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(separator);
        contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = _appIcon,
            Text = "VoiceDictation - Yerel Sesli Dikte",
            Visible = true,
            ContextMenuStrip = contextMenu
        };

        _notifyIcon.DoubleClick += (_, _) => onOpenSettings();
    }

    private static Icon CreateMicrophoneIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var backgroundBrush = new SolidBrush(Color.FromArgb(13, 17, 23));
            g.FillEllipse(backgroundBrush, 1, 1, 30, 30);

            using var borderPen = new Pen(Color.FromArgb(88, 166, 255), 2);
            g.DrawEllipse(borderPen, 1, 1, 30, 30);

            using var micBrush = new SolidBrush(Color.FromArgb(88, 166, 255));
            g.FillRoundedRectangle(micBrush, 12, 7, 8, 12, 4);

            using var arcPen = new Pen(Color.FromArgb(240, 246, 252), 2);
            g.DrawArc(arcPen, 9, 11, 14, 10, 0, 180);
            g.DrawLine(arcPen, 16, 21, 16, 25);
            g.DrawLine(arcPen, 12, 25, 20, 25);
        }

        var hIcon = bitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    public void ShowNotification(string title, string message)
    {
        _notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _appIcon.Dispose();
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, int x, int y, int width, int height, int radius)
    {
        using var path = new GraphicsPath();
        path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
        path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
        path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
