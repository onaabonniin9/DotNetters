from __future__ import annotations

import re
from dataclasses import dataclass, field
from typing import Any

from .odoo_client import OdooApiError, OdooJson2Client
from .xml_contract import (
    Batch,
    ClientRecord,
    ContractError,
    build_odoo_response_xml,
    external_key,
)

_EXTERNAL_KEY_RE = re.compile(r"^GENTEFIT-CLIENTE-([1-9][0-9]*)$")


@dataclass
class ImportSummary:
    total: int = 0
    created: int = 0
    updated: int = 0
    failed: int = 0
    errors: list[str] = field(default_factory=list)

    @property
    def successful(self) -> int:
        return self.created + self.updated


def import_gentefit_batch(client: OdooJson2Client, batch: Batch) -> ImportSummary:
    """Crea/actualiza en Odoo los clientes de un lote GenteFit -> Odoo."""
    if batch.header.direction != "GENTEFIT_A_ODOO":
        raise ContractError("El módulo Python solo importa lotes GENTEFIT_A_ODOO en esta operación.")

    summary = ImportSummary(total=len(batch.clients))
    for record in batch.clients:
        try:
            _sync_one_client(client, record, summary)
        except (OdooApiError, ContractError, ValueError) as exc:
            summary.failed += 1
            summary.errors.append(f"Cliente GenteFit {record.id_gentefit}: {exc}")
    return summary


def _sync_one_client(client: OdooJson2Client, record: ClientRecord, summary: ImportSummary) -> None:
    expected_key = external_key(record.id_gentefit)
    if record.external_key != expected_key:
        raise ContractError("ClaveExterna no corresponde con IdGenteFit.")
    if not record.name or not record.surname:
        raise ContractError("Nombre y Apellidos son obligatorios para crear/actualizar el contacto.")

    matches = client.find_partner_by_external_key(record.external_key)
    if len(matches) > 1:
        raise ContractError("Hay más de un contacto Odoo con esta clave externa.")

    values = _partner_values(record)
    if not matches:
        client.create_partner(values)
        created = client.find_partner_by_external_key(record.external_key)
        if len(created) != 1:
            raise OdooApiError("Se ha enviado la creación, pero no se ha podido confirmar el contacto en Odoo.")
        summary.created += 1
        return

    existing = matches[0]
    if existing.get("is_company") is True:
        raise ContractError("La referencia externa ya está asignada a una empresa, no a una persona.")
    partner_id = existing.get("id")
    if not isinstance(partner_id, int) or partner_id <= 0:
        raise OdooApiError("Odoo ha devuelto un identificador de contacto no válido.")

    client.update_partner(partner_id, values)
    summary.updated += 1


def _partner_values(record: ClientRecord) -> dict[str, Any]:
    full_name = f"{record.name or ''} {record.surname or ''}".strip()
    return {
        "name": full_name,
        "ref": record.external_key,
        "is_company": False,
        "email": record.email or False,
        "phone": record.phone or False,
        "street": record.street or False,
        "city": record.city or False,
        "zip": record.zip_code or False,
        "active": record.active,
    }


def export_odoo_clients_to_xml(
    client: OdooJson2Client,
    original_batch: Batch,
    batch_name: str | None = None,
) -> bytes:
    """Genera la respuesta ODOO -> GENTEFIT para los clientes del lote original."""
    if original_batch.header.direction != "GENTEFIT_A_ODOO":
        raise ContractError("El fichero relacionado debe ser un lote GENTEFIT_A_ODOO.")

    expected_keys = {record.external_key for record in original_batch.clients}
    partners = client.find_partners_by_external_keys(sorted(expected_keys))
    records: list[ClientRecord] = []
    seen_ids: set[int] = set()
    seen_keys: set[str] = set()

    for partner in partners:
        key = _as_text(partner.get("ref"))
        if key not in expected_keys:
            continue
        match = _EXTERNAL_KEY_RE.fullmatch(key)
        if not match:
            raise ContractError(f"Referencia externa no válida devuelta por Odoo: {key}.")

        gentefit_id = int(match.group(1))
        odoo_id = partner.get("id")
        if not isinstance(odoo_id, int) or odoo_id <= 0:
            raise OdooApiError(f"Identificador Odoo inválido para la referencia {key}.")
        if gentefit_id in seen_ids or key in seen_keys:
            raise ContractError(f"Odoo contiene más de un contacto para el cliente GenteFit {gentefit_id}.")
        seen_ids.add(gentefit_id)
        seen_keys.add(key)

        records.append(ClientRecord(
            id_gentefit=gentefit_id,
            external_key=key,
            id_odoo=odoo_id,
            email=_as_text(partner.get("email")),
            phone=_as_text(partner.get("phone")),
            street=_as_text(partner.get("street")),
            city=_as_text(partner.get("city")),
            zip_code=_as_text(partner.get("zip")),
            active=bool(partner.get("active", True)),
        ))

    return build_odoo_response_xml(
        clients=records,
        related_batch_id=original_batch.header.batch_id,
        batch_name=batch_name or f"Respuesta de Odoo - {original_batch.header.batch_name or original_batch.header.batch_id}",
    )


def _as_text(value: Any) -> str:
    if value is False or value is None:
        return ""
    return str(value).strip()
