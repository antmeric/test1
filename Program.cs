using SiiTaxStatusApi.Models;
using SiiTaxStatusApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("Sii", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
});
builder.Services.AddScoped<SiiService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/situacion-tributaria/{rut}", async (string rut, SiiService siiService) =>
{
    try
    {
        var resultado = await siiService.ConsultarAsync(rut);
        return Results.Ok(resultado);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ErrorResponse { Mensaje = ex.Message });
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem(
            detail: $"Error al conectar con el SII: {ex.Message}",
            statusCode: 502);
    }
})
.WithName("ConsultarSituacionTributaria")
.WithOpenApi()
.Produces<SituacionTributariaResponse>()
.Produces<ErrorResponse>(400)
.Produces(502);

app.Run();
