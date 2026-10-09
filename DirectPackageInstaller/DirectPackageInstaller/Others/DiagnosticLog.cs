using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DirectPackageInstaller.Others
{
    /// <summary>
    /// Small in-memory diagnostic log. Sensitive URL query values and API keys are redacted.
    /// </summary>
    public static class DiagnosticLog
    {
        private const int MaxEntries = 2000;
        private static readonly object Sync = new();
        private static readonly List<(DateTime Time, string Level, string Message)> Entries = new();

        public static event Action? Changed;

        public static void Info(string message) => Add("INFO", message);
        public static void Warn(string message) => Add("WARN", message);
        public static void Error(string message) => Add("ERROR", message);

        private static void Add(string level, string message)
        {
            var safe = Redact(message ?? string.Empty);
            lock (Sync)
            {
                Entries.Add((DateTime.Now, level, safe));
                if (Entries.Count > MaxEntries)
                    Entries.RemoveRange(0, Entries.Count - MaxEntries);
            }

            try { Changed?.Invoke(); } catch { }
        }

        public static string GetAll()
        {
            lock (Sync)
                return string.Join(Environment.NewLine, Entries.Select(Format));
        }

        public static string GetErrors()
        {
            lock (Sync)
                return string.Join(Environment.NewLine, Entries.Where(x => x.Level == "ERROR" || x.Level == "WARN").Select(Format));
        }

        public static void Clear()
        {
            lock (Sync)
                Entries.Clear();
            try { Changed?.Invoke(); } catch { }
        }

        private static string Format((DateTime Time, string Level, string Message) entry) =>
            $"[{entry.Time:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] {entry.Message}";

        private static string Redact(string value)
        {
            // Never preserve URL query strings, which commonly contain share tokens or API keys.
            value = Regex.Replace(value, @"https?://[^\s""'<>]+", match =>
            {
                if (!Uri.TryCreate(match.Value.TrimEnd('.', ',', ')', ']'), UriKind.Absolute, out var uri))
                    return "[URL redacted]";
                return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}?[query redacted]";
            }, RegexOptions.IgnoreCase);

            value = Regex.Replace(value,
                @"(?i)(api[_-]?key|token|authorization|password|secret)(\s*[:=]\s*)[^\s,;]+",
                "$1$2[redacted]");

            return value;
        }
    }
}
