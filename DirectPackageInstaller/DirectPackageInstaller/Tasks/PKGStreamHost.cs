using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DirectPackageInstaller.Tasks
{
    public static class PKGStreamHost
    {
        private static readonly object Sync = new();
        private static Process? Process;
        private static Task<bool>? StartTask;

        public static string BaseUrl =>
            (Environment.GetEnvironmentVariable("PKGSTREAM_URL") ?? "http://127.0.0.1:8080").TrimEnd('/');

        public static async Task<bool> EnsureStartedAsync(CancellationToken cancellationToken = default)
        {
            if (Environment.GetEnvironmentVariable("PKGSTREAM_URL") is string externalUrl &&
                !string.IsNullOrWhiteSpace(externalUrl))
                return true;

            lock (Sync)
            {
                if (Process is { HasExited: false })
                    return true;

                if (StartTask is null || StartTask.IsCompleted)
                    StartTask = StartCoreAsync();
            }

            return await StartTask.WaitAsync(cancellationToken);
        }

        private static async Task<bool> StartCoreAsync()
        {
            var root = AppContext.BaseDirectory;
            var script = Path.Combine(root, "PKGStream", "src", "server.js");

            if (!File.Exists(script))
                script = Path.Combine(App.WorkingDirectory, "PKGStream", "src", "server.js");

            if (!File.Exists(script))
                return false;

            var bundledNode = Path.Combine(Path.GetDirectoryName(script)!, "node", OperatingSystem.IsWindows() ? "node.exe" : "node");
            var node = Environment.GetEnvironmentVariable("PKGSTREAM_NODE");

            if (string.IsNullOrWhiteSpace(node))
                node = File.Exists(bundledNode) ? bundledNode : "node";

            var psi = new ProcessStartInfo
            {
                FileName = node,
                Arguments = $"\"{script}\"",
                WorkingDirectory = Path.GetDirectoryName(script)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            psi.Environment["HOST"] = "127.0.0.1";
            psi.Environment["PORT"] = "8080";
            psi.Environment["PKGSTREAM_ALLOW_REMOTE"] = "1";

            try
            {
                var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
                process.OutputDataReceived += (_, _) => { };
                process.ErrorDataReceived += (_, _) => { };

                if (!process.Start())
                    return false;

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                lock (Sync)
                    Process = process;

                using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(300) };

                for (var i = 0; i < 50; i++)
                {
                    try
                    {
                        using var response = await client.GetAsync($"{BaseUrl}/health");
                        if (response.IsSuccessStatusCode)
                            return true;
                    }
                    catch { }

                    await Task.Delay(100);
                }

                TryStop();
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static void TryStop()
        {
            lock (Sync)
            {
                StartTask = null;

                if (Process is not { } process)
                    return;

                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch { }

                process.Dispose();
                Process = null;
            }
        }
    }
}
