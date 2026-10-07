using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Jint;
using Jint.Native;

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
        private Engine? _engine;
        private JsValue? _match;
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
                var init = engine.Invoke("DpiUbo.initialize", listsJson);
                await init.UnwrapIfPromiseAsync().ConfigureAwait(false);

                _engine = engine;
                _match = engine.GetValue("DpiUboMatch");
                _count = engine.GetValue("DpiUboRuleCount").AsNumber() is var n ? (int)n : 0;
            }
            catch
            {
                _engine?.Dispose();
                _engine = null;
                throw;
            }
            finally
            {
                _gate.Release();
            }
        }

        public bool IsBlocked(string? url, string? originUrl, string type = "other")
        {
            if (string.IsNullOrWhiteSpace(url) || _engine == null || _match == null)
                return false;

            try
            {
                var origin = string.IsNullOrWhiteSpace(originUrl) ? url : originUrl;
                var result = _match.Value.Call(
                    JsValue.Undefined,
                    url,
                    origin,
                    type);

                return result.AsNumber() == 1;
            }
            catch
            {
                return false;
            }
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
            _engine?.Dispose();
            _engine = null;
        }
    }
}
