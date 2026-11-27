from __future__ import annotations

from typing import Any
from urllib.parse import urljoin

import requests

from .config import Settings


class OdooApiError(RuntimeError):
    """Error devuelto por la API de Odoo."""


class OdooJson2Client:
    """Cliente pequeño para la API JSON-2 de Odoo 20."""

    MODEL = "res.partner"
    PARTNER_FIELDS = [
        "id", "ref", "is_company", "name", "email", "phone",
        "street", "city", "zip", "active",
    ]

    def __init__(self, settings: Settings):
        self.settings = settings
        self.base_url = f"{settings.odoo_url}/json/2/"
        self.session = requests.Session()
        self.session.headers.update({
            "Authorization": f"bearer {settings.api_key}",
            "Content-Type": "application/json; charset=utf-8",
            "Accept": "application/json",
            "User-Agent": "GenteFit-Integration/1.0",
        })
        if settings.database:
            self.session.headers["X-Odoo-Database"] = settings.database

    def close(self) -> None:
        self.session.close()

    def call(self, model: str, method: str, payload: dict[str, Any]) -> Any:
        url = urljoin(self.base_url, f"{model}/{method}")
        try:
            response = self.session.post(
                url,
                json=payload,
                timeout=self.settings.timeout_seconds,
            )
        except requests.RequestException as exc:
            raise OdooApiError(f"No se pudo conectar con Odoo: {exc}") from exc

        if not response.ok:
            detail = _extract_error(response)
            if response.status_code in (401, 403):
                detail = "Autenticación rechazada o el usuario no tiene permisos suficientes. " + detail
            elif response.status_code == 404:
                detail = "No se encuentra el endpoint JSON-2. Comprueba la versión y la URL de Odoo. " + detail
            raise OdooApiError(f"Odoo respondió HTTP {response.status_code}: {detail}")

        if not response.content:
            return None
        try:
            return response.json()
        except requests.JSONDecodeError as exc:
            raise OdooApiError("Odoo devolvió una respuesta que no es JSON válido.") from exc

    def search_read(
        self,
        domain: list[list[Any]],
        fields: list[str] | None = None,
        limit: int = 2,
        offset: int = 0,
        order: str = "id asc",
    ) -> list[dict[str, Any]]:
        result = self.call(self.MODEL, "search_read", {
            "domain": domain,
            "fields": fields or self.PARTNER_FIELDS,
            "limit": limit,
            "offset": offset,
            "order": order,
            "context": {"active_test": False},
        })
        if not isinstance(result, list):
            raise OdooApiError("La respuesta search_read de Odoo no contiene una lista de registros.")
        return result

    def create_partner(self, values: dict[str, Any]) -> None:
        self.call(self.MODEL, "create", {"vals_list": [values]})

    def update_partner(self, partner_id: int, values: dict[str, Any]) -> None:
        result = self.call(self.MODEL, "write", {
            "ids": [partner_id],
            "vals": values,
        })
        if result is False:
            raise OdooApiError(f"Odoo no ha actualizado el contacto {partner_id}.")

    def test_connection(self) -> None:
        result = self.call(self.MODEL, "search", {"domain": [], "limit": 1})
        if not isinstance(result, list):
            raise OdooApiError("La prueba de conexión no ha devuelto una lista de identificadores.")

    def find_partner_by_external_key(self, external_key: str) -> list[dict[str, Any]]:
        return self.search_read(
            domain=[["ref", "=", external_key]],
            fields=self.PARTNER_FIELDS,
            limit=2,
        )

    def find_partners_by_external_keys(self, external_keys: list[str]) -> list[dict[str, Any]]:
        """Busca solo los contactos incluidos en el lote original."""
        if not external_keys:
            return []
        return self.search_read(
            domain=[
                ["ref", "in", external_keys],
                ["is_company", "=", False],
            ],
            fields=self.PARTNER_FIELDS,
            limit=max(len(external_keys) * 2, 1),
            order="id asc",
        )


def _extract_error(response: requests.Response) -> str:
    try:
        payload = response.json()
        if isinstance(payload, dict):
            message = payload.get("message") or payload.get("error") or payload.get("name")
            if message:
                return str(message)[:1000]
    except requests.JSONDecodeError:
        pass
    return (response.text or response.reason or "Error sin detalle")[:1000]