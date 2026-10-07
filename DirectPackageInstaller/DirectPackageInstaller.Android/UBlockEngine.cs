using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Jint;

namespace DirectPackageInstaller.Android
{
    /// <summary>
    /// Hosts the official uBlock Origin static network filtering engine (SNFE)
    /// inside the Android process. The JavaScript engine is isolated from the
    /// WebView and is only given the uBO bundle.
    /// </summary>
    internal sealed class UBlockEngine : IDisposable
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly object _engineLock = new();
        private Engine? _engine;
        private int _count;

        public int Count => _count;

        public async Task LoadAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_engine != null) return;

                var engine = new Engine(options =>
                {
                    options.LimitRecursion(256);
                    options.TimeoutInterval(TimeSpan.FromSeconds(30));
                });

                using var stream = Application.Context.Assets?.Open("ubocore.bundle.js")
                    ?? throw new InvalidOperationException("uBO core asset is missing.");
                using var reader = new StreamReader(stream);
                var bundle = await reader.ReadToEndAsync().ConfigureAwait(false);

                engine.Execute(bundle);

                var lists = await FetchListsAsync().ConfigureAwait(false);
                var listsJson = JsonSerializer.Serialize(lists);

                // Jint 4.2 exposes async JavaScript invocation through InvokeAsync.
                // DpiUbo.initialize() returns a Promise because uBO list compilation
                // is asynchronous.
                await engine.InvokeAsync("DpiUbo.initialize", listsJson).ConfigureAwait(false);

                _count = (int)engine.GetValue("DpiUboRuleCount").AsNumber();

                lock (_engineLock)
                {
                    _engine = engine;
                }
            }
            catch
            {
                engineDispose(_engine);
                throw;
            }
            finally
            {
                _gate.Release();
            }
        }

        public bool IsBlocked(string? url, string? originUrl, string type = "other")
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            Engine? engine;
            lock (_engineLock)
            {
                engine = _engine;
            }

            if (engine == null)
                return false;

            try
            {
                var origin = string.IsNullOrWhiteSpace(originUrl) ? url : originUrl;

                lock (_engineLock)
                {
                    if (!ReferenceEquals(engine, _engine))
                        return false;

                    // uBO's matchRequest() returns a numeric result. A value of 1
                    // represents a blocked request for the filtering context used here.
                    var result = engine.Invoke(
                        "DpiUboMatch",
                        url,
                        origin,
                        type);

                    return result.AsNumber() == 1;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void engineDispose(Engine? engine)
        {
            engine?.Dispose();
        }

        private static async Task<List<Dictionary<string, string>>> FetchListsAsync()
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var urls = new[]
            {
                ("ubo-ads", "https://ublockorigin.github.io/uAssetsCDN/filters/filters.min.txt"),
                ("ubo-badware", "https://ublockorigin.github.io/uAssetsCDN/filters/badware.min.txt"),
                ("ubo-privacy", "https://ublockorigin.github.io/uAssetsCDN/filters/privacy.min.txt"),
                ("ubo-unbreak", "https://ublockorigin.github.io/uAssetsCDN/filters/unbreak.min.txt"),
                ("ubo-quick", "https://ublockorigin.github.io/uAssetsCDN/filters/quick-fixes.min.txt"),
                ("easylist", "https://easylist.to/easylist/easylist.txt"),
                ("easyprivacy", "https://easylist.to/easylist/easyprivacy.txt"),
                ("plowe", "https://pgl.yoyo.org/adservers/serverlist.php?hostformat=hosts&showintro=1&mimetype=plaintext")
            };
            var result = new List<Dictionary<string, string>>();
            foreach (var (name, url) in urls)
            {
                try
                {
                    var raw = await client.GetStringAsync(url).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(raw))
                        result.Add(new Dictionary<string, string> { ["name"] = name, ["raw"] = raw });
                }
                catch { }
            }
            return result;
        }

        public void Dispose()
        {
            _gate.Dispose();
            lock (_engineLock)
            {
                _engine?.Dispose();
                _engine = null;
            }
        }
    }
}
