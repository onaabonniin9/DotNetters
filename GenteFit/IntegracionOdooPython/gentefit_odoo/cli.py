from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from .config import Settings
from .odoo_client import OdooApiError, OdooJson2Client
from .sync_service import export_odoo_clients_to_xml, import_gentefit_batch
from .xml_contract import ContractError, parse_batch_file


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="gentefit-odoo",
        description="Intercambio mínimo de clientes entre GenteFit y Odoo 20.",
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    importar = subparsers.add_parser("importar", help="Importa en Odoo clientes de un XML GenteFit.")
    importar.add_argument("xml", type=Path, help="XML GenteFit -> Odoo.")

    exportar = subparsers.add_parser("exportar", help="Exporta desde Odoo los clientes de un lote GenteFit.")
    exportar.add_argument("xml_entrada", type=Path, help="XML original GenteFit -> Odoo.")
    exportar.add_argument("xml_salida", type=Path, help="Ruta del XML Odoo -> GenteFit que se generará.")
    exportar.add_argument("--nombre-lote", default=None, help="Nombre descriptivo del fichero de respuesta.")

    subparsers.add_parser("probar-conexion", help="Comprueba la conexión y los permisos básicos en Odoo.")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)

    try:
        settings = Settings.from_environment()
        client = OdooJson2Client(settings)
        try:
            if args.command == "probar-conexion":
                client.test_connection()
                print("Conexión con Odoo correcta.")
                return 0

            if args.command == "importar":
                batch = parse_batch_file(args.xml, expected_direction="GENTEFIT_A_ODOO")
                summary = import_gentefit_batch(client, batch)
                print(json.dumps({
                    "lote": batch.header.batch_id,
                    "leidos": summary.total,
                    "creados": summary.created,
                    "actualizados": summary.updated,
                    "errores": summary.failed,
                    "detalleErrores": summary.errors,
                }, ensure_ascii=False, indent=2))
                return 0 if summary.failed == 0 else 2

            if args.command == "exportar":
                original_batch = parse_batch_file(args.xml_entrada, expected_direction="GENTEFIT_A_ODOO")
                content = export_odoo_clients_to_xml(
                    client,
                    original_batch=original_batch,
                    batch_name=args.nombre_lote,
                )
                args.xml_salida.parent.mkdir(parents=True, exist_ok=True)
                args.xml_salida.write_bytes(content)
                print(f"XML de respuesta generado: {args.xml_salida}")
                return 0

            return 1
        finally:
            client.close()

    except (ValueError, OdooApiError, ContractError, OSError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())