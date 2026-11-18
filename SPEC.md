# Baralho Poliglota — Especificação

App em **.NET MAUI** para aprender vocabulário de idiomas. O usuário traduz palavras, guarda num
"baralho" pessoal e joga rodadas em que as **cartas de um baralho real (API)** decidem a pergunta
e a pontuação.

> Atividade de APIs (Prof. Wellington): mínimo de 2 APIs externas, 3 modos de navegação e POO.
> Uso obrigatório de pelo menos uma API da lista do aluno João Vitor Santos Portela
> (BrasilAPI, REST Countries, **Deck of Cards API**).

## 1. APIs utilizadas

| API | Origem | Autenticação | Uso no app |
|---|---|---|---|
| **Deck of Cards API** (`deckofcardsapi.com`) | Lista do João Portela | Nenhuma | Cria/embaralha baralho e saca cartas durante o jogo |
| **MyMemory Translation** (`api.mymemory.translated.net`) | Externa | Nenhuma (cota anônima) | Traduz palavras entre idiomas |
| **Free Dictionary API** (`api.dictionaryapi.dev`) | Externa | Nenhuma | Definição, classe gramatical, exemplo, fonética e áudio (palavras em inglês) |

Nenhuma dessas APIs aparece na tabela de APIs dos demais alunos.

Descartadas (verificadas em 18/11/2026): SpaceX API (fora do ar, HTTP 522) e REST Countries
(v3.1 desativada; a v5 exige chave de API).

### 1.1 Contratos usados

- `GET https://deckofcardsapi.com/api/deck/new/shuffle/?deck_count=1`
  → `{ success, deck_id, remaining }`
- `GET https://deckofcardsapi.com/api/deck/{deck_id}/draw/?count=1`
  → `{ success, deck_id, cards:[{ code, image, value, suit }], remaining }`
- `GET https://api.mymemory.translated.net/get?q={texto}&langpair={origem}|{destino}`
  → `{ responseData:{ translatedText }, responseStatus, matches:[{ translation }] }`
  (`responseStatus` pode vir como número ou texto)
- `GET https://api.dictionaryapi.dev/api/v2/entries/en/{palavra}`
  → lista de entradas; **404** quando a palavra não existe (não é erro do app).

## 2. Navegação (3 modos)

1. **Flyout (menu lateral):** *Baralho*, *Placar*, *Sobre*.
2. **TabBar (abas inferiores)**, dentro do item *Baralho*: *Traduzir*, *Meu Baralho*, *Jogar*.
3. **Pilha (push/pop via `Shell.GoToAsync`)**:
   - Meu Baralho → **Detalhe da palavra** (definição completa, áudio, remover).
   - Jogar → **Resultado da partida** (pontuação, jogar de novo).

## 3. Telas

| Tela | Conteúdo | Regras |
|---|---|---|
| Traduzir | Campo de palavra, pickers de idioma de origem/destino, botão Traduzir, resultado com alternativas e definição, sugestões tocáveis (o MyMemory é colaborativo, então o usuário escolhe a tradução certa), botão "Guardar no baralho" | Idiomas: pt, en, es, fr, de, it. Origem ≠ destino. Último par escolhido é lembrado (`Preferences`) |
| Meu Baralho | Lista de palavras guardadas com busca; toque abre o detalhe; botões de favoritar (★) e excluir (🗑, com confirmação) | Não permite duplicata (mesmo texto + mesmo par de idiomas) |
| Detalhe | Palavra, tradução, idiomas, definição, exemplo, fonética, "Ouvir pronúncia", favoritar, remover | Áudio abre o link da pronúncia no navegador/player do sistema |
| Jogar | Mostra a carta sacada e a pergunta; resposta por múltipla escolha ou digitada | Ver seção 4 |
| Resultado | Pontos, acertos/rodadas, melhor sequência, botões "Jogar de novo" e "Voltar" | Grava a partida no placar |
| Placar | Estatísticas (partidas, melhor pontuação, média, taxa de acerto) e histórico | Dados locais |
| Sobre | APIs usadas, créditos, versão | — |

## 4. Regras do jogo

- **Pré-requisito:** pelo menos **4 palavras** no baralho (necessário para montar alternativas).
- Uma partida tem **10 rodadas**. A cada rodada o app saca **1 carta** da Deck of Cards API
  (baralho criado e embaralhado no início da partida).
- **Naipe define o tipo da pergunta:**
  - Copas ♥ e Ouros ♦ (vermelhas) → **múltipla escolha** (4 opções).
  - Espadas ♠ e Paus ♣ (pretas) → **digitar** a tradução.
- **Valor da carta define a pontuação:** `A = 11`, `2–10 = valor`, `J/Q/K = 10`.
- **A palavra sorteada** vem de `((valorNumerico − 1) × 4 + índiceDoNaipe + rodada) mod N`, avançando
  até achar uma palavra ainda não usada na partida (enquanto houver).
- **Acerto:** soma `pontosDaCarta + bônus`, com `bônus = min(sequênciaAtual − 1, 5)` quando a sequência
  é ≥ 2. **Erro:** 0 ponto e zera a sequência.
- **Comparação de texto digitado:** ignora maiúsculas/minúsculas, acentos e espaços nas pontas.
  Aceita também qualquer alternativa devolvida pela tradução.
- Se a Deck of Cards API falhar, o app mostra o erro e permite tentar de novo (não inventa cartas).

## 5. Arquitetura

```
BaralhoPoliglota.sln
├─ src/BaralhoPoliglota.Core   (net10.0)  Modelos, serviços de API, repositórios e regras do jogo
├─ src/BaralhoPoliglota        (MAUI)     Views (XAML), ViewModels (MVVM), Shell, DI
└─ tests/BaralhoPoliglota.Tests (xUnit)   Testes do Core (HTTP simulado, regras, persistência)
```

- **MVVM** com `CommunityToolkit.Mvvm`.
- **Injeção de dependência** em `MauiProgram`; serviços atrás de interfaces.
- Persistência local em JSON (`palavras.json`, `partidas.json`) no diretório de dados do app.

### 5.1 POO (pontos que a atividade pede)

- **Herança:** `Palavra` (abstrata) → `PalavraSalva`; `Pergunta` (abstrata) → `PerguntaMultiplaEscolha`,
  `PerguntaDigitada`.
- **Polimorfismo:** `Pergunta.Verificar(resposta)` e `Pergunta.Tipo` têm comportamento próprio em cada
  subclasse; a tela de jogo trata todas como `Pergunta`.
- **Abstração/Interfaces:** `IDicionarioService`, `ITraducaoService`, `IBaralhoService`,
  `IPalavraRepository`, `IPartidaRepository`.
- **Encapsulamento:** `Partida` controla pontos, sequência e rodadas por métodos; estado só muda por ali.
- **Composição:** `MotorDoJogo` usa `IBaralhoService` + lista de `PalavraSalva` para montar rodadas.

## 6. Tratamento de erros

- Sem internet / timeout (10 s) / HTTP 5xx → mensagem clara na tela, sem travar; ação pode ser repetida.
- MyMemory com cota esgotada (`quotaFinished`) → mensagem específica.
- Dicionário 404 → mostra "Definição não encontrada", mas permite guardar a palavra.
- Arquivos JSON corrompidos → começa com lista vazia (e não derruba o app).

## 7. Testes (xUnit)

- Parsing de cada API com respostas simuladas (`HttpMessageHandler` falso), incluindo 404 e
  `responseStatus` como texto.
- Regras do jogo: pontuação por carta, bônus de sequência, escolha de palavra sem repetição,
  tipo de pergunta por naipe, comparação sem acento.
- Repositórios: salvar, evitar duplicata, remover, arquivo corrompido.

## 8. Critérios de aceite

1. Traduz uma palavra (MyMemory) e exibe definição (Free Dictionary) quando houver.
2. Guarda, lista, favorita e remove palavras; dados persistem ao reabrir o app.
3. Partida de 10 rodadas usando cartas reais da Deck of Cards API, com as regras da seção 4.
4. Os 3 modos de navegação funcionando (Flyout, TabBar, pilha).
5. `dotnet test` passa e `dotnet build` do app MAUI (Windows) compila sem erros.

## 9. Fora de escopo

Login, sincronização em nuvem, áudio embutido (usa o link da pronúncia), idiomas sem suporte do
MyMemory, publicação nas lojas.
