# DirectPackageInstaller Extended

**DPI-Extended** es una versión derivada de DirectPackageInstaller con captura de PKG desde el navegador, streaming remoto usando memoria, soporte RAR remoto → PKG y filtrado de red integrado de uBlock Origin.

[🇺🇸 English](README.MD) · [🇧🇷 Português](README.pt-BR.md)

## Características

Se mantienen las principales funciones de DirectPackageInstaller, incluyendo vista previa y descarga directa de PKG, instalación en PS4/PS5, Remote Package Installer, GoldHEN Payload Server, API DPIv2 de etaHEN, proxy, descargas segmentadas/reanudables, servicios de alojamiento compatibles, manifiestos de PSN/PKG divididos, CLI, JDownloader Click'n Load, conexión LAN en Windows y fuentes NFS.

### Añadido por DPI-Extended

- **Navegador Android integrado** para navegar por sitios de descarga
- **Captura automática de URLs de PKG/descarga** mediante navegación, Fetch/XHR, eventos de descarga del WebView y solicitudes iniciadas por el usuario
- Las URLs capturadas pueden entrar directamente en el flujo normal de instalación de DPI
- **Streaming segmentado usando memoria** con HTTP Range y RAM limitada, evitando almacenar el PKG remoto completo
- **uBlock Origin Static Network Filtering Engine (SNFE)** integrado en el navegador Android, con interruptor ON/OFF
- El filtrado de uBlock es opcional durante la ejecución, por lo que el navegador puede continuar si falla la inicialización de los filtros
- Soporte RAR remoto → PKG mediante PKGStream
- Streaming y caché progresivos mediante PKGStream
- No es necesario instalar RAR/unrar por separado para la ruta integrada

## Navegador Android

El navegador es un WebView orientado a descargas. Ofrece navegación atrás/adelante, actualizar, barra de URL, captura de descargas, captura automática de posibles PKG, interceptación Fetch/XHR para descargas iniciadas por JavaScript, protección contra capturas duplicadas e interruptor de filtrado uBlock.

No intenta capturar todas las solicitudes de red. Recursos estáticos como imágenes, CSS, fuentes y scripts quedan fuera de la lógica de captura, mientras que los candidatos a paquetes/descargas reciben prioridad.

## uBlock Origin

DPI-Extended incluye el **uBlock Origin Static Network Filtering Engine (SNFE)** oficial de `@gorhill/ubo-core` dentro de la aplicación Android. Es el motor de filtrado de red, no la extensión completa de uBlock Origin.

El código uBO incluido está bajo GPL-3.0-or-later. Consulta el proyecto upstream y la información de licencia incluida para más detalles.

## Streaming remoto

Las descargas remotas compatibles pueden usar solicitudes HTTP Range segmentadas con una caché en memoria de tamaño limitado. Esto permite consumir un PKG remoto sin escribir primero el archivo completo, que puede ocupar varios gigabytes, en el almacenamiento del teléfono.

```
PKG remoto → segmentos HTTP Range → caché RAM → flujo de instalación DPI → PS4/PS5
```

## RAR remoto → PKG

PKGStream puede resolver un RAR remoto, localizar PKG dentro de él y proporcionar el paquete seleccionado al flujo de instalación existente de DPI. No hace falta instalar una utilidad RAR/unrar separada para esta ruta.

## Cómo utilizarlo

### PKG directo
1. Inicia el servicio de instalación compatible con tu configuración.
2. Abre DPI-Extended.
3. Pega una URL directa de PKG o captura una con el navegador Android.
4. Selecciona **Open**.
5. Selecciona **Install** cuando el paquete esté listo.

### Captura desde el navegador
1. Abre el navegador integrado.
2. Navega hasta la página de descarga.
3. Activa normalmente el PKG/download.
4. DPI-Extended intenta capturar automáticamente la URL correspondiente.
5. Usa la URL capturada y continúa con el flujo normal de DPI.

### RAR remoto con un PKG
1. Pega la URL del RAR remoto en DPI-Extended.
2. Selecciona **Open**.
3. PKGStream analiza el archivo y localiza el PKG.
4. Si hay varios PKG, selecciona el deseado.
5. Continúa con el flujo normal de instalación.

## Servicios de alojamiento

El DPI original admite enlaces directos y servicios como AllDebrid, RealDebrid, DebridLink, Google Drive, MediaFire, PixelDrain y 1Fichier. El soporte RAR remoto también depende de que PKGStream pueda resolver y acceder al host del archivo.

## Instalación y releases

Descarga la versión más reciente desde la página de Releases de GitHub. Windows, Linux y Android utilizan .NET 8 según el paquete de destino; las builds de macOS son autocontenidas.

Consulta los assets de la release para conocer las arquitecturas Android publicadas actualmente por CI, en lugar de asumir que todas las arquitecturas históricas siguen disponibles.

**Importante:** un workflow verde de GitHub Actions confirma que el paquete se compiló y empaquetó correctamente. No significa que todas las funciones hayan sido probadas en hardware real. El navegador Android y la interceptación del WebView deben probarse en un dispositivo Android real.

## Compilación

1. Instala el SDK .NET 8.x.
2. Clona el repositorio con sus submódulos.
3. Ejecuta `Build.cmd`.
4. Los paquetes se colocan en `Release`.

La build de Android incluye el motor de filtrado uBlock utilizado por el navegador. PKGStream se incluye como submódulo Git.

## Estructura del proyecto

- `DirectPackageInstaller/` — aplicación principal de DPI
- `DirectPackageInstaller/DirectPackageInstaller.Android/` — aplicación Android e integración del navegador
- `DirectPackageInstaller/DirectPackageInstaller/IO/` — infraestructura de streaming
- `PKGStream/` — componente RAR remoto → PKG
- `Tools/` — auxiliares de empaquetado
- `Assets/ubocore.bundle.js` — motor de filtrado uBlock incluido para Android

## Créditos

DPI-Extended deriva de **DirectPackageInstaller** de **marcussacana**. Entre los principales componentes originales están DirectPackageInstaller, LibOrbisPkg, HttpServerLite, el template de payload, definiciones/internals de PS4 y componentes de OpenOrbis.

Las adiciones de DPI-Extended incluyen el navegador Android/captura de descargas, streaming segmentado usando memoria, integración de PKGStream e integración del uBlock Origin SNFE.

## Aviso

Este software está destinado a usos legítimos, como instalar software y copias de seguridad personales que tengas autorización para utilizar. Los autores no apoyan la piratería. Eres responsable del uso del software.