# DirectPackageInstaller Extended

**DPI-Extended** is a community-derived DirectPackageInstaller with browser-based PKG capture, memory-backed remote streaming, remote RAR → PKG support, and integrated uBlock Origin network filtering.

[🇧🇷 Português](README.pt-BR.md) · [🇪🇸 Español](README.es.md)

## Features

Everything from the original DirectPackageInstaller remains available, including PKG preview, direct PKG downloads, PS4/PS5 installation, Remote Package Installer, GoldHEN Payload Server, etaHEN DPIv2 API, proxying, segmented/resumable downloads, supported file hosts, PSN manifests/split PKGs, CLI, JDownloader Click'n Load, Windows LAN connection, and NFS sources.

### DPI-Extended additions

- **Android built-in browser** for navigating download sites
- **Automatic PKG/download URL capture** from navigation, Fetch/XHR, WebView download events, and user-initiated requests
- Captured URLs can be sent into the normal DPI installation flow
- **Memory-backed segmented streaming** using HTTP Range requests and bounded RAM, avoiding storage of the complete remote PKG
- **uBlock Origin Static Network Filtering Engine (SNFE)** integrated into the Android browser, with an ON/OFF toggle
- uBlock filtering is optional at runtime, so the browser can continue if filter initialization fails
- Remote RAR → PKG handling through PKGStream
- Progressive streaming and caching through PKGStream
- No separate RAR/unrar installation required for the integrated RAR path

## Android browser

The browser is a download-oriented WebView. It provides back/forward navigation, refresh, URL bar, download capture, automatic capture of likely package downloads, Fetch/XHR interception for JavaScript-driven downloads, duplicate-capture protection, and a uBlock filtering toggle.

It does not try to capture every network request. Static resources such as images, CSS, fonts, and scripts are excluded from capture logic while package/download candidates are prioritized.

## uBlock Origin

DPI-Extended bundles the official **uBlock Origin Static Network Filtering Engine (SNFE)** from `@gorhill/ubo-core` inside the Android application. This is the network-filtering engine, not the complete uBlock Origin browser extension.

The bundled uBO code is GPL-3.0-or-later. See the upstream project and included licensing information for details.

## Remote streaming

Supported remote downloads can use segmented HTTP Range requests backed by a bounded in-memory cache. This allows DPI to consume a remote PKG without first writing the complete multi-gigabyte file to phone storage.

```
Remote PKG → HTTP Range segments → bounded RAM cache → DPI installation stream → PS4/PS5
```

## Remote RAR → PKG

PKGStream can resolve a remote RAR, locate PKG entries inside it, and provide the selected package to the existing DPI installation flow. No separate RAR/unrar utility is required for this integrated path.

## How to use

### Direct PKG
1. Start the package installation service supported by your setup.
2. Open DPI-Extended.
3. Paste a direct PKG URL or capture one with the Android browser.
4. Select **Open**.
5. Select **Install** when the package is ready.

### Browser capture
1. Open the built-in browser.
2. Navigate to the download page.
3. Trigger the PKG/download normally.
4. DPI-Extended attempts to capture the relevant download URL automatically.
5. Use the captured URL and continue with the normal DPI flow.

### Remote RAR containing a PKG
1. Paste the remote RAR URL into DPI-Extended.
2. Select **Open**.
3. PKGStream inspects the archive and locates the PKG.
4. Select the desired PKG if there are multiple entries.
5. Continue with the normal DPI installation flow.

## Supported file-hosting services

The original DPI supports direct links and services such as AllDebrid, RealDebrid, DebridLink, Google Drive, MediaFire, PixelDrain, and 1Fichier. Remote RAR handling additionally depends on whether PKGStream can resolve and access the archive host.

## Installation and releases

Download the latest build from the [GitHub Releases page](https://github.com/Miguellbr/DPI-Extended/releases/latest). Windows, Linux, and Android use the .NET 8 runtime according to the target package; macOS builds are self-contained.

Check the release assets for the exact Android architectures currently published by CI rather than assuming every historical architecture is available.

**Important:** a green GitHub Actions workflow confirms that the package built and was packaged successfully. It does not mean every feature has been tested on real hardware. The Android browser/WebView interception should be tested on a real Android device.

## Building

1. Install the .NET 8.x SDK.
2. Clone the repository with its submodules.
3. Run `Build.cmd`.
4. Packages are placed in `Release`.

The Android build bundles the uBlock filtering engine used by the browser. PKGStream is included as a Git submodule.

## Project structure

- `DirectPackageInstaller/` — main DPI application
- `DirectPackageInstaller/DirectPackageInstaller.Android/` — Android application and browser integration
- `DirectPackageInstaller/DirectPackageInstaller/IO/` — streaming infrastructure
- `PKGStream/` — remote archive → PKG component
- `Tools/` — packaging helpers
- `Assets/ubocore.bundle.js` — bundled uBlock filtering engine for Android

## Credits

DPI-Extended is derived from **DirectPackageInstaller** by **marcussacana**. Major original components include DirectPackageInstaller, LibOrbisPkg, HttpServerLite, the payload template, PS4 internals/export definitions, and OpenOrbis components.

DPI-Extended additions include the Android browser/download capture, memory-backed segmented streaming, PKGStream integration, and uBlock Origin SNFE integration.

## Disclaimer

This software is intended for legitimate uses such as installing software and personal backups that you are authorized to use. The authors do not support piracy. You are responsible for how you use the software.