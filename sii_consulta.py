"""Módulo para consultar la situación tributaria de terceros en el SII."""

import base64
import json
import requests
from lxml import html


SII_CAPTCHA_URL = "https://zeus.sii.cl/cvc_cgi/stc/CViewCaptcha.cgi"
SII_QUERY_URL = "https://zeus.sii.cl/cvc_cgi/stc/getstc"


def validar_rut(rut_completo):
    """Valida un RUT chileno y retorna (numero, digito_verificador)."""
    rut_limpio = rut_completo.replace(".", "").replace("-", "").strip().upper()

    if len(rut_limpio) < 2:
        raise ValueError(f"RUT inválido: {rut_completo}")

    dv = rut_limpio[-1]
    numero = rut_limpio[:-1]

    if not numero.isdigit():
        raise ValueError(f"RUT inválido: {rut_completo}")

    # Calcular dígito verificador esperado
    suma = 0
    multiplicador = 2
    for digito in reversed(numero):
        suma += int(digito) * multiplicador
        multiplicador = multiplicador + 1 if multiplicador < 7 else 2

    resto = suma % 11
    dv_esperado = str(11 - resto)
    if dv_esperado == "11":
        dv_esperado = "0"
    elif dv_esperado == "10":
        dv_esperado = "K"

    if dv != dv_esperado:
        raise ValueError(
            f"Dígito verificador incorrecto para RUT {numero}: "
            f"esperado {dv_esperado}, recibido {dv}"
        )

    return numero, dv


def obtener_captcha(session):
    """Obtiene y resuelve el captcha del SII."""
    response = session.post(SII_CAPTCHA_URL, data={"oper": 0})
    response.raise_for_status()

    captcha_data = response.json()
    txt_captcha = captcha_data["txtCaptcha"]

    # Decodificar el captcha: los bytes 36-40 del base64 decodificado
    decoded = base64.b64decode(txt_captcha)
    txt_code = decoded[36:40].decode("ascii")

    return txt_captcha, txt_code


def consultar_situacion_tributaria(rut_completo):
    """Consulta la situación tributaria de un RUT en el SII.

    Args:
        rut_completo: RUT con dígito verificador (ej: "76632059-7")

    Returns:
        dict con la información tributaria del contribuyente
    """
    numero, dv = validar_rut(rut_completo)

    session = requests.Session()
    session.headers.update({
        "User-Agent": "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 "
                      "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    })

    txt_captcha, txt_code = obtener_captcha(session)

    response = session.post(SII_QUERY_URL, data={
        "RUT": numero,
        "DV": dv,
        "PRG": "STC",
        "OPC": "NOR",
        "txt_code": txt_code,
        "txt_captcha": txt_captcha,
    })
    response.raise_for_status()

    return parsear_respuesta(response.text)


def parsear_respuesta(html_content):
    """Parsea la respuesta HTML del SII y extrae los datos."""
    tree = html.fromstring(html_content)

    resultado = {
        "razon_social": None,
        "rut": None,
        "actividades": [],
        "documentos_timbrados": [],
    }

    # Extraer razón social
    nombre_elements = tree.xpath("/html/body/div/div[4]")
    if nombre_elements:
        texto = nombre_elements[0].text_content().strip()
        resultado["razon_social"] = texto

    # Extraer información de actividades económicas
    filas_actividades = tree.xpath("/html/body/div/table[1]/tr")
    for fila in filas_actividades[1:]:  # Saltar header
        celdas = fila.xpath("td")
        if len(celdas) >= 4:
            actividad = {
                "codigo": celdas[0].text_content().strip(),
                "actividad": celdas[1].text_content().strip(),
                "categoria": celdas[2].text_content().strip(),
                "afecta_iva": celdas[3].text_content().strip(),
            }
            if len(celdas) >= 5:
                actividad["fecha"] = celdas[4].text_content().strip()
            resultado["actividades"].append(actividad)

    # Extraer documentos timbrados
    filas_documentos = tree.xpath("//table[@class='tabla']/tr")
    for fila in filas_documentos[1:]:  # Saltar header
        celdas = fila.xpath("td")
        if len(celdas) >= 2:
            documento = {
                "documento": celdas[0].text_content().strip(),
                "ultimo_timbraje": celdas[1].text_content().strip(),
            }
            resultado["documentos_timbrados"].append(documento)

    # Extraer campos de estado adicionales
    spans = tree.xpath("//span")
    for span in spans:
        texto = span.text_content().strip()
        if "Inicio de Actividades" in texto:
            resultado["inicio_actividades"] = texto
        elif "Término de Giro" in texto:
            resultado["termino_giro"] = texto
        elif "Autorizado" in texto:
            resultado["autorizacion_documentos"] = texto

    return resultado
