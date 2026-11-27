from __future__ import annotations
import os
from dataclasses import dataclass

@dataclass(frozen=True)
class Settings:
    odoo_url: str
    api_key: str
    database: str | None = None
    timeout_seconds: int = 30

    @classmethod
    def from_environment(cls) -> "Settings":
        url = os.getenv("GENTEFIT_ODOO_URL", "").strip().rstrip("/")
        api_key = os.getenv("GENTEFIT_ODOO_API_KEY", "").strip()
        database = os.getenv("GENTEFIT_ODOO_DATABASE", "").strip() or None
        timeout_raw = os.getenv("GENTEFIT_ODOO_TIMEOUT_SECONDS", "30").strip()

        if not url:
            raise ValueError("Falta la variable GENTEFIT_ODOO_URL.")
        if not api_key:
            raise ValueError("Falta la variable GENTEFIT_ODOO_API_KEY.")

        try:
            timeout = int(timeout_raw)
        except ValueError as exc:
            raise ValueError("GENTEFIT_ODOO_TIMEOUT_SECONDS debe ser un entero.") from exc
        if timeout < 1 or timeout > 300:
            raise ValueError("El timeout debe estar entre 1 y 300 segundos.")

        return cls(
            odoo_url=url,
            api_key=api_key,
            database=database,
            timeout_seconds=timeout,
        )