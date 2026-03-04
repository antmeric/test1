# SII - Consulta Situación Tributaria de Terceros

Script en Python para consultar la situación tributaria de un contribuyente en el SII (Servicio de Impuestos Internos de Chile) usando su RUT.

## Instalación

```bash
pip install -r requirements.txt
```

## Uso

```bash
python main.py <RUT>
```

Ejemplo:

```bash
python main.py 76632059-7
```

El RUT puede ingresarse con o sin puntos y guión.

## Respuesta

El script retorna un JSON con la siguiente información:
- Razón social
- Actividades económicas
- Documentos timbrados
