# DirectPackageInstaller Extended

**DPI-Extended** é uma versão derivada do DirectPackageInstaller com captura de PKGs pelo navegador, streaming remoto usando memória, suporte a RAR remoto → PKG e filtragem de rede integrada do uBlock Origin.

[🇺🇸 English](README.MD) · [🇪🇸 Español](README.es.md)

## Recursos

Todos os principais recursos do DirectPackageInstaller continuam disponíveis, incluindo visualização e download direto de PKGs, instalação em PS4/PS5, Remote Package Installer, GoldHEN Payload Server, API DPIv2 do etaHEN, proxy, downloads segmentados/retomáveis, serviços de hospedagem compatíveis, manifestos da PSN/PKGs divididos, CLI, JDownloader Click'n Load, conexão LAN no Windows e fontes NFS.

### Adições do DPI-Extended

- **Navegador Android integrado** para navegar em sites de download
- **Captura automática de URLs de PKG/download** por navegação, Fetch/XHR, eventos de download do WebView e requisições iniciadas pelo usuário
- URLs capturadas podem entrar diretamente no fluxo normal de instalação do DPI
- **Streaming segmentado usando memória** com HTTP Range e RAM limitada, evitando armazenar o PKG remoto completo
- **uBlock Origin Static Network Filtering Engine (SNFE)** integrado ao navegador Android, com botão ON/OFF
- A filtragem do uBlock é opcional em tempo de execução, então o navegador pode continuar se a inicialização dos filtros falhar
- Suporte a RAR remoto → PKG através do PKGStream
- Streaming e cache progressivos através do PKGStream
- Não é necessário instalar RAR/unrar separadamente para o fluxo integrado

## Navegador Android

O navegador é um WebView voltado para downloads. Ele oferece voltar/avançar, atualizar, barra de URL, captura de downloads, captura automática de possíveis PKGs, interceptação de Fetch/XHR para downloads acionados por JavaScript, proteção contra capturas duplicadas e botão de filtragem do uBlock.

Ele não tenta capturar todas as requisições. Recursos estáticos como imagens, CSS, fontes e scripts são excluídos da lógica de captura, enquanto candidatos a pacotes/downloads recebem prioridade.

## uBlock Origin

O DPI-Extended inclui o **uBlock Origin Static Network Filtering Engine (SNFE)** oficial de `@gorhill/ubo-core` dentro do aplicativo Android. Trata-se do mecanismo de filtragem de rede, não da extensão completa do uBlock Origin.

O código uBO incluído é GPL-3.0-or-later. Consulte o projeto upstream e as informações de licença incluídas para detalhes.

## Streaming remoto

Downloads remotos compatíveis podem usar requisições HTTP Range segmentadas apoiadas por um cache em memória com tamanho limitado. Isso permite consumir um PKG remoto sem gravar primeiro o arquivo inteiro, que pode ter vários gigabytes, no armazenamento do celular.

```
PKG remoto → segmentos HTTP Range → cache em RAM → fluxo de instalação do DPI → PS4/PS5
```

## RAR remoto → PKG

O PKGStream pode resolver um RAR remoto, localizar PKGs dentro dele e fornecer o pacote escolhido ao fluxo de instalação existente do DPI. Não é necessário instalar uma ferramenta RAR/unrar separadamente para esse fluxo.

## Como usar

### PKG direto
1. Inicie o serviço de instalação compatível com sua configuração.
2. Abra o DPI-Extended.
3. Cole uma URL direta de PKG ou capture uma usando o navegador Android.
4. Selecione **Open**.
5. Selecione **Install** quando o pacote estiver pronto.

### Captura pelo navegador
1. Abra o navegador integrado.
2. Navegue até a página do download.
3. Acione o PKG/download normalmente.
4. O DPI-Extended tenta capturar automaticamente a URL relevante.
5. Use a URL capturada e continue pelo fluxo normal do DPI.

### RAR remoto contendo um PKG
1. Cole a URL do RAR remoto no DPI-Extended.
2. Selecione **Open**.
3. O PKGStream analisa o arquivo e localiza o PKG.
4. Se houver vários PKGs, selecione o desejado.
5. Continue pelo fluxo normal de instalação.

## Serviços de hospedagem

O DPI original suporta links diretos e serviços como AllDebrid, RealDebrid, DebridLink, Google Drive, MediaFire, PixelDrain e 1Fichier. O suporte a RAR remoto também depende de o PKGStream conseguir resolver e acessar o host do arquivo.

## Instalação e releases

Baixe a versão mais recente pela página de Releases do GitHub. Windows, Linux e Android usam o runtime .NET 8 de acordo com o pacote do alvo; as builds do macOS são autocontidas.

Confira os assets da release para saber quais arquiteturas Android estão sendo publicadas atualmente pelo CI, em vez de assumir que todas as arquiteturas históricas continuam disponíveis.

**Importante:** um workflow verde do GitHub Actions confirma que o pacote foi compilado e empacotado com sucesso. Isso não significa que todos os recursos foram testados em hardware real. O navegador Android e a interceptação do WebView devem ser testados em um aparelho Android real.

## Compilação

1. Instale o SDK .NET 8.x.
2. Clone o repositório com seus submódulos.
3. Execute `Build.cmd`.
4. Os pacotes ficam em `Release`.

O build Android inclui o mecanismo de filtragem do uBlock usado pelo navegador. O PKGStream é incluído como submódulo Git.

## Estrutura do projeto

- `DirectPackageInstaller/` — aplicação principal do DPI
- `DirectPackageInstaller/DirectPackageInstaller.Android/` — aplicação Android e integração do navegador
- `DirectPackageInstaller/DirectPackageInstaller/IO/` — infraestrutura de streaming
- `PKGStream/` — componente RAR remoto → PKG
- `Tools/` — auxiliares de empacotamento
- `Assets/ubocore.bundle.js` — mecanismo de filtragem uBlock incluído para Android

## Créditos

O DPI-Extended é derivado do **DirectPackageInstaller** de **marcussacana**. Entre os principais componentes originais estão DirectPackageInstaller, LibOrbisPkg, HttpServerLite, o template de payload, definições/internals do PS4 e componentes do OpenOrbis.

As adições do DPI-Extended incluem o navegador Android/captura de downloads, streaming segmentado usando memória, integração com PKGStream e integração com o uBlock Origin SNFE.

## Aviso

Este software é destinado a usos legítimos, como instalação de software e backups pessoais que você tenha autorização para utilizar. Os autores não apoiam pirataria. Você é responsável pelo uso do software.