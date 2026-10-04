# DirectPackageInstaller Extended

**DirectPackageInstaller Extended (DPI-Extended)** é uma versão derivada do DirectPackageInstaller que mantém o fluxo original de instalação do DPI e adiciona uma função extra: lidar com arquivos RAR remotos que contêm arquivos PKG.

[🇺🇸 English](README.MD) · [🇪🇸 Español](README.es.md)

## O que é o DPI-Extended?

O DPI continua sendo o instalador principal. A principal adição é a integração com o **PKGStream**.

Com ela, o DPI pode receber um link de um RAR remoto, localizar um PKG dentro desse RAR e disponibilizá-lo para o fluxo de instalação já existente, sem exigir que o usuário instale separadamente uma ferramenta como RAR/unrar.

```
Link do RAR remoto
       │
       ▼
DPI-Extended
       │
       ▼
PKGStream
 ├─ resolve a origem remota
 ├─ abre o RAR
 ├─ encontra o PKG
 └─ faz streaming/cache do PKG
       │
       ▼
Fluxo normal do DPI
       │
       ▼
PS4 / PS5
```

A interface continua simples: cole o link e clique em **Open**. O DPI-Extended decide automaticamente se deve usar o caminho normal ou o caminho integrado RAR → PKG.

## Recursos

Todos os principais recursos do DirectPackageInstaller original continuam disponíveis:

- Visualização de arquivos PKG
- Download direto de PKGs
- Instalação em PS4 e PS5
- Suporte ao Remote Package Installer
- Suporte ao GoldHEN Payload Server
- Suporte à API DPIv2 do etaHEN
- Proxy automático
- Suporte a arquivos RAR/7z
- Downloads retomáveis quando suportados
- Downloads segmentados
- Vários serviços de hospedagem
- Manifestos de atualização da PSN / PKGs divididos
- Interface de linha de comando (CLI)
- JDownloader Click'n Load
- Conexão LAN automática no Windows
- Fontes de PKG via NFS

### Adições do DPI-Extended

- Suporte a RAR remoto → PKG através do PKGStream
- Localização de PKGs dentro de RARs remotos
- Streaming progressivo e cache através do PKGStream
- Não é necessário instalar separadamente uma ferramenta RAR/unrar para esse fluxo
- O PKGStream é um componente auxiliar; o DPI continua sendo o instalador
- Builds desktop compatíveis podem incluir o runtime Node.js necessário pelo PKGStream

## Serviços de hospedagem

O DPI original suporta:

- Links diretos sem autenticação
- AllDebrid
- RealDebrid
- DebridLink
- Google Drive
- MediaFire
- PixelDrain
- 1Fichier

O suporte a RAR remoto também depende da possibilidade de o host ser resolvido e acessado pelo PKGStream.

## Como usar

### PKG direto

1. Inicie o serviço de instalação compatível com sua configuração.
2. Abra o DPI-Extended.
3. Cole o link direto do PKG no campo de URL.
4. Clique em **Open**.
5. Clique em **Install** quando o pacote estiver pronto.

### RAR remoto contendo um PKG

1. Abra o DPI-Extended.
2. Cole o link do RAR remoto no campo de URL.
3. Clique em **Open**.
4. O DPI-Extended usa o PKGStream para analisar o arquivo e localizar o PKG.
5. Se houver vários PKGs, selecione o desejado.
6. Continue pelo fluxo normal de instalação do DPI.

O usuário não precisa instalar manualmente uma ferramenta de extração RAR para esse fluxo integrado.

## Como funciona

O DPI-Extended não substitui o instalador existente nem o GoldHEN/RPI.

Quando um RAR remoto é detectado, o DPI-Extended utiliza o componente local PKGStream para analisar o arquivo. O PKGStream resolve a origem remota, localiza o PKG e o fornece ao DPI como um fluxo normal de bytes.

Depois disso, o DPI continua usando sua infraestrutura de instalação existente.

A separação é intencional:

- **DPI** = interface e instalador
- **PKGStream** = acesso ao arquivo remoto e extração/streaming do PKG
- **GoldHEN/RPI/etaHEN** = mecanismos existentes de instalação no console

## Instalação

Baixe uma build compatível com sua plataforma.

Windows, Linux e Android usam o runtime .NET 8 de acordo com o pacote do alvo. As builds do macOS são autocontidas.

> O runtime do PKGStream é separado do runtime .NET da aplicação. Os pacotes desktop compatíveis incluem o Node.js e as dependências necessárias para o recurso integrado de RAR remoto.

## Downloads

### Release mais recente

**O DPI-Extended v0.1.0 já está publicado como uma GitHub Release.**

[**Baixar a release mais recente — GitHub Releases**](https://github.com/Miguellbr/DPI-Extended/releases/latest)

A release contém builds para Windows, Linux, Android e macOS.

Para Android, escolha o pacote correspondente à arquitetura do seu aparelho (`ARM`, `ARM64`, `X64` ou `X86`) e extraia o APK do ZIP baixado.

### Estado real dos testes

> **IMPORTANTE:** o workflow estar verde significa que os pacotes foram compilados com sucesso. Isso **não significa que os aplicativos já foram testados em dispositivos reais**.
>
> - **Android:** o APK ainda aguarda teste em um dispositivo Android real.
> - **Windows:** ainda não testado fisicamente.
> - **Linux:** ainda não testado fisicamente.
> - **macOS:** ainda não testado fisicamente.
> - O mantenedor atualmente **não possui PC/Mac** para realizar os testes dessas plataformas.
>
> Portanto, o primeiro teste recomendado é instalar o APK em um Android real e verificar o funcionamento do DPI-Extended, especialmente o fluxo RAR remoto → PKG através do PKGStream.

O GitHub Actions é usado para compilar e publicar a release. A release publicada é o local recomendado para obter as builds atuais.

## Compilação

1. Instale o SDK .NET 8.x.
2. Clone o repositório com os submódulos.
3. Execute `Build.cmd`.
4. Os pacotes compilados serão colocados no diretório `Release`.

O PKGStream é incluído como submódulo Git e preparado durante o processo de empacotamento desktop.

## Estrutura do projeto

- `DirectPackageInstaller/` — aplicação principal do DPI
- `PKGStream/` — componente de streaming remoto RAR → PKG
- fork do `@mary/rar` — biblioteca RAR usada pelo PKGStream
- `Tools/` — scripts auxiliares de empacotamento

## Créditos

O DPI-Extended é derivado do **DirectPackageInstaller**, criado por **marcussacana**.

Componentes e projeto original:

- **DirectPackageInstaller** por marcussacana
- **LibOrbisPkg** por maxton
- **HttpServerLite** por jchristn
- Template de payload por sleirsgoevy
- Ajuda com internals do PS4 por LM
- Definições de exportação do PS4 pelo OpenOrbis SDK

Adições do DPI-Extended:

- Integração com **PKGStream**
- Tratamento de RAR remoto baseado em **@mary/rar**
- Empacotamento do runtime do PKGStream

## Aviso

Este software é destinado a usos legítimos, como instalação de software e backups pessoais que você tenha autorização para utilizar. Os autores não apoiam pirataria. Você é responsável pelo uso do software.
