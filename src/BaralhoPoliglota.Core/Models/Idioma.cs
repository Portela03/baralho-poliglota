namespace BaralhoPoliglota.Core.Models;

/// <summary>Idioma suportado pelo app (código ISO 639-1 usado pela API de tradução).</summary>
public sealed record Idioma(string Codigo, string Nome)
{
    public static readonly IReadOnlyList<Idioma> Suportados =
    [
        new("pt", "Português"),
        new("en", "Inglês"),
        new("es", "Espanhol"),
        new("fr", "Francês"),
        new("de", "Alemão"),
        new("it", "Italiano"),
    ];

    public static Idioma? Buscar(string? codigo) =>
        Suportados.FirstOrDefault(i => string.Equals(i.Codigo, codigo, StringComparison.OrdinalIgnoreCase));

    public static string NomeDe(string codigo) => Buscar(codigo)?.Nome ?? codigo;

    public override string ToString() => Nome;
}
