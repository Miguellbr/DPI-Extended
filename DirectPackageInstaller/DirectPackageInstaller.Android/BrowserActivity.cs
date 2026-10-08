using System;
using Android.App;
using Android.OS;
using Android.Webkit;
using Android.Widget;
using Android.Views;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views.InputMethods;
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
        private long _lastCandidateAt;
        private string _currentPageUrl = "";
        private bool _uBlockEnabled = true;
        private bool _autoCaptureEnabled = true;
        private long _lastUserGestureAt;
        private readonly UBlockEngine _uBlock = new();

        protected override async void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var root = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical,
                Background = new ColorDrawable(Color.ParseColor("#101114"))
            };

            // Compact browser chrome: navigation controls on the first row,
            // then a rounded omnibox that doubles as URL bar + Google search.
            var toolbar = new LinearLayout(this)
            {
                Orientation = Orientation.Horizontal,
            };
            toolbar.SetGravity(GravityFlags.CenterVertical);
            toolbar.SetPadding(10, 8, 10, 4);

            _back = MakeToolButton("‹");
            _forward = MakeToolButton("›");
            _refresh = MakeToolButton("↻");
            _ublock = MakeToolButton("🛡 ON");
            var use = MakeToolButton("Use");

            toolbar.AddView(_back);
            toolbar.AddView(_forward);
            toolbar.AddView(_refresh);
            toolbar.AddView(_ublock);
            toolbar.AddView(use);
            root.AddView(toolbar);

            var addressRow = new LinearLayout(this)
            {
                Orientation = Orientation.Horizontal,
            };
            addressRow.SetGravity(GravityFlags.CenterVertical);
            addressRow.SetPadding(10, 2, 10, 8);

            _address = new EditText(this)
            {
                Text = Intent?.DataString ?? "",
                Hint = "Pesquisar ou digitar URL",
                TextSize = 15
            };
            _address.SetTextColor(Color.ParseColor("#F1F3F4"));
            _address.SetHintTextColor(Color.ParseColor("#9AA0A6"));
            _address.SetSingleLine(true);
            _address.SetPadding(18, 0, 14, 0);
            _address.Background = RoundedBackground("#202124", "#3C4043", 22);
            _address.LayoutParameters = new LinearLayout.LayoutParams(0, 52, 1);
            addressRow.AddView(_address);

            var search = MakeToolButton("⌕");
            search.ContentDescription = "Pesquisar";
            addressRow.AddView(search);
            root.AddView(addressRow);

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

            search.Click += (_, _) => Navigate();
            _address.EditorAction += (_, e) =>
            {
                if (e.ActionId == ImeAction.Search || e.ActionId == ImeAction.Go || e.ActionId == ImeAction.Done)
                {
                    Navigate();
                    e.Handled = true;
                }
            };
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

            // uBlock is optional: a failure while loading its JS engine or filter
            // lists must never prevent the browser itself from opening.
            try
            {
                await _uBlock.LoadAsync();
                _uBlockEnabled = true;
                _ublock.Text = "🛡 ON";
                Toast.MakeText(this, $"uBlock: {_uBlock.Count} regras carregadas.", ToastLength.Short).Show();
            }
            catch (Exception ex)
            {
                _uBlockEnabled = false;
                _ublock.Text = "🛡 OFF";
                System.Diagnostics.Debug.WriteLine($"uBlock initialization failed: {ex}");
                Toast.MakeText(this, "uBlock indisponível. Browser continua funcionando.", ToastLength.Long).Show();
            }
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
            var input = _address.Text?.Trim();
            if (string.IsNullOrWhiteSpace(input)) return;

            string url;
            if (Uri.TryCreate(input, UriKind.Absolute, out var parsed) &&
                (string.Equals(parsed.Scheme, "http", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(parsed.Scheme, "https", StringComparison.OrdinalIgnoreCase)))
            {
                url = input;
            }
            else if (input.Contains(".") && !input.Contains(" "))
            {
                url = "https://" + input;
            }
            else
            {
                url = "https://www.google.com/search?q=" + Uri.EscapeDataString(input);
            }

            _address.Text = url;
            _webView.LoadUrl(url);
            _address.ClearFocus();
        }

        private Button MakeToolButton(string text)
        {
            var button = new Button(this)
            {
                Text = text,
                TextSize = 13,
                Background = RoundedBackground("#202124", "#202124", 18)
            };
            button.SetAllCaps(false);
            button.SetTextColor(Color.ParseColor("#E8EAED"));
            button.SetPadding(10, 0, 10, 0);
            button.LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent, 46) { RightMargin = 5 };
            return button;
        }

        private static GradientDrawable RoundedBackground(string fill, string stroke, int radius)
        {
            var drawable = new GradientDrawable();
            drawable.SetColor(Color.ParseColor(fill));
            drawable.SetCornerRadius(radius);
            drawable.SetStroke(1, Color.ParseColor(stroke));
            return drawable;
        }

        private bool IsBlocked(string? url, string type = "other") => _uBlockEnabled && _uBlock.IsBlocked(url, _currentPageUrl, type);

        private void MarkUserGesture()
        {
            _lastUserGestureAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private bool HasRecentUserGesture()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return now - _lastUserGestureAt <= 5000;
        }

        private void Capture(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return;

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (string.Equals(_lastCandidate, url, StringComparison.Ordinal) && now - _lastCandidateAt < 1500)
                return;

            _lastCandidate = url;
            _lastCandidateAt = now;
            RunOnUiThread(() =>
                Toast.MakeText(this, "Download capturado. Toque em Use.", ToastLength.Short).Show());
        }

        private static bool LooksLikeDownloadUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            var lower = url.ToLowerInvariant();
            var path = lower.Split('?', '#')[0];
            return path.EndsWith(".pkg", StringComparison.Ordinal) ||
                   lower.Contains(".pkg?", StringComparison.Ordinal) ||
                   lower.Contains(".pkg&", StringComparison.Ordinal) ||
                   path.Contains("/download/", StringComparison.Ordinal) ||
                   path.EndsWith("/download", StringComparison.Ordinal) ||
                   lower.Contains("download=1", StringComparison.Ordinal) ||
                   lower.Contains("download=true", StringComparison.Ordinal);
        }

        private static bool LooksLikeDownloadResponse(string? contentType, string? contentDisposition)
        {
            var type = contentType ?? string.Empty;
            var disposition = contentDisposition ?? string.Empty;
            return disposition.IndexOf("attachment", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   type.IndexOf("application/octet-stream", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   type.IndexOf("application/x-pkg", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   type.IndexOf("application/vnd.playstation", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool LooksLikeStaticResource(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return true;
            var path = url.Split('?', '#')[0].ToLowerInvariant();
            return path.EndsWith(".js", StringComparison.Ordinal) ||
                   path.EndsWith(".mjs", StringComparison.Ordinal) ||
                   path.EndsWith(".css", StringComparison.Ordinal) ||
                   path.EndsWith(".map", StringComparison.Ordinal) ||
                   path.EndsWith(".png", StringComparison.Ordinal) ||
                   path.EndsWith(".jpg", StringComparison.Ordinal) ||
                   path.EndsWith(".jpeg", StringComparison.Ordinal) ||
                   path.EndsWith(".gif", StringComparison.Ordinal) ||
                   path.EndsWith(".webp", StringComparison.Ordinal) ||
                   path.EndsWith(".svg", StringComparison.Ordinal) ||
                   path.EndsWith(".ico", StringComparison.Ordinal) ||
                   path.EndsWith(".woff", StringComparison.Ordinal) ||
                   path.EndsWith(".woff2", StringComparison.Ordinal) ||
                   path.EndsWith(".ttf", StringComparison.Ordinal) ||
                   path.EndsWith(".otf", StringComparison.Ordinal) ||
                   path.EndsWith(".mp3", StringComparison.Ordinal) ||
                   path.EndsWith(".mp4", StringComparison.Ordinal) ||
                   path.EndsWith(".webm", StringComparison.Ordinal);
        }

        private bool ShouldCaptureUserInitiatedRequest(IWebResourceRequest? request)
        {
            if (!_autoCaptureEnabled || request == null) return false;
            if (!HasRecentUserGesture()) return false;

            var url = request.Url?.ToString();
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase)) return false;
            if (LooksLikeStaticResource(url)) return false;

            return LooksLikeDownloadUrl(url) ||
                   url.IndexOf("download", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   url.IndexOf("pkg", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   url.IndexOf("fpkg", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   url.IndexOf("payload", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   url.IndexOf("/file", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void CaptureAuto(string? url, string? contentType = null, string? contentDisposition = null)
        {
            if (!_autoCaptureEnabled) return;
            if (LooksLikeDownloadUrl(url) || LooksLikeDownloadResponse(contentType, contentDisposition))
                Capture(url);
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
                if (_owner.IsBlocked(url, InferType(url))) return true;
                if (url != null) _owner.CaptureAuto(url);
                return false;
            }

            public override void OnPageFinished(WebView? view, string? url)
            {
                base.OnPageFinished(view, url);
                if (url != null) { _owner._address.Text = url; _owner._currentPageUrl = url; }

                const string script = @"(function(){
                    if (window.__dpiCaptureInstalled) return;
                    window.__dpiCaptureInstalled = true;

                    let lastUserGesture = 0;
                    document.addEventListener('click', function() {
                        lastUserGesture = Date.now();
                        try { window.DpiBridge && window.DpiBridge.markGesture(); } catch (_) {}
                    }, true);

                    function recentGesture() {
                        return Date.now() - lastUserGesture < 10000;
                    }

                    function maybeCapture(url, contentType, contentDisposition) {
                        try {
                            const text = String(url || '');
                            const lower = text.toLowerCase();
                            const obvious = lower.split(/[?#]/)[0].endsWith('.pkg') ||
                                lower.includes('.pkg?') || lower.includes('.pkg&') ||
                                lower.includes('/download/') ||
                                lower.endsWith('/download') ||
                                lower.includes('download=1') ||
                                lower.includes('download=true');
                            const attachment = String(contentDisposition || '').toLowerCase().includes('attachment');
                            const binary = String(contentType || '').toLowerCase().includes('application/octet-stream') ||
                                String(contentType || '').toLowerCase().includes('application/x-pkg') ||
                                String(contentType || '').toLowerCase().includes('application/vnd.playstation');
                            if ((obvious || attachment || binary) && (obvious || recentGesture() || attachment || binary))
                                window.DpiBridge && window.DpiBridge.capture(text);
                        } catch (_) {}
                    }

                    document.addEventListener('click', function(event) {
                        try {
                            const a = event.target && event.target.closest ? event.target.closest('a') : null;
                            if (!a) return;
                            const href = a.href || a.getAttribute('href');
                            if (href) maybeCapture(href, '', '');
                        } catch (_) {}
                    }, true);

                    const oldFetch = window.fetch;
                    window.fetch = function(input, init) {
                        const result = oldFetch.apply(this, arguments);
                        result.then(function(response) {
                            try {
                                maybeCapture(response.url || (typeof input === 'string' ? input : input.url),
                                    response.headers.get('content-type'),
                                    response.headers.get('content-disposition'));
                            } catch (_) {}
                        }).catch(function(){});
                        return result;
                    };

                    const oldOpen = XMLHttpRequest.prototype.open;
                    const oldSend = XMLHttpRequest.prototype.send;
                    XMLHttpRequest.prototype.open = function(method, url) {
                        try {
                            this.__dpiUrl = String(url);
                            this.__dpiMethod = String(method || 'GET').toUpperCase();
                        } catch (_) {}
                        return oldOpen.apply(this, arguments);
                    };
                    XMLHttpRequest.prototype.send = function() {
                        try {
                            this.addEventListener('load', function() {
                                maybeCapture(this.responseURL || this.__dpiUrl,
                                    this.getResponseHeader('Content-Type'),
                                    this.getResponseHeader('Content-Disposition'));
                            });
                        } catch (_) {}
                        return oldSend.apply(this, arguments);
                    };
                })();";
                view?.EvaluateJavascript(script, null);
            }

            private static string InferType(string? url)
            {
                if (string.IsNullOrWhiteSpace(url)) return "other";
                var path = url.Split('?')[0].ToLowerInvariant();
                if (path.EndsWith(".js") || path.EndsWith(".mjs")) return "script";
                if (path.EndsWith(".css")) return "stylesheet";
                if (path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".jpeg") || path.EndsWith(".gif") || path.EndsWith(".webp") || path.EndsWith(".svg")) return "image";
                if (path.EndsWith(".woff") || path.EndsWith(".woff2") || path.EndsWith(".ttf")) return "font";
                return "other";
            }

            public override WebResourceResponse? ShouldInterceptRequest(WebView? view, IWebResourceRequest? request)
            {
                var url = request?.Url?.ToString();
                if (_owner.IsBlocked(url, InferType(url)))
                    return new WebResourceResponse("text/plain", "utf-8", null);
                if (_owner.ShouldCaptureUserInitiatedRequest(request))
                    _owner.Capture(url);
                else if (url != null)
                    _owner.CaptureAuto(url);
                return null;
            }

            public override WebResourceResponse? ShouldInterceptRequest(WebView? view, string? url)
            {
                if (_owner.IsBlocked(url))
                    return new WebResourceResponse("text/plain", "utf-8", null);
                _owner.CaptureAuto(url);
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

            [JavascriptInterface]
            [Export("markGesture")]
            public void MarkGesture() => _owner.MarkUserGesture();
        }

        protected override void OnDestroy()
        {
            try
            {
                _webView?.StopLoading();
                _webView?.Destroy();
                _uBlock.Dispose();
            }
            catch { }
            base.OnDestroy();
        }
    }
}
