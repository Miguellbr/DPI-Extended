using System;
using System.IO;
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

                var init = engine.Evaluate(@"DpiUbo.initialize()");
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

        public bool IsBlocked(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || _engine == null || _match == null)
                return false;

            try
            {
                var origin = _engine.GetValue("DpiUboOrigin");
                var result = _match.Value.Call(
                    JsValue.Undefined,
                    url,
                    origin,
                    "other");

                return result.AsNumber() == 1;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _gate.Dispose();
            _engine?.Dispose();
            _engine = null;
        }
    }
}
