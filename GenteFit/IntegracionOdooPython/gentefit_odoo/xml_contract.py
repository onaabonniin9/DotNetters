from __future__ import annotations

import re
import uuid
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path

VERSION = "1.0"
ROOT = "IntercambioGenteFitOdoo"
MAX_XML_BYTES = 5_000_000
EXTERNAL_KEY_PREFIX = "GENTEFIT-CLIENTE-"
_EXTERNAL_KEY_RE = re.compile(r"^GENTEFIT-CLIENTE-([1-9][0-9]*)$")


class ContractError(ValueError):
    """El fichero no respeta el contrato XML GenteFit-Odoo v1.0."""


@dataclass(frozen=True)
class ClientRecord:
    id_gentefit: int
    external_key: str
    id_odoo: int | None = None
    name: str | None = None
    surname: str | None = None
    email: str = ""
    phone: str = ""
    street: str = ""
    city: str = ""
    zip_code: str = ""
    active: bool = True


@dataclass(frozen=True)
class BatchHeader:
    batch_id: str
    batch_name: str | None
    related_batch_id: str | None
    generated_at_utc: datetime
    origin: str
    destination: str
    direction: str
    data_type: str
    record_count: int


@dataclass(frozen=True)
class Batch:
    header: BatchHeader
    clients: list[ClientRecord]
    version: str = VERSION


def external_key(client_id: int) -> str:
    if client_id <= 0:
        raise ContractError("IdGenteFit debe ser mayor que cero.")
    return f"{EXTERNAL_KEY_PREFIX}{client_id}"


def parse_batch_file(path: str | Path, expected_direction: str | None = None) -> Batch:
    xml_bytes = Path(path).read_bytes()
    return parse_batch_bytes(xml_bytes, expected_direction=expected_direction)


def parse_batch_bytes(xml_bytes: bytes, expected_direction: str | None = None) -> Batch:
    if not xml_bytes:
        raise ContractError("El fichero XML está vacío.")
    if len(xml_bytes) > MAX_XML_BYTES:
        raise ContractError("El fichero XML supera el tamaño máximo permitido.")

    upper = xml_bytes.upper()
    if b"<!DOCTYPE" in upper or b"<!ENTITY" in upper:
        raise ContractError("No se permiten declaraciones DTD ni entidades XML.")

    try:
        root = ET.fromstring(xml_bytes)
    except ET.ParseError as exc:
        raise ContractError(f"El XML está mal formado: {exc}") from exc

    if root.tag != ROOT:
        raise ContractError(f"La raíz XML debe ser {ROOT}.")

    version = root.attrib.get("version", "")
    if version != VERSION:
        raise ContractError(f"Versión XML no soportada: {version!r}.")

    header_element = root.find("Cabecera")
    clients_element = root.find("Clientes")
    if header_element is None or clients_element is None:
        raise ContractError("El XML debe contener Cabecera y Clientes.")

    batch_id = _required_text(header_element, "IdLote")
    related_id = _optional_text(header_element, "IdLoteRelacionado")
    try:
        uuid.UUID(batch_id)
    except ValueError as exc:
        raise ContractError("IdLote debe ser un UUID válido.") from exc

    if related_id:
        try:
            uuid.UUID(related_id)
        except ValueError as exc:
            raise ContractError("IdLoteRelacionado debe ser un UUID válido.") from exc

    generated_text = _required_text(header_element, "FechaGeneracionUtc")
    try:
        generated_at = datetime.fromisoformat(generated_text.replace("Z", "+00:00"))
    except ValueError as exc:
        raise ContractError("FechaGeneracionUtc debe usar fecha y hora ISO 8601.") from exc
    if generated_at.tzinfo is None or generated_at.utcoffset() is None:
        raise ContractError("FechaGeneracionUtc debe incluir zona horaria UTC.")
    if generated_at.utcoffset().total_seconds() != 0:
        raise ContractError("FechaGeneracionUtc debe expresarse en UTC.")

    origin = _required_text(header_element, "SistemaOrigen")
    destination = _required_text(header_element, "SistemaDestino")
    direction = _required_text(header_element, "Direccion")
    data_type = _required_text(header_element, "TipoDatos")
    try:
        record_count = int(_required_text(header_element, "CantidadRegistros"))
    except ValueError as exc:
        raise ContractError("CantidadRegistros debe ser un entero.") from exc

    if direction not in ("GENTEFIT_A_ODOO", "ODOO_A_GENTEFIT"):
        raise ContractError("Direccion debe ser GENTEFIT_A_ODOO u ODOO_A_GENTEFIT.")
    if expected_direction and direction != expected_direction:
        raise ContractError(f"Dirección incorrecta: se esperaba {expected_direction}.")
    expected_origin, expected_destination = (
        ("GENTEFIT", "ODOO") if direction == "GENTEFIT_A_ODOO" else ("ODOO", "GENTEFIT")
    )
    if origin != expected_origin or destination != expected_destination:
        raise ContractError("SistemaOrigen y SistemaDestino no corresponden con Direccion.")
    if data_type != "CLIENTES":
        raise ContractError("TipoDatos debe ser CLIENTES.")
    if record_count < 0:
        raise ContractError("CantidadRegistros no puede ser negativo.")
    if direction == "ODOO_A_GENTEFIT" and not related_id:
        raise ContractError("La respuesta Odoo -> GenteFit debe incluir IdLoteRelacionado.")

    header = BatchHeader(
        batch_id=batch_id,
        batch_name=_optional_text(header_element, "NombreLote"),
        related_batch_id=related_id,
        generated_at_utc=generated_at.astimezone(timezone.utc),
        origin=origin,
        destination=destination,
        direction=direction,
        data_type=data_type,
        record_count=record_count,
    )

    clients: list[ClientRecord] = []
    seen_ids: set[int] = set()
    seen_keys: set[str] = set()

    for element in clients_element.findall("Cliente"):
        try:
            client_id = int(_required_text(element, "IdGenteFit"))
        except ValueError as exc:
            raise ContractError("IdGenteFit debe ser un entero.") from exc
        key = _required_text(element, "ClaveExterna")
        if client_id <= 0 or key != external_key(client_id):
            raise ContractError(f"Clave externa incorrecta para el cliente {client_id}.")
        if client_id in seen_ids or key in seen_keys:
            raise ContractError(f"Cliente duplicado en el lote: {client_id}.")
        seen_ids.add(client_id)
        seen_keys.add(key)

        id_odoo_text = _optional_text(element, "IdOdoo")
        try:
            id_odoo = int(id_odoo_text) if id_odoo_text else None
        except ValueError as exc:
            raise ContractError(f"IdOdoo inválido para el cliente {client_id}.") from exc

        active = _parse_bool(_required_text(element, "Activo"), client_id)
        record = ClientRecord(
            id_gentefit=client_id,
            external_key=key,
            id_odoo=id_odoo,
            name=_optional_text(element, "Nombre"),
            surname=_optional_text(element, "Apellidos"),
            email=_optional_text(element, "CorreoElectronico") or "",
            phone=_optional_text(element, "Telefono") or "",
            street=_optional_text(element, "Direccion") or "",
            city=_optional_text(element, "Ciudad") or "",
            zip_code=_optional_text(element, "CodigoPostal") or "",
            active=active,
        )

        if direction == "GENTEFIT_A_ODOO":
            if not record.name or not record.surname:
                raise ContractError(f"El cliente {client_id} debe incluir Nombre y Apellidos.")
            if id_odoo is not None:
                raise ContractError(f"El envío a Odoo no debe incluir IdOdoo ({client_id}).")
        elif id_odoo is None or id_odoo <= 0:
            raise ContractError(f"La respuesta Odoo debe incluir IdOdoo válido ({client_id}).")

        clients.append(record)

    if record_count != len(clients):
        raise ContractError("CantidadRegistros no coincide con los elementos Cliente del XML.")

    return Batch(header=header, clients=clients, version=version)


def build_odoo_response_xml(
    clients: list[ClientRecord],
    related_batch_id: str,
    batch_name: str | None = None,
) -> bytes:
    try:
        uuid.UUID(related_batch_id)
    except ValueError as exc:
        raise ContractError("El IdLoteRelacionado debe ser un UUID válido.") from exc

    root = ET.Element(ROOT, {"version": VERSION})
    header = ET.SubElement(root, "Cabecera")
    _add_text(header, "IdLote", str(uuid.uuid4()))
    if batch_name:
        _add_text(header, "NombreLote", batch_name)
    _add_text(header, "IdLoteRelacionado", related_batch_id)
    _add_text(header, "FechaGeneracionUtc", datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z"))
    _add_text(header, "SistemaOrigen", "ODOO")
    _add_text(header, "SistemaDestino", "GENTEFIT")
    _add_text(header, "Direccion", "ODOO_A_GENTEFIT")
    _add_text(header, "TipoDatos", "CLIENTES")
    _add_text(header, "CantidadRegistros", str(len(clients)))

    clients_element = ET.SubElement(root, "Clientes")
    for client in clients:
        if client.id_odoo is None or client.id_odoo <= 0:
            raise ContractError(f"Falta IdOdoo para el cliente {client.id_gentefit}.")
        if client.external_key != external_key(client.id_gentefit):
            raise ContractError(f"Clave externa incorrecta para el cliente {client.id_gentefit}.")
        element = ET.SubElement(clients_element, "Cliente")
        _add_text(element, "IdGenteFit", str(client.id_gentefit))
        _add_text(element, "ClaveExterna", client.external_key)
        _add_text(element, "IdOdoo", str(client.id_odoo))
        _add_text(element, "CorreoElectronico", client.email)
        _add_text(element, "Telefono", client.phone)
        _add_text(element, "Direccion", client.street)
        _add_text(element, "Ciudad", client.city)
        _add_text(element, "CodigoPostal", client.zip_code)
        _add_text(element, "Activo", "true" if client.active else "false")

    ET.indent(root, space="  ")
    return ET.tostring(root, encoding="utf-8", xml_declaration=True)


def _required_text(parent: ET.Element, tag: str) -> str:
    element = parent.find(tag)
    if element is None or element.text is None or not element.text.strip():
        raise ContractError(f"Falta el elemento obligatorio {tag}.")
    return element.text.strip()


def _optional_text(parent: ET.Element, tag: str) -> str | None:
    element = parent.find(tag)
    if element is None or element.text is None:
        return None
    return element.text.strip()


def _parse_bool(value: str, client_id: int) -> bool:
    normalized = value.strip().lower()
    if normalized in ("true", "1"):
        return True
    if normalized in ("false", "0"):
        return False
    raise ContractError(f"Activo debe ser true/false para el cliente {client_id}.")


def _add_text(parent: ET.Element, tag: str, value: str) -> None:
    child = ET.SubElement(parent, tag)
    child.text = value