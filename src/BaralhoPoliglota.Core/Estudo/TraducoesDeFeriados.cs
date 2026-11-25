using BaralhoPoliglota.Core.Models;

namespace BaralhoPoliglota.Core.Estudo;

/// <summary>
/// Tabela fixa com o nome dos feriados nacionais (como a BrasilAPI os devolve) em cada idioma suportado.
/// Existe porque o MyMemory erra nomes curtos (ex.: "Natal" → "allowance").
/// </summary>
public static class TraducoesDeFeriados
{
    // Ordem das colunas da tabela abaixo. Precisa vir antes dela: campos estáticos inicializam em ordem.
    private static readonly string[] Idiomas = ["en", "es", "fr", "de", "it"];

    // nome em português → (idioma → tradução)
    private static readonly Dictionary<string, Dictionary<string, string>> Tabela = Montar(
    [
        ("Confraternização mundial", "New Year's Day", "Año Nuevo", "Jour de l'An", "Neujahr", "Capodanno"),
        ("Carnaval", "Carnival", "Carnaval", "Carnaval", "Karneval", "Carnevale"),
        ("Sexta-feira Santa", "Good Friday", "Viernes Santo", "Vendredi saint", "Karfreitag", "Venerdì Santo"),
        ("Páscoa", "Easter", "Pascua", "Pâques", "Ostern", "Pasqua"),
        ("Tiradentes", "Tiradentes Day", "Día de Tiradentes", "Jour de Tiradentes", "Tiradentes-Tag", "Giorno di Tiradentes"),
        ("Dia do trabalho", "Labour Day", "Día del Trabajo", "Fête du Travail", "Tag der Arbeit", "Festa del Lavoro"),
        ("Corpus Christi", "Corpus Christi", "Corpus Christi", "Fête-Dieu", "Fronleichnam", "Corpus Domini"),
        ("Independência do Brasil", "Brazilian Independence Day", "Día de la Independencia de Brasil",
            "Fête de l'indépendance du Brésil", "Unabhängigkeitstag Brasiliens", "Giorno dell'indipendenza del Brasile"),
        ("Nossa Senhora Aparecida", "Our Lady of Aparecida", "Nuestra Señora de Aparecida",
            "Notre-Dame d'Aparecida", "Unsere Liebe Frau von Aparecida", "Nostra Signora di Aparecida"),
        ("Finados", "All Souls' Day", "Día de los Difuntos", "Jour des Morts", "Allerseelen", "Commemorazione dei defunti"),
        ("Proclamação da República", "Republic Proclamation Day", "Proclamación de la República",
            "Proclamation de la République", "Ausrufung der Republik", "Proclamazione della Repubblica"),
        ("Dia da consciência negra", "Black Awareness Day", "Día de la Conciencia Negra",
            "Journée de la conscience noire", "Tag des Schwarzen Bewusstseins", "Giornata della coscienza nera"),
        ("Natal", "Christmas", "Navidad", "Noël", "Weihnachten", "Natale"),
    ]);

    /// <summary>Tradução fixa do feriado, ou <c>null</c> se o nome ou o idioma não estiver na tabela.</summary>
    public static string? Buscar(string nomeEmPortugues, string idioma) =>
        Tabela.TryGetValue(Normalizador.Normalizar(nomeEmPortugues), out var porIdioma)
        && porIdioma.TryGetValue(idioma.ToLowerInvariant(), out var traducao)
            ? traducao
            : null;

    private static Dictionary<string, Dictionary<string, string>> Montar(
        (string Pt, string En, string Es, string Fr, string De, string It)[] linhas)
    {
        var resultado = new Dictionary<string, Dictionary<string, string>>();
        foreach (var l in linhas)
        {
            string[] valores = [l.En, l.Es, l.Fr, l.De, l.It];
            var porIdioma = new Dictionary<string, string>();
            for (var i = 0; i < Idiomas.Length; i++)
                porIdioma[Idiomas[i]] = valores[i];
            resultado[Normalizador.Normalizar(l.Pt)] = porIdioma;
        }
        return resultado;
    }
}
