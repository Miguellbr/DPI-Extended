using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DirectPackageInstaller.Others;

namespace DirectPackageInstaller.Views
{
    public sealed class DiagnosticLogsWindow : Window
    {
        private readonly TextBox _content;

        public DiagnosticLogsWindow()
        {
            Title = "DPI-Extended - Diagnostic Logs";
            Width = 850;
            Height = 560;
            MinWidth = 360;
            MinHeight = 300;
            Background = new SolidColorBrush(Color.Parse("#171C2C"));
            Foreground = Brushes.White;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _content = new TextBox
            {
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.NoWrap,
                FontFamily = new FontFamily("monospace"),
                MinHeight = 180
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            buttons.Children.Add(MakeButton("Copy all", async () => await CopyText(DiagnosticLog.GetAll())));
            buttons.Children.Add(MakeButton("Copy warnings/errors", async () =>
            {
                var errors = DiagnosticLog.GetErrors();
                await CopyText(string.IsNullOrWhiteSpace(errors) ? "No warnings or errors recorded." : errors);
            }));
            buttons.Children.Add(MakeButton("Clear", () =>
            {
                DiagnosticLog.Clear();
                Refresh();
                return Task.CompletedTask;
            }));
            buttons.Children.Add(MakeButton("Export .txt", Export));
            buttons.Children.Add(MakeButton("Close", () =>
            {
                Close();
                return Task.CompletedTask;
            }));

            var layout = new DockPanel { Margin = new Thickness(12), LastChildFill = true };
            DockPanel.SetDock(buttons, Dock.Bottom);
            layout.Children.Add(buttons);
            layout.Children.Add(_content);
            Content = layout;

            DiagnosticLog.Changed += OnLogChanged;
            Closed += (_, _) => DiagnosticLog.Changed -= OnLogChanged;
            Refresh();
        }

        private static Button MakeButton(string label, Func<Task> action)
        {
            var button = new Button { Content = label, Padding = new Thickness(10, 6) };
            button.Click += async (_, _) =>
            {
                try { await action(); }
                catch (Exception ex) { DiagnosticLog.Error($"Log window action failed: {ex.Message}"); }
            };
            return button;
        }

        private async Task CopyText(string text)
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard == null)
            {
                DiagnosticLog.Warn("Clipboard is unavailable on this platform.");
                return;
            }
            await clipboard.SetTextAsync(text);
        }

        private async Task Export()
        {
            var path = Path.Combine(App.WorkingDirectory, "DPI-Extended-logs.txt");
            await File.WriteAllTextAsync(path, DiagnosticLog.GetAll());
            DiagnosticLog.Info($"Logs exported to {path}");
            Refresh();
        }

        private void OnLogChanged() => Dispatcher.UIThread.Post(Refresh);

        private void Refresh()
        {
            if (_content != null)
                _content.Text = DiagnosticLog.GetAll();
        }
    }
}
