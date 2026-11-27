import unittest
import uuid
from datetime import datetime, timezone

from gentefit_odoo.sync_service import export_odoo_clients_to_xml, import_gentefit_batch
from gentefit_odoo.xml_contract import (
    Batch, BatchHeader, ClientRecord, external_key, parse_batch_bytes,
)


class FakeOdooClient:
    def __init__(self, matches=None):
        self.matches = list(matches or [])
        self.created_values = []
        self.updated_values = []

    def find_partner_by_external_key(self, _key):
        return self.matches

    def create_partner(self, values):
        self.created_values.append(values)
        self.matches = [{"id": 71, "is_company": False}]

    def update_partner(self, partner_id, values):
        self.updated_values.append((partner_id, values))

    def find_partners_by_external_keys(self, keys):
        return [
            {
                "id": 71, "ref": keys[0], "is_company": False, "active": False,
                "email": "ana@example.com", "phone": False, "street": False,
                "city": "Barcelona", "zip": "08001",
            }
        ] if keys else []


class SyncServiceTests(unittest.TestCase):
    def _batch(self):
        record = ClientRecord(
            id_gentefit=5,
            external_key=external_key(5),
            name="Ana",
            surname="López",
            email="ana@example.com",
            active=True,
        )
        header = BatchHeader(
            batch_id=str(uuid.uuid4()),
            batch_name="Prueba",
            related_batch_id=None,
            generated_at_utc=datetime.now(timezone.utc),
            origin="GENTEFIT",
            destination="ODOO",
            direction="GENTEFIT_A_ODOO",
            data_type="CLIENTES",
            record_count=1,
        )
        return Batch(header=header, clients=[record])

    def test_creates_missing_contact(self):
        fake = FakeOdooClient()
        result = import_gentefit_batch(fake, self._batch())
        self.assertEqual(result.created, 1)
        self.assertEqual(result.failed, 0)
        self.assertEqual(fake.created_values[0]["name"], "Ana López")
        self.assertEqual(fake.created_values[0]["is_company"], False)

    def test_updates_existing_contact(self):
        fake = FakeOdooClient(matches=[{"id": 71, "is_company": False}])
        result = import_gentefit_batch(fake, self._batch())
        self.assertEqual(result.updated, 1)
        self.assertEqual(fake.updated_values[0][0], 71)
        self.assertEqual(result.failed, 0)

    def test_rejects_duplicate_external_reference(self):
        fake = FakeOdooClient(matches=[{"id": 71}, {"id": 72}])
        result = import_gentefit_batch(fake, self._batch())
        self.assertEqual(result.failed, 1)
        self.assertEqual(len(fake.created_values), 0)
        self.assertEqual(len(fake.updated_values), 0)

    def test_export_is_limited_to_original_batch(self):
        original = self._batch()
        fake = FakeOdooClient()
        xml_bytes = export_odoo_clients_to_xml(fake, original_batch=original)
        response = parse_batch_bytes(xml_bytes, expected_direction="ODOO_A_GENTEFIT")
        self.assertEqual(response.header.related_batch_id, original.header.batch_id)
        self.assertEqual(len(response.clients), 1)
        self.assertEqual(response.clients[0].id_gentefit, 5)
        self.assertEqual(response.clients[0].active, False)


if __name__ == "__main__":
    unittest.main()
