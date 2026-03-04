namespace SiiTaxStatusApi.Services;

public static class RutValidator
{
    public static (string numero, string dv) Validar(string rutCompleto)
    {
        var limpio = rutCompleto.Replace(".", "").Replace("-", "").Trim().ToUpper();

        if (limpio.Length < 2)
            throw new ArgumentException($"RUT inválido: {rutCompleto}");

        var dv = limpio[^1].ToString();
        var numero = limpio[..^1];

        if (!numero.All(char.IsDigit))
            throw new ArgumentException($"RUT inválido: {rutCompleto}");

        var dvEsperado = CalcularDv(numero);
        if (dv != dvEsperado)
            throw new ArgumentException(
                $"Dígito verificador incorrecto para RUT {numero}: esperado {dvEsperado}, recibido {dv}");

        return (numero, dv);
    }

    private static string CalcularDv(string numero)
    {
        var suma = 0;
        var multiplicador = 2;

        for (var i = numero.Length - 1; i >= 0; i--)
        {
            suma += (numero[i] - '0') * multiplicador;
            multiplicador = multiplicador < 7 ? multiplicador + 1 : 2;
        }

        var resto = suma % 11;
        var resultado = 11 - resto;

        return resultado switch
        {
            11 => "0",
            10 => "K",
            _ => resultado.ToString()
        };
    }
}
