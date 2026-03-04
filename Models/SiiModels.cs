namespace SiiTaxStatusApi.Models;

public class SituacionTributariaResponse
{
    public string? Rut { get; set; }
    public string? RazonSocial { get; set; }
    public string? InicioActividades { get; set; }
    public string? TerminoGiro { get; set; }
    public string? AutorizacionDocumentos { get; set; }
    public List<ActividadEconomica> Actividades { get; set; } = new();
    public List<DocumentoTimbrado> DocumentosTimbrados { get; set; } = new();
}

public class ActividadEconomica
{
    public string? Codigo { get; set; }
    public string? Actividad { get; set; }
    public string? Categoria { get; set; }
    public string? AfectaIva { get; set; }
    public string? Fecha { get; set; }
}

public class DocumentoTimbrado
{
    public string? Documento { get; set; }
    public string? UltimoTimbraje { get; set; }
}

public class ErrorResponse
{
    public string Mensaje { get; set; } = string.Empty;
}
