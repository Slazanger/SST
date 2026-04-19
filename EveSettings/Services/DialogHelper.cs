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

        var scroll = new ScrollViewer { Content = text };

        var yes = new Button { Content = "Apply", MinWidth = 100 };
        var no = new Button { Content = "Cancel", MinWidth = 100 };

        var w = new Window
        {
            Title = title,
            MinWidth = 520,
            Width = 580,
            MinHeight = 260,
            Height = 480,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
        };

        var maxH = parent.Screens?.Primary?.WorkingArea.Height * 0.88 ?? 800;
        if (maxH > 200)
            w.MaxHeight = maxH;

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
            Margin = new Avalonia.Thickness(0, 12, 0, 0),
            Children = { no, yes },
        };

        var grid = new Grid
        {
            Margin = new Avalonia.Thickness(16),
            RowDefinitions =
            [
                new RowDefinition(1, GridUnitType.Star),
                new RowDefinition(GridLength.Auto),
            ],
        };

        Grid.SetRow(scroll, 0);
        Grid.SetRow(buttons, 1);
        grid.Children.Add(scroll);
        grid.Children.Add(buttons);

        w.Content = grid;

        w.Closed += (_, _) => tcs.TrySetResult(false);

        await w.ShowDialog(parent);
        return await tcs.Task;
    }
}
