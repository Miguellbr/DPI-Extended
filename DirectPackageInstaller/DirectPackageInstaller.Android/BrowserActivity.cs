using System;
using Android.App;
using Android.OS;
using Android.Webkit;
using Android.Widget;
using Android.Views;
using Android.Graphics;
using Android.Content;
using Java.Interop;

namespace DirectPackageInstaller.Android
{
    [Activity(Label = "DPI Browser", Exported = false)]
    public sealed class BrowserActivity : Activity
    {
        private WebView _webView = null!;
        private EditText _address = null!;
        private string _lastCandidate = "";

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var root = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical
            };

            var bar = new LinearLayout(this)
            {
                Orientation = Orientation.Horizontal
            };

            _address = new EditText(this)
            {
                Text = Intent?.DataString ?? "https://"
            };
            _address.SetSingleLine(true);
            _address.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1);

            var go = new Button(this) { Text = "Go" };
            var use = new Button(this) { Text = "Use" };

            bar.AddView(_address);
            bar.AddView(go);
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
        }

        private void Navigate()
        {
            var url = _address.Text?.Trim();
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url;

            _address.Text = url;
            _webView.LoadUrl(url);
        }

        private void Capture(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return;

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
            if (string.IsNullOrWhiteSpace(url))
                return;

            Capture(url);

            RunOnUiThread(() =>
            {
                var name = contentDisposition ?? string.Empty;
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
                if (request?.Url != null)
                    _owner.Capture(request.Url.ToString());

                return false;
            }

            public override void OnPageFinished(WebView? view, string? url)
            {
                base.OnPageFinished(view, url);

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
                if (request?.Url != null)
                    _owner.Capture(request.Url.ToString());

                return null;
            }

            public override WebResourceResponse? ShouldInterceptRequest(WebView? view, string? url)
            {
                _owner.Capture(url);
                return null;
            }
        }

        private sealed class CaptureDownloadListener : Java.Lang.Object, IDownloadListener
        {
            private readonly BrowserActivity _owner;

            public CaptureDownloadListener(BrowserActivity owner) => _owner = owner;

            public void OnDownloadStart(string? url, string? userAgent, string? contentDisposition, string? mimetype, long contentLength)
            {
                _owner.CaptureDownload(url, userAgent, contentDisposition, mimetype, contentLength);
            }
        }

        private sealed class JsBridge : Java.Lang.Object
        {
            private readonly BrowserActivity _owner;

            public JsBridge(BrowserActivity owner) => _owner = owner;

            [JavascriptInterface]
            [Export("capture")]
            public void Capture(string? url) => _owner.Capture(url);
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
