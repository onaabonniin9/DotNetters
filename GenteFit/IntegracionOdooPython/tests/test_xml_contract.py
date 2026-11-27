import unittest
import uuid
from datetime import datetime, timezone

from gentefit_odoo.xml_contract import (
    BatchHeader,
    ClientRecord,
    ContractError,
    build_odoo_response_xml,
    external_key,
    parse_batch_bytes,
)


class XmlContractTests(unittest.TestCase):
    def test_build_and_parse_odoo_response(self):
        related_id = str(uuid.uuid4())
        client = ClientRecord(
            id_gentefit=17,
            external_key=external_key(17),
            id_odoo=204,
            email="cliente@example.com",
            phone="600123123",
            street="Calle Mayor 1",
            city="Barcelona",
            zip_code="08001",
            active=True,
        )
        xml_bytes = build_odoo_response_xml([client], related_id, "Respuesta de prueba")
        batch = parse_batch_bytes(xml_bytes, expected_direction="ODOO_A_GENTEFIT")

        self.assertEqual(batch.header.related_batch_id, related_id)
        self.assertEqual(batch.header.origin, "ODOO")
        self.assertEqual(batch.header.destination, "GENTEFIT")
        self.assertEqual(batch.header.record_count, 1)
        self.assertEqual(batch.clients[0].id_gentefit, 17)
        self.assertEqual(batch.clients[0].id_odoo, 204)
        self.assertEqual(batch.clients[0].city, "Barcelona")

    def test_rejects_doctype(self):
        with self.assertRaises(ContractError):
            parse_batch_bytes(b'<!DOCTYPE x [<!ENTITY e "boom">]><x/>')

    def test_rejects_invalid_direction(self):
        batch_id = str(uuid.uuid4())
        xml = f'''<?xml version="1.0" encoding="utf-8"?>
<IntercambioGenteFitOdoo version="1.0">
  <Cabecera>
    <IdLote>{batch_id}</IdLote>
    <FechaGeneracionUtc>2026-11-27T10:00:00Z</FechaGeneracionUtc>
    <SistemaOrigen>GENTEFIT</SistemaOrigen>
    <SistemaDestino>ODOO</SistemaDestino>
    <Direccion>OTRA</Direccion>
    <TipoDatos>CLIENTES</TipoDatos>
    <CantidadRegistros>0</CantidadRegistros>
  </Cabecera>
  <Clientes />
</IntercambioGenteFitOdoo>'''.encode()
        with self.assertRaises(ContractError):
            parse_batch_bytes(xml)


if __name__ == "__main__":
    unittest.main()
