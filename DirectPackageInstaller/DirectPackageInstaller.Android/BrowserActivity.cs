using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Webkit;
using Android.Widget;
using Android.Views;
using Android.Content;
using Java.Interop;

namespace DirectPackageInstaller.Android
{
    [Activity(Label = "DPI Browser", Exported = false)]
    public sealed class BrowserActivity : Activity
    {
        private WebView _webView = null!;
        private EditText _address = null!;
        private Button _back = null!;
        private Button _forward = null!;
        private Button _refresh = null!;
        private Button _ublock = null!;
        private string _lastCandidate = "";
        private bool _uBlockEnabled = true;
        private readonly UBlockFilter _uBlock = new();

        protected override async void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
            var bar = new LinearLayout(this) { Orientation = Orientation.Horizontal };

            _back = new Button(this) { Text = "←" };
            _forward = new Button(this) { Text = "→" };
            _refresh = new Button(this) { Text = "↻" };
            _ublock = new Button(this) { Text = "🛡 ON" };

            _address = new EditText(this) { Text = Intent?.DataString ?? "https://" };
            _address.SetSingleLine(true);
            _address.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1);

            var go = new Button(this) { Text = "Go" };
            var use = new Button(this) { Text = "Use" };

            bar.AddView(_back);
            bar.AddView(_forward);
            bar.AddView(_refresh);
            bar.AddView(_address);
            bar.AddView(go);
            bar.AddView(_ublock);
            bar.AddView(use);
            root.AddView(bar);

            _webView = new WebView(this);
            _webView.Settings.JavaScriptEnabled = true;
            _webView.Settings.DomStorageEnabled = true;
            _webView.Settings.AllowFileAccess = false;
            _webView.Settings.AllowContentAccess = false;
            _webView.SetWebViewClient(new CaptureClient(this));
            _webView.SetDownloadListener(new CaptureDownloadListener(this));
            _webView.AddJavascriptInterface(new JsBridge(this), "DpiBridge");
            root.AddView(_webView, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 0, 1));

            SetContentView(root);

            go.Click += (_, _) => Navigate();
            _back.Click += (_, _) => { if (_webView.CanGoBack()) _webView.GoBack(); };
            _forward.Click += (_, _) => { if (_webView.CanGoForward()) _webView.GoForward(); };
            _refresh.Click += (_, _) => _webView.Reload();
            _ublock.Click += (_, _) => ToggleUBlock();
            use.Click += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(_lastCandidate))
                {
                    App.PublishBrowserUrl(_lastCandidate);
                    Finish();
                }
                else
                    Toast.MakeText(this, "Nenhum link de download capturado ainda.", ToastLength.Short).Show();
            };

            Navigate();
            await _uBlock.LoadAsync();
            Toast.MakeText(this, $"uBlock: {_uBlock.Count} regras carregadas.", ToastLength.Short).Show();
        }

        private void ToggleUBlock()
        {
            _uBlockEnabled = !_uBlockEnabled;
            _ublock.Text = _uBlockEnabled ? "🛡 ON" : "🛡 OFF";
            Toast.MakeText(this, _uBlockEnabled ? "uBlock ativado." : "uBlock desativado.", ToastLength.Short).Show();
            _webView.Reload();
        }

        private void Navigate()
        {
            var url = _address.Text?.Trim();
            if (string.IsNullOrWhiteSpace(url)) return;

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url;

            _address.Text = url;
            _webView.LoadUrl(url);
        }

        private bool IsBlocked(string? url) => _uBlockEnabled && _uBlock.IsBlocked(url);

        private void Capture(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return;

            _lastCandidate = url;
            RunOnUiThread(() =>
            {
                if (url.Contains(".pkg", StringComparison.OrdinalIgnoreCase) ||
                    url.Contains("download", StringComparison.OrdinalIgnoreCase))
                    Toast.MakeText(this, "Link de download capturado. Toque em Use.", ToastLength.Short).Show();
            });
        }

        private void CaptureDownload(string? url, string? userAgent, string? contentDisposition, string? mimetype, long contentLength)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            Capture(url);
            RunOnUiThread(() =>
            {
                var type = mimetype ?? string.Empty;
                var size = contentLength > 0 ? $" {contentLength / 1024d / 1024d:0.0} MB" : string.Empty;
                Toast.MakeText(this, $"Download capturado: {type}{size}", ToastLength.Short).Show();
            });
        }

        private sealed class CaptureClient : WebViewClient
        {
            private readonly BrowserActivity _owner;
            public CaptureClient(BrowserActivity owner) => _owner = owner;

            public override bool ShouldOverrideUrlLoading(WebView? view, IWebResourceRequest? request)
            {
                var url = request?.Url?.ToString();
                if (_owner.IsBlocked(url)) return true;
                if (url != null) _owner.Capture(url);
                return false;
            }

            public override void OnPageFinished(WebView? view, string? url)
            {
                base.OnPageFinished(view, url);
                if (url != null) _owner._address.Text = url;

                const string script = @"(function(){
                    if (window.__dpiCaptureInstalled) return;
                    window.__dpiCaptureInstalled = true;
                    const oldFetch = window.fetch;
                    window.fetch = function(input, init) {
                        try {
                            const u = typeof input === 'string' ? input : input.url;
                            if (window.DpiBridge) window.DpiBridge.capture(String(u));
                        } catch (_) {}
                        return oldFetch.apply(this, arguments);
                    };
                    const oldOpen = XMLHttpRequest.prototype.open;
                    XMLHttpRequest.prototype.open = function(method, url) {
                        try {
                            if (window.DpiBridge) window.DpiBridge.capture(String(url));
                        } catch (_) {}
                        return oldOpen.apply(this, arguments);
                    };
                })();";
                view?.EvaluateJavascript(script, null);
            }

            public override WebResourceResponse? ShouldInterceptRequest(WebView? view, IWebResourceRequest? request)
            {
                var url = request?.Url?.ToString();
                if (_owner.IsBlocked(url))
                    return new WebResourceResponse("text/plain", "utf-8", null);
                if (url != null) _owner.Capture(url);
                return null;
            }

            public override WebResourceResponse? ShouldInterceptRequest(WebView? view, string? url)
            {
                if (_owner.IsBlocked(url))
                    return new WebResourceResponse("text/plain", "utf-8", null);
                _owner.Capture(url);
                return null;
            }
        }

        private sealed class CaptureDownloadListener : Java.Lang.Object, IDownloadListener
        {
            private readonly BrowserActivity _owner;
            public CaptureDownloadListener(BrowserActivity owner) => _owner = owner;
            public void OnDownloadStart(string? url, string? userAgent, string? contentDisposition, string? mimetype, long contentLength)
                => _owner.CaptureDownload(url, userAgent, contentDisposition, mimetype, contentLength);
        }

        private sealed class JsBridge : Java.Lang.Object
        {
            private readonly BrowserActivity _owner;
            public JsBridge(BrowserActivity owner) => _owner = owner;
            [JavascriptInterface]
            [Export("capture")]
            public void Capture(string? url) => _owner.Capture(url);
        }

        private sealed class UBlockFilter
        {
            private readonly HashSet<string> _domains = new(StringComparer.OrdinalIgnoreCase);
            public int Count => _domains.Count;

            public async Task LoadAsync()
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(20);
                var urls = new[]
                {
                    "https://ublockorigin.github.io/uAssetsCDN/filters/filters.min.txt",
                    "https://ublockorigin.github.io/uAssetsCDN/filters/badware.min.txt",
                    "https://ublockorigin.github.io/uAssetsCDN/filters/privacy.min.txt",
                    "https://easylist.to/easylist/easylist.txt",
                    "https://easylist.to/easylist/easyprivacy.txt"
                };

                foreach (var listUrl in urls)
                {
                    try
                    {
                        var raw = await client.GetStringAsync(listUrl);
                        Parse(raw);
                    }
                    catch { }
                }
            }

            private void Parse(string raw)
            {
                foreach (var line0 in raw.Split('\n'))
                {
                    var line = line0.Trim();
                    if (line.Length == 0 || line[0] == '!' || line[0] == '[') continue;

                    if (line.StartsWith("||", StringComparison.Ordinal))
                    {
                        var value = line.Substring(2);
                        var end = value.IndexOfAny(new[] { '^', '/', '$', '*' });
                        if (end >= 0) value = value.Substring(0, end);
                        if (value.Length > 2 && value.IndexOf('.') > 0 && !value.Contains(' '))
                            _domains.Add(value.Trim('.'));
                    }
                    else if (!line.StartsWith("#") && line.Split(' ').Length >= 2)
                    {
                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && parts[0] is "0.0.0.0" or "127.0.0.1")
                            _domains.Add(parts[1].Trim('.'));
                    }
                }
            }

            public bool IsBlocked(string? url)
            {
                if (string.IsNullOrWhiteSpace(url)) return false;
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
                var host = uri.Host.TrimEnd('.');
                foreach (var domain in _domains)
                    if (host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
                        host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }
        }

        protected override void OnDestroy()
        {
            try
            {
                _webView?.StopLoading();
                _webView?.Destroy();
            }
            catch { }
            base.OnDestroy();
        }
    }
}
