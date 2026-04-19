using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace SST.Services;

internal static class DialogHelper
{
    public static async Task<bool> ConfirmAsync(Window parent, string title, string body)
    {
        var tcs = new TaskCompletionSource<bool>();

        var text = new TextBlock
        {
            Text = body,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontSize = 11,
        };

        var scroll = new ScrollViewer { Content = text };

        var yes = new Button { Content = "Apply", MinWidth = 72 };
        var no = new Button { Content = "Cancel", MinWidth = 72 };

        var w = new Window
        {
            Title = title,
            MinWidth = 440,
            Width = 480,
            MinHeight = 200,
            Height = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
            Background = new SolidColorBrush(Color.Parse("#141414")),
            Foreground = new SolidColorBrush(Color.Parse("#E8E8E8")),
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
            Spacing = 6,
            Margin = new Avalonia.Thickness(0, 8, 0, 0),
            Children = { no, yes },
        };

        var grid = new Grid
        {
            Margin = new Avalonia.Thickness(10),
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
