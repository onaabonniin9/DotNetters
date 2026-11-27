# Integración Python GenteFit en Odoo
Módulo externo mínimo para intercambiar clientes mediante el contrato XML `IntercambioGenteFitOdoo` versión `1.0`.

## Funcionalidades
- `importar`: lee un XML `GENTEFIT_A_ODOO` y crea/actualiza contactos personales (`res.partner`) en Odoo.
- `exportar`: recupera de Odoo exclusivamente los contactos cuyo campo `ref` sigue el patrón `GENTEFIT-CLIENTE-{Id}` y genera un XML `ODOO_A_GENTEFIT`.
- `probar-conexion`: comprueba que la API responde y que el usuario puede consultar `res.partner`.

!!! No se intercambian contraseñas, nombres de usuario, DNI, fecha de nacimiento ni IBAN.

## Requisitos
- Python 3.10 o superior.
- Odoo 20.0 Community autohospedado, con un usuario técnico y permisos para leer/crear/modificar contactos.
- Una API key válida y acceso al endpoint JSON-2 de la instalación.
- Dependencias Python de `requirements.txt`.

## Instalación
Desde PowerShell, situado en esta carpeta:
```powershell
py -3 -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
pip install -r requirements.txt
```

## Configuración
Configura las variables de entorno en PowerShell. No introduzcas la API key en el XML ni en el repositorio:
```powershell
$env:GENTEFIT_ODOO_URL = "http://localhost:8069"
$env:GENTEFIT_ODOO_API_KEY = "PEGA_AQUI_LA_API_KEY"
$env:GENTEFIT_ODOO_DATABASE = "NOMBRE_BD"  # Opcional?
$env:GENTEFIT_ODOO_TIMEOUT_SECONDS = "30"
```
El usuario técnico debe tener permisos de lectura, creación y escritura en Contactos. Para minimizar privilegios, no utilices el usuario administrador habitual.

## Accionrs

Probar la conexión:
```powershell
python -m gentefit_odoo.cli probar-conexion
```

Importar un lote generado por GenteFit:
```powershell
python -m gentefit_odoo.cli importar "C:\GenteFit\Intercambio\clientes.xml"
```

Exportar desde Odoo los clientes que pertenecen al lote original:
```powershell
python -m gentefit_odoo.cli exportar "C:\GenteFit\Intercambio\clientes.xml" "C:\GenteFit\Intercambio\respuesta_odoo.xml"
```

El módulo lee el `IdLote` y la lista de clientes del XML original. La respuesta solo incluirá los contactos de Odoo que correspondan a esos clientes y guardará el `IdLote` original en `IdLoteRelacionado`.

## Correspondencia con Odoo

| XML | `res.partner` |
|---|---|
| `ClaveExterna` | `ref` |
| `Nombre` + `Apellidos` | `name` (concatenados) |
| `CorreoElectronico` | `email` |
| `Telefono` | `phone` |
| `Direccion` | `street` |
| `Ciudad` | `city` |
| `CodigoPostal` | `zip` |
| `Activo` | `active` |
| — | `is_company = false` |
| — | `id` devuelto como `IdOdoo` en la exportación |

La referencia `ref` tiene el formato `GENTEFIT-CLIENTE-{IdGenteFit}`. Si no existe se crea el contacto; si existe exactamente uno se actualiza; si existen varios, se informa del error y no se modifica ninguno.

## Notas

- La exportación desde Odoo solo devuelve los contactos del lote original que tengan la referencia externa correspondiente y `is_company = false`.
- La importación desde GenteFit no sincroniza contactos generales de Odoo.
- Cada llamada JSON-2 se ejecuta en su propia transacción. Por eso se informa del resultado por cliente y no se promete una transacción atómica para todo el fichero.
- Un fallo de conexión, autenticación, permisos o API se muestra como error y el proceso termina con código distinto de cero.
- Las claves API se guardan en variables de entorno no se escriben en XML, logs ni código fuente.

