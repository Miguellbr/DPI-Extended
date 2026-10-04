# DirectPackageInstaller Extended

**DirectPackageInstaller Extended (DPI-Extended)** es una versión derivada de DirectPackageInstaller que mantiene el flujo original de instalación de DPI y añade una función adicional: trabajar con archivos RAR remotos que contienen archivos PKG.

[🇺🇸 English](README.MD) · [🇧🇷 Português](README.pt-BR.md)

## ¿Qué es DPI-Extended?

DPI sigue siendo el instalador principal. La principal incorporación es la integración con **PKGStream**.

Esto permite proporcionar a DPI un enlace a un RAR remoto, localizar un PKG dentro del archivo y entregarlo al flujo de instalación existente, sin exigir que el usuario instale por separado una herramienta RAR/unrar.

```
URL de RAR remoto
       │
       ▼
DPI-Extended
       │
       ▼
PKGStream
 ├─ resuelve el origen remoto
 ├─ abre el RAR
 ├─ encuentra el PKG
 └─ transmite/almacena el PKG
       │
       ▼
Flujo normal de DPI
       │
       ▼
PS4 / PS5
```

La interfaz sigue siendo sencilla: pega la URL y pulsa **Open**. DPI-Extended decide automáticamente qué ruta utilizar.

## Características

Se mantienen las principales funciones del DirectPackageInstaller original:

- Vista previa de archivos PKG
- Descargas directas de PKG
- Instalación en PS4 y PS5
- Remote Package Installer
- GoldHEN Payload Server
- API DPIv2 de etaHEN
- Proxy automático
- Archivos RAR/7z
- Descargas reanudables cuando son compatibles
- Descargas segmentadas
- Varios servicios de alojamiento
- Manifiestos de actualización de PSN / PKG divididos
- CLI
- JDownloader Click'n Load
- Conexión LAN automática en Windows
- Fuentes PKG mediante NFS

### Añadido por DPI-Extended

- RAR remoto → PKG mediante PKGStream
- Búsqueda de PKG dentro de RAR remotos
- Streaming progresivo y caché
- Sin instalación separada de RAR/unrar para esta ruta integrada
- PKGStream funciona como componente auxiliar; DPI sigue siendo el instalador
- Las builds de escritorio compatibles pueden incluir el runtime de Node.js necesario para PKGStream

## Cómo utilizarlo

### PKG directo

1. Inicia el servicio de instalación compatible con tu configuración.
2. Abre DPI-Extended.
3. Pega la URL directa del PKG.
4. Pulsa **Open**.
5. Pulsa **Install** cuando el paquete esté listo.

### RAR remoto con un PKG

1. Abre DPI-Extended.
2. Pega la URL del RAR remoto.
3. Pulsa **Open**.
4. DPI-Extended utiliza PKGStream para analizar el archivo y localizar el PKG.
5. Si hay varios PKG, selecciona el que quieras.
6. Continúa con el flujo normal de instalación de DPI.

## Cómo funciona

DPI-Extended no sustituye el instalador existente ni GoldHEN/RPI.

Cuando detecta un RAR remoto, DPI-Extended utiliza PKGStream para resolver el origen, localizar el PKG y proporcionarlo a DPI como un flujo normal de bytes.

La separación es intencional:

- **DPI** = interfaz e instalador
- **PKGStream** = acceso al archivo remoto y extracción/streaming del PKG
- **GoldHEN/RPI/etaHEN** = mecanismos existentes de instalación en la consola

## Compilación

1. Instala el SDK .NET 8.x.
2. Clona el repositorio con sus submódulos.
3. Ejecuta `Build.cmd`.
4. Los paquetes compilados aparecerán en `Release`.

## Créditos

DPI-Extended deriva de **DirectPackageInstaller**, creado por **marcussacana**.

Componentes originales:

- **DirectPackageInstaller** por marcussacana
- **LibOrbisPkg** por maxton
- **HttpServerLite** por jchristn
- Template de payload por sleirsgoevy
- Ayuda con internals de PS4 por LM
- Definiciones de exportación de PS4 por OpenOrbis SDK

Añadido por DPI-Extended:

- Integración con **PKGStream**
- Gestión de RAR remotos basada en **@mary/rar**
- Empaquetado del runtime de PKGStream

## Aviso

Este software está destinado a usos legítimos, como instalar software y copias de seguridad personales que tengas autorización para utilizar. Los autores no apoyan la piratería. Eres responsable del uso del software.
