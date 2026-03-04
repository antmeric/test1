#!/usr/bin/env python3
"""Consulta la situación tributaria de un contribuyente en el SII por RUT."""

import json
import sys

from sii_consulta import consultar_situacion_tributaria


def main():
    if len(sys.argv) < 2:
        print("Uso: python main.py <RUT>")
        print("Ejemplo: python main.py 76632059-7")
        sys.exit(1)

    rut = sys.argv[1]

    try:
        resultado = consultar_situacion_tributaria(rut)
        print(json.dumps(resultado, indent=2, ensure_ascii=False))
    except ValueError as e:
        print(f"Error de validación: {e}", file=sys.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"Error al consultar SII: {e}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
