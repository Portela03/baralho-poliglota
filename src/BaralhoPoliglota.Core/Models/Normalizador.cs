using System.Globalization;
using System.Text;

namespace BaralhoPoliglota.Core.Models;

/// <summary>Normaliza textos para comparação: minúsculas, sem acentos e sem espaços extras.</summary>
public static class Normalizador
{
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var decomposto = texto.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        var ultimoFoiEspaco = false;

        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsWhiteSpace(c))
            {
                if (!ultimoFoiEspaco)
                    sb.Append(' ');
                ultimoFoiEspaco = true;
                continue;
            }

            sb.Append(char.ToLowerInvariant(c));
            ultimoFoiEspaco = false;
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
