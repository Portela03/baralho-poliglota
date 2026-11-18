# Baralho Poliglota

App **.NET MAUI** para aprender vocabulário: traduza palavras, guarde no seu baralho e revise com
repetição espaçada, como no Anki. Projeto da atividade de APIs (mín. 2 APIs externas, 3 modos de navegação, POO).

Especificação completa: [SPEC.md](SPEC.md).

Capturas das telas de tradução e baralho em [docs/screenshots](docs/screenshots).

## APIs

| API | Uso |
|---|---|
| [BrasilAPI](https://brasilapi.com.br) | Feriados nacionais, que viram um baralho pronto |
| [MyMemory Translation](https://mymemory.translated.net/doc/spec.php) | Tradução de palavras |
| [Free Dictionary API](https://dictionaryapi.dev) | Definição, fonética e áudio (inglês) |

Nenhuma exige chave de API.

## Navegação

Flyout (menu lateral) · TabBar (Traduzir, Meu Baralho, Estudar) · Pilha (detalhe da palavra e fim da sessão).

## Estrutura

```
src/BaralhoPoliglota.Core   Modelos, serviços de API, repositório, agendador e sessão de estudo (net10.0)
src/BaralhoPoliglota        App MAUI: Views (XAML), ViewModels (MVVM), Shell, DI
tests/BaralhoPoliglota.Tests  Testes xUnit do Core
```

## Como rodar

Requisitos: .NET 10 SDK com a workload MAUI (`dotnet workload install maui`).

```powershell
# testes
dotnet test tests/BaralhoPoliglota.Tests

# app no Windows
dotnet run --project src/BaralhoPoliglota -f net10.0-windows10.0.19041.0

# app no Android (emulador ou aparelho conectado)
dotnet build src/BaralhoPoliglota -f net10.0-android -t:Run
```

## Como estudar

1. Na aba **Traduzir**, traduza palavras e toque em **Guardar no baralho**. Ou abra **Feriados do Brasil**
   no menu lateral para criar um baralho pronto a partir da BrasilAPI.
2. Na aba **Estudar**, toque em *Estudar agora*. Você vê a frente da carta, pensa na resposta e toca em
   **Mostrar resposta**.
3. Avalie com **Errei / Difícil / Bom / Fácil**. Cada botão mostra quando a carta volta (ex.: 1 dia, 3 dias,
   2 meses). Cartas erradas voltam ao fim da sessão.
4. O menu **Progresso** mostra cartas novas, para hoje, aprendendo, maduras (21+ dias) e taxa de acerto.

## Observações

- O MyMemory é colaborativo: a primeira sugestão pode estar errada (ex.: `sun → eno`). Por isso o app
  mostra outras sugestões e deixa você tocar na certa antes de guardar.
- A cota gratuita do MyMemory é limitada por dia; quando acaba, o app avisa.
- O dicionário só tem palavras em inglês: as definições aparecem quando um dos idiomas é o inglês.
