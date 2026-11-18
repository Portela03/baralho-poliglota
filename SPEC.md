# Baralho Poliglota — Especificação

App em **.NET MAUI** para aprender vocabulário de idiomas no estilo **Anki**: o usuário traduz palavras,
guarda em um baralho pessoal e revisa as cartas com **repetição espaçada** (frente/verso, avaliação
Errei/Difícil/Bom/Fácil, cartas "para hoje").

> Atividade de APIs (Prof. Wellington): mínimo de 2 APIs externas, 3 modos de navegação e POO.
> Uso obrigatório de pelo menos uma API da lista do aluno João Vitor Santos Portela
> (BrasilAPI, REST Countries, Deck of Cards API) — aqui, a **BrasilAPI**.

## 1. APIs utilizadas

| API | Origem | Autenticação | Uso no app |
|---|---|---|---|
| **BrasilAPI** (`brasilapi.com.br`) | Lista do João Portela | Nenhuma | Feriados nacionais do ano, que viram o baralho pronto "Feriados do Brasil" |
| **MyMemory Translation** (`api.mymemory.translated.net`) | Externa | Nenhuma (cota anônima) | Traduz palavras entre idiomas (e os nomes dos feriados) |
| **Free Dictionary API** (`api.dictionaryapi.dev`) | Externa | Nenhuma | Definição, classe gramatical, exemplo, fonética e áudio (palavras em inglês) |

Descartadas (verificadas em 18/11/2026): SpaceX API (fora do ar, HTTP 522), REST Countries (v3.1
desativada; a v5 exige chave de API — por isso não há mapa de países) e Deck of Cards API (funciona, mas
foi removida porque sortear cartas de baralho não tinha relação com o estudo).

### 1.1 Contratos usados

- `GET https://brasilapi.com.br/api/feriados/v1/{ano}`
  → `[{ date:"2026-09-07", name:"Independência do Brasil", type:"national", weekday:"segunda-feira" }]`
- `GET https://api.mymemory.translated.net/get?q={texto}&langpair={origem}|{destino}`
  → `{ responseData:{ translatedText }, responseStatus, matches:[{ translation }] }`
  (`responseStatus` pode vir como número ou texto)
- `GET https://api.dictionaryapi.dev/api/v2/entries/en/{palavra}`
  → lista de entradas; **404** quando a palavra não existe (não é erro do app).

## 2. Navegação (3 modos)

1. **Flyout (menu lateral):** *Baralho*, *Feriados do Brasil*, *Progresso*, *Sobre*.
2. **TabBar (abas inferiores)**, dentro do item *Baralho*: *Traduzir*, *Meu Baralho*, *Estudar*.
3. **Pilha (push/pop via `Shell.GoToAsync`)**:
   - Meu Baralho → **Detalhe da palavra** (definição completa, áudio, remover).
   - Estudar → **Sessão concluída** (resumo da revisão).

## 3. Telas

| Tela | Conteúdo | Regras |
|---|---|---|
| Traduzir | Campo de palavra, pickers de idioma, resultado com alternativas e definição, sugestões tocáveis (o MyMemory é colaborativo, então o usuário escolhe a tradução certa), "Guardar no baralho" | Idiomas: pt, en, es, fr, de, it. Origem ≠ destino. Último par é lembrado |
| Meu Baralho | Lista com busca, situação de estudo de cada carta (Nova / Para revisar hoje / Próxima revisão), favoritar (★) e excluir (🗑, com confirmação) | Sem duplicata (mesmo texto + mesmo par de idiomas) |
| Detalhe | Palavra, tradução, definição, exemplo, fonética, "Ouvir pronúncia", favoritar, remover | Áudio abre o link da pronúncia no sistema |
| Estudar | Início: contadores (novas, para hoje, maduras). Sessão: frente da carta → "Mostrar resposta" → verso e botões Errei/Difícil/Bom/Fácil com o intervalo previsto em cada um | Ver seção 4 |
| Sessão concluída | Quantidade de avaliações, lembradas, cartas que voltaram por erro | — |
| Feriados do Brasil | Idioma + ano; cria uma carta por feriado (nome traduzido na frente, português no verso, data no exemplo) com barra de andamento | Feriados repetidos (Carnaval) geram uma carta; reimportar não duplica |
| Progresso | Total, para hoje, novas, aprendendo, maduras (21+ dias), revisões, taxa de acerto | Dados locais |
| Sobre | APIs usadas, créditos, versão | — |

## 4. Regras do estudo (repetição espaçada, estilo SM-2)

- Cada carta guarda: facilidade (inicial 2,5, mínimo 1,3), repetições, intervalo em dias e data da
  próxima revisão. Carta sem data é **nova**; com data igual ou anterior a agora está **pendente**.
- **Sessão:** até 30 cartas pendentes (vencidas primeiro, depois novas).
- **Avaliações:**
  - **Errei:** zera as repetições e o intervalo, facilidade −0,20; a carta volta ao fim da fila da sessão.
  - **Difícil:** 1 dia na primeira vez; depois intervalo × 1,2 (mínimo +1 dia); facilidade −0,15.
  - **Bom:** 1 dia, depois 3 dias, depois intervalo × facilidade.
  - **Fácil:** 4 dias na primeira vez; depois intervalo × facilidade × 1,3 (mínimo +2 dias); facilidade +0,15.
- A sessão só termina quando todas as cartas forem lembradas pelo menos uma vez.
- Cada avaliação é gravada na hora: encerrar no meio não perde o que já foi avaliado.
- **Madura:** intervalo de 21 dias ou mais.

## 5. Arquitetura

```
BaralhoPoliglota.sln
├─ src/BaralhoPoliglota.Core   (net10.0)  Modelos, serviços de API, repositório, agendador e sessão de estudo
├─ src/BaralhoPoliglota        (MAUI)     Views (XAML), ViewModels (MVVM), Shell, DI
└─ tests/BaralhoPoliglota.Tests (xUnit)   Testes do Core (HTTP simulado, agendador, sessão, persistência)
```

- **MVVM** com `CommunityToolkit.Mvvm`; **injeção de dependência** em `MauiProgram`; serviços atrás de interfaces.
- Persistência local em JSON (`palavras.json`) no diretório de dados do app.

### 5.1 POO (pontos que a atividade pede)

- **Herança:** `Palavra` (abstrata) → `PalavraSalva`.
- **Encapsulamento:** o estado de repetição de `PalavraSalva` só muda por `Revisar`; `SessaoDeEstudo`
  controla a fila e os contadores.
- **Abstração/Interfaces:** `IFeriadoService`, `ITraducaoService`, `IDicionarioService`, `IPalavraRepository`.
- **Composição:** `ImportadorDeFeriados` usa `IFeriadoService` + `ITraducaoService` + `IPalavraRepository`;
  `SessaoDeEstudo` recebe a lista de palavras e o relógio.
- **Polimorfismo:** `Palavra.Descricao` é virtual e `PalavraSalva` a especializa.

## 6. Tratamento de erros

- Sem internet / timeout (10 s) / HTTP 5xx → mensagem clara na tela, sem travar; ação pode ser repetida.
- MyMemory com cota esgotada → mensagem específica.
- Na importação de feriados, falha numa tradução não derruba as outras (mostra quantas falharam).
- Dicionário 404 → mostra "Definição não encontrada", mas permite guardar a palavra.
- Arquivo JSON corrompido → começa com lista vazia.

## 7. Testes (xUnit)

- Parsing de cada API com respostas simuladas, incluindo 404, itens inválidos e `responseStatus` como texto.
- Agendador: intervalos crescentes, erro zera o ciclo, facilidade mínima, Fácil > Bom > Difícil, prever sem alterar.
- Sessão: ordem das cartas, limite, carta errada volta ao fim, sessão vazia.
- Importador de feriados: uma carta por nome, sem duplicar, falha parcial, idioma português recusado.
- Repositório: salvar, evitar duplicata, remover, arquivo corrompido, gravações simultâneas.

## 8. Critérios de aceite

1. Traduz uma palavra (MyMemory) e exibe definição (Free Dictionary) quando houver.
2. Guarda, lista, favorita e remove palavras; dados e agendamento persistem ao reabrir o app.
3. Cria o baralho de feriados a partir da BrasilAPI.
4. Sessão de estudo com frente/verso, quatro avaliações e intervalos previstos.
5. Os 3 modos de navegação funcionando (Flyout, TabBar, pilha).
6. `dotnet test` passa e `dotnet build` do app MAUI (Windows) compila sem erros.

## 9. Fora de escopo

Login, sincronização em nuvem, mapa de países (REST Countries exige chave), áudio embutido, idiomas sem
suporte do MyMemory, publicação nas lojas.
