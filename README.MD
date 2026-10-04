# DirectPackageInstaller Extended

**DirectPackageInstaller Extended (DPI-Extended)** is a community-derived version of DirectPackageInstaller that keeps the original DPI installation flow while adding an extra capability: handling remote RAR archives that contain PKG files.

[🇧🇷 Português](README.pt-BR.md) · [🇪🇸 Español](README.es.md)

## What is DPI-Extended?

DPI-Extended keeps DirectPackageInstaller as the main installer and preserves its existing PS4/PS5 installation paths.

The main addition is **PKGStream** integration. It allows DPI to resolve a remote RAR, find a PKG inside it, and expose that PKG through the existing installation flow instead of requiring the user to install a RAR/unrar tool separately.

Conceptually:

```
Remote RAR URL
     │
     ▼
DPI-Extended
     │
     ▼
PKGStream
 ├─ resolve remote source
 ├─ open the RAR
 ├─ find the PKG
 └─ stream/cache the PKG
     │
     ▼
Existing DPI installation flow
     │
     ▼
PS4 / PS5
```

The interface remains intentionally simple: paste the URL and use **Open**. DPI-Extended decides whether the URL should be handled by the normal DPI path or by the integrated RAR-to-PKG path.

## Features

Everything from the original DirectPackageInstaller remains available, including:

- Preview PKG files
- Direct PKG downloads
- PS4 and PS5 package installation
- Remote Package Installer support
- GoldHEN Payload Server support
- etaHEN DPIv2 API support
- Automatic proxying
- RAR/7z compressed file support
- Resumable downloads where supported
- Segmented downloads
- Support for several file-hosting services
- PSN update manifests / split PKGs
- Command Line Interface (CLI)
- JDownloader Click'n Load support
- Automatic LAN connection on Windows
- NFS package sources

### DPI-Extended additions

- Remote RAR → PKG handling through PKGStream
- Finds PKG entries inside remote RAR archives
- Progressive streaming and caching through PKGStream
- No separate RAR/unrar installation required for the integrated path
- PKGStream is kept as an auxiliary component; DPI remains the installer
- Desktop releases can bundle the Node.js runtime required by PKGStream

## Supported file-hosting services

The original DPI supports:

- Any direct link (without authentication)
- AllDebrid ([API key required](https://alldebrid.com/apikeys/))
- RealDebrid ([API key required](https://real-debrid.com/apitoken))
- DebridLink ([API key required](https://debrid-link.com/webapp/apikey))
- Google Drive
- MediaFire
- PixelDrain
- 1Fichier

Remote RAR handling additionally depends on whether the archive host can be resolved and accessed by PKGStream.

## How to use

### Direct PKG

1. Start the package installation service supported by your setup.
2. Open DPI-Extended.
3. Paste a direct PKG URL into the URL field.
4. Click **Open**.
5. Click **Install** when the package is ready.

### Remote RAR containing a PKG

1. Open DPI-Extended.
2. Paste the URL of the remote RAR into the URL field.
3. Click **Open**.
4. DPI-Extended uses PKGStream to inspect the archive and locate the PKG.
5. Select the desired PKG if the archive contains multiple packages.
6. Continue with the normal DPI installation flow.

The user does not need to manually install a RAR extraction utility for this integrated path.

## How it works

DPI-Extended does not replace the existing installer or GoldHEN/RPI path.

When a remote RAR URL is detected, DPI-Extended asks the local PKGStream component to inspect the archive. PKGStream resolves the remote source, locates the requested PKG entry, and provides it as a normal byte stream to DPI.

DPI then continues using its existing package installation infrastructure.

This separation is intentional:

- **DPI** = user interface and installer
- **PKGStream** = remote archive access and PKG extraction/streaming
- **GoldHEN/RPI/etaHEN** = existing console-side installation mechanisms

## Installation

Download a build compatible with your platform.

Windows, Linux, and Android builds use the .NET 8 runtime according to the target package. macOS builds are self-contained.

> The integrated PKGStream runtime is packaged separately from the .NET application runtime. Desktop packages built for supported targets include the Node.js runtime and PKGStream dependencies needed by the integrated remote-RAR feature.

## Downloads

Build artifacts are currently produced by GitHub Actions. Release links will be added here when the distribution packages are published.

## Building

1. Install the .NET 8.x SDK.
2. Clone the repository with its submodules.
3. Run `Build.cmd`.
4. The compiled packages are placed in the `Release` directory.

PKGStream is included as a Git submodule and is prepared as part of the desktop packaging process.

## Project structure

- `DirectPackageInstaller/` — main DPI application
- `PKGStream/` — remote archive → PKG streaming component
- `Miguellbr/rar` — maintained RAR library fork used by PKGStream
- `Tools/` — packaging helpers for the integrated PKGStream runtime

## Credits

DPI-Extended is derived from **DirectPackageInstaller** by **marcussacana**.

Original project and major components:

- **DirectPackageInstaller** by marcussacana
- **LibOrbisPkg** by maxton
- **HttpServerLite** by jchristn
- Payload template by sleirsgoevy
- PS4 OS internals help by LM
- PS4 export definitions by OpenOrbis SDK

DPI-Extended additions:

- **PKGStream** integration
- **@mary/rar** based remote RAR handling
- Bundled PKGStream runtime packaging

## Disclaimer

This software is intended for legitimate uses such as installing software and personal backups that you are authorized to use. The authors do not support piracy. You are responsible for how you use the software.
