using System.Runtime.InteropServices;

namespace IFY.SyncthingTray;

public class SystrayForm : Form
{
    private readonly Icon _defaultIcon;
    private Bitmap? _currentIcon;

    private readonly NotifyIcon _icon;

    private string? _hoverText;
    public string? HoverText
    {
        get => _hoverText;
        set
        {
            _hoverText = value;
            _icon.Text = value?.Length > 0 ? $"{Text}: {value}" : Text;
        }
    }

    public Action? IconLeftClickAction { get; set; }
    public Action? IconRightClickAction { get; set; }
    public Action? IconMiddleClickAction { get; set; }
    public Action? IconDoubleClickAction { get; set; }

    public SystrayForm(string text, Bitmap icon)
    {
        Text = text;
        _defaultIcon = Icon.FromHandle(icon.GetHicon());
        _currentIcon = icon;
        _icon = new()
        {
            Icon = _defaultIcon,
            Text = text,
        };

        Opacity = 0;
        ShowInTaskbar = false;

        // Prevent double-click being consumed as two clicks
        (MouseButtons Button, bool IsDouble) ev = new();
        var delay = new ManualResetEventSlim();
        var clickHandler = Task.Run(async () =>
        {
            while (delay.Wait(Timeout.Infinite))
            {
                await Task.Delay(250);

                Invoke(() =>
                {
                    if (ev.IsDouble)
                    {
                        IconDoubleClickAction?.Invoke();
                    }
                    else if (ev.Button == MouseButtons.Left)
                    {
                        IconLeftClickAction?.Invoke();
                    }
                    else if (ev.Button == MouseButtons.Right)
                    {
                        IconRightClickAction?.Invoke();
                    }
                    else if (ev.Button == MouseButtons.Middle)
                    {
                        IconMiddleClickAction?.Invoke();
                    }
                });

                ev = new();
                delay.Reset();
            }
        });
        _icon.MouseClick += (_, e) =>
        {
            if (ev.Button == MouseButtons.None)
            {
                ev.Button = e.Button;
                ev.IsDouble = false;
                delay.Set();
            }
        };
        _icon.DoubleClick += (_, _) =>
        {
            if (!ev.IsDouble)
            {
                ev.IsDouble = true;
                delay.Set();
            }
        };
    }

    public void Stop()
    {
        if (!Disposing && !IsDisposed)
        {
            Invoke(Close);
        }
    }

    public void ShowBalloonTip(int timeout, string tipTitle, string tipText, ToolTipIcon tipIcon)
        => _icon.ShowBalloonTip(timeout, tipTitle, tipText, tipIcon);

    public void ReplaceIcon(Bitmap icon)
    {
        if (_currentIcon == icon)
        {
            return;
        }

        if (Disposing || IsDisposed)
        {
            return;
        }
        if (InvokeRequired)
        {
            Invoke(() => ReplaceIcon(icon));
            return;
        }

        try
        {
            _currentIcon = icon;
            _icon.Icon = Icon.FromHandle(icon.GetHicon());
        }
        catch (ExternalException)
        {
            // GDI+ sometimes fails on GetHicon
            _icon.Icon = _defaultIcon;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        Visible = false;
        _icon.Visible = true;
        base.OnShown(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _icon.Visible = false;
            _icon.Dispose();
        }
        base.Dispose(disposing);
    }
}
