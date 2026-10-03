using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DirectPackageInstaller.IO;

namespace DirectPackageInstaller.Tasks
{
    public sealed class PKGStreamResult
    {
        public required Stream Stream { get; init; }
        public required string EntryName { get; init; }
        public required string StreamUrl { get; init; }
        public required long Size { get; init; }
        public required string[] Entries { get; init; }
    }

    public static class PKGStreamClient
    {
        private static readonly HttpClient Client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        public static string BaseUrl =>
            (Environment.GetEnvironmentVariable("PKGSTREAM_URL") ?? "http://127.0.0.1:8080").TrimEnd('/');

        public static async Task<PKGStreamResult?> TryOpenAsync(
            string archiveUrl,
            string? entryName = null,
            CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(archiveUrl, UriKind.Absolute, out var source) ||
                (source.Scheme != Uri.UriSchemeHttp && source.Scheme != Uri.UriSchemeHttps))
                return null;

            try
            {
                if (!await PKGStreamHost.EnsureStartedAsync(cancellationToken))
                    return null;

                var listUrl = $"{BaseUrl}/list?archive={Uri.EscapeDataString(archiveUrl)}";
                using var response = await Client.GetAsync(listUrl, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return null;

                await using var jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(jsonStream, cancellationToken: cancellationToken);

                if (!document.RootElement.TryGetProperty("entries", out var entriesElement))
                    return null;

                var entries = new List<(string Path, long Size)>();
                foreach (var item in entriesElement.EnumerateArray())
                {
                    if (item.TryGetProperty("directory", out var directory) && directory.GetBoolean())
                        continue;

                    if (!item.TryGetProperty("path", out var pathElement))
                        continue;

                    var path = pathElement.GetString();
                    if (string.IsNullOrWhiteSpace(path) ||
                        !path.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var size = item.TryGetProperty("size", out var sizeElement) &&
                               sizeElement.TryGetInt64(out var parsedSize)
                        ? parsedSize
                        : 0;

                    entries.Add((path, size));
                }

                if (entries.Count == 0)
                    return null;

                var selected = string.IsNullOrWhiteSpace(entryName)
                    ? entries[0]
                    : entries.FirstOrDefault(x => x.Path.Equals(entryName, StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrWhiteSpace(selected.Path))
                    throw new FileNotFoundException($"PKG entry not found in remote RAR: {entryName}");

                var streamUrl =
                    $"{BaseUrl}/stream?archive={Uri.EscapeDataString(archiveUrl)}&entry={Uri.EscapeDataString(selected.Path)}";

                var rawStream = await Client.GetStreamAsync(streamUrl, cancellationToken);
                var seekable = new ReadSeekableStream(rawStream, TempHelper.GetTempFile(Path.GetFileName(selected.Path)))
                {
                    ReportLength = selected.Size
                };

                return new PKGStreamResult
                {
                    Stream = seekable,
                    EntryName = selected.Path,
                    StreamUrl = streamUrl,
                    Size = selected.Size,
                    Entries = entries.Select(x => x.Path).ToArray()
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
