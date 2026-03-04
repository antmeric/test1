using System.Text;
using System.Text.Json;
using HtmlAgilityPack;
using SiiTaxStatusApi.Models;

namespace SiiTaxStatusApi.Services;

public class SiiService
{
    private const string CaptchaUrl = "https://zeus.sii.cl/cvc_cgi/stc/CViewCaptcha.cgi";
    private const string QueryUrl = "https://zeus.sii.cl/cvc_cgi/stc/getstc";

    private readonly IHttpClientFactory _httpClientFactory;

    public SiiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<SituacionTributariaResponse> ConsultarAsync(string rut)
    {
        var (numero, dv) = RutValidator.Validar(rut);

        var client = _httpClientFactory.CreateClient("Sii");

        // Paso 1: Obtener y resolver captcha
        var (txtCaptcha, txtCode) = await ObtenerCaptchaAsync(client);

        // Paso 2: Consultar situación tributaria
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["RUT"] = numero,
            ["DV"] = dv,
            ["PRG"] = "STC",
            ["OPC"] = "NOR",
            ["txt_code"] = txtCode,
            ["txt_captcha"] = txtCaptcha
        });

        var response = await client.PostAsync(QueryUrl, formData);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        var resultado = ParsearRespuesta(html);
        resultado.Rut = $"{numero}-{dv}";

        return resultado;
    }

    private async Task<(string txtCaptcha, string txtCode)> ObtenerCaptchaAsync(HttpClient client)
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["oper"] = "0"
        });

        var response = await client.PostAsync(CaptchaUrl, formData);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var txtCaptcha = doc.RootElement.GetProperty("txtCaptcha").GetString()
            ?? throw new InvalidOperationException("No se pudo obtener el captcha del SII");

        // Decodificar captcha: bytes 36-40 del base64
        var decoded = Convert.FromBase64String(txtCaptcha);
        var txtCode = Encoding.ASCII.GetString(decoded, 36, 4);

        return (txtCaptcha, txtCode);
    }

    private static SituacionTributariaResponse ParsearRespuesta(string htmlContent)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        var resultado = new SituacionTributariaResponse();

        // Extraer razón social
        var nombreNode = doc.DocumentNode.SelectSingleNode("/html/body/div/div[4]");
        if (nombreNode != null)
            resultado.RazonSocial = nombreNode.InnerText.Trim();

        // Extraer actividades económicas
        var filasActividades = doc.DocumentNode.SelectNodes("/html/body/div/table[1]/tr");
        if (filasActividades != null)
        {
            foreach (var fila in filasActividades.Skip(1)) // Saltar header
            {
                var celdas = fila.SelectNodes("td");
                if (celdas is { Count: >= 4 })
                {
                    var actividad = new ActividadEconomica
                    {
                        Codigo = celdas[0].InnerText.Trim(),
                        Actividad = celdas[1].InnerText.Trim(),
                        Categoria = celdas[2].InnerText.Trim(),
                        AfectaIva = celdas[3].InnerText.Trim()
                    };

                    if (celdas.Count >= 5)
                        actividad.Fecha = celdas[4].InnerText.Trim();

                    resultado.Actividades.Add(actividad);
                }
            }
        }

        // Extraer documentos timbrados
        var filasDocumentos = doc.DocumentNode.SelectNodes("//table[@class='tabla']/tr");
        if (filasDocumentos != null)
        {
            foreach (var fila in filasDocumentos.Skip(1))
            {
                var celdas = fila.SelectNodes("td");
                if (celdas is { Count: >= 2 })
                {
                    resultado.DocumentosTimbrados.Add(new DocumentoTimbrado
                    {
                        Documento = celdas[0].InnerText.Trim(),
                        UltimoTimbraje = celdas[1].InnerText.Trim()
                    });
                }
            }
        }

        // Extraer campos de estado
        var spans = doc.DocumentNode.SelectNodes("//span");
        if (spans != null)
        {
            foreach (var span in spans)
            {
                var texto = span.InnerText.Trim();
                if (texto.Contains("Inicio de Actividades"))
                    resultado.InicioActividades = texto;
                else if (texto.Contains("Término de Giro"))
                    resultado.TerminoGiro = texto;
                else if (texto.Contains("Autorizado"))
                    resultado.AutorizacionDocumentos = texto;
            }
        }

        return resultado;
    }
}
