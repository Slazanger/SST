using Avalonia.Controls;
using Avalonia.Layout;

namespace EveSettings.Services;

internal static class DialogHelper
{
    public static async Task<bool> ConfirmAsync(Window parent, string title, string body)
    {
        var tcs = new TaskCompletionSource<bool>();

        var text = new TextBlock
        {
            Text = body,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };

        var w = new Window
        {
            Title = title,
            Width = 560,
            Height = 260,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var yes = new Button { Content = "Apply", MinWidth = 100 };
        var no = new Button { Content = "Cancel", MinWidth = 100 };

        yes.Click += (_, _) =>
        {
            tcs.TrySetResult(true);
            w.Close();
        };

        no.Click += (_, _) =>
        {
            tcs.TrySetResult(false);
            w.Close();
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { no, yes },
        };

        w.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 12,
            Children = { text, buttons },
        };

        w.Closed += (_, _) => tcs.TrySetResult(false);

        await w.ShowDialog(parent);
        return await tcs.Task;
    }
}
