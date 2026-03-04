# SII Tax Status API

API en .NET 8 para consultar la situación tributaria de un contribuyente en el SII (Servicio de Impuestos Internos de Chile) usando su RUT.

## Requisitos

- .NET 8 SDK

## Ejecutar

```bash
dotnet run
```

La API estará disponible en `http://localhost:5000` (o el puerto configurado).

## Endpoint

```
GET /api/situacion-tributaria/{rut}
```

### Ejemplo

```bash
curl http://localhost:5000/api/situacion-tributaria/76632059-7
```

### Respuesta

```json
{
  "rut": "76632059-7",
  "razonSocial": "EMPRESA EJEMPLO S.A.",
  "inicioActividades": "SI - 01/03/2010",
  "actividades": [
    {
      "codigo": "620100",
      "actividad": "ACTIVIDADES DE CONSULTORIA INFORMATICA",
      "categoria": "Primera",
      "afectaIva": "Si",
      "fecha": "01/03/2010"
    }
  ],
  "documentosTimbrados": []
}
```

### Swagger

Documentación interactiva disponible en `/swagger`.
