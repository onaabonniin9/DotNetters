
using GenteFit.Data;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using GenteFit.Models;
using GenteFit.Services;

// Ona, Víctor y Andreu, si vuestro SQL Server tiene otro nombre, configurad la variable
// GENTEFIT_SQL_SERVER con vuestra instancia.
// Si usáis localhost\SQLEXPRESS, no tenéis que tocar nada.

// Configuración de SQL Server.
string servidor = Environment.GetEnvironmentVariable("GENTEFIT_SQL_SERVER")
    ?? @"localhost\SQLEXPRESS";

string cadenaConexion =
    $"Server={servidor};Database=GenteFitDB;Trusted_Connection=True;TrustServerCertificate=True;";

var opciones = new DbContextOptionsBuilder<GenteFitContext>()
    .UseSqlServer(cadenaConexion)
    .Options;

using var contexto = new GenteFitContext(opciones);
var clienteService = new ClienteService(contexto);

// Comprobar conexión con SQL Server.
bool conectado = contexto.Database.CanConnect();

Console.WriteLine(conectado
    ? "Conexión con SQL Server correcta."
    : "No se ha podido conectar con SQL Server.");

Console.WriteLine();
Console.WriteLine("================================");
Console.WriteLine("       GENTEFIT - XML");
Console.WriteLine("================================");
Console.WriteLine("1. Exportar clientes a XML");
Console.WriteLine("2. Importar clientes desde XML");
Console.WriteLine("0. Salir");
Console.WriteLine("================================");
Console.Write("Selecciona una opción: ");

string? opcion = Console.ReadLine();

switch (opcion)
{
    case "1":
        {
            Console.Write(
                "Introduce el nombre del archivo XML para exportar (ej. clientes.xml): ");

            string? nombreArchivo = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(nombreArchivo))
            {
                Console.WriteLine("Debes introducir un nombre de archivo.");
                break;
            }

            // Evitar que se utilicen rutas de carpetas.
            if (!string.Equals(
                    Path.GetFileName(nombreArchivo),
                    nombreArchivo,
                    StringComparison.Ordinal))
            {
                Console.WriteLine(
                    "Introduce solamente el nombre del archivo, sin carpetas.");
                break;
            }

            if (!nombreArchivo.EndsWith(
                    ".xml",
                    StringComparison.OrdinalIgnoreCase))
            {
                nombreArchivo += ".xml";
            }

            string rutaXml = Path.Combine(
                Environment.CurrentDirectory,
                nombreArchivo
            );

            var clientes = contexto.Clientes.ToList();

            XDocument documentoXml = new XDocument(
                new XElement("GenteFit",
                    new XElement("Clientes",
                        clientes.Select(cliente =>
                            new XElement("Cliente",
                                new XElement("Id", cliente.Id),
                                new XElement("Name", cliente.Name),
                                new XElement("Surname", cliente.Surname),
                                new XElement("Username", cliente.Username),
                                new XElement("Password", cliente.Password),
                                new XElement("Email", cliente.Email),
                                new XElement("Phone", cliente.Phone),
                                new XElement("Active", cliente.Active),
                                new XElement("DateCreated", cliente.DateCreated),
                                new XElement("DNI", cliente.DNI),
                                new XElement("BirthDate", cliente.BirthDate),
                                new XElement("Address", cliente.Address),
                                new XElement("City", cliente.City),
                                new XElement("PostalCode", cliente.PostalCode),
                                new XElement("IBAN", cliente.IBAN)
                            )
                        )
                    )
                )
            );

            documentoXml.Save(rutaXml);

            Console.WriteLine(
                $"Clientes exportados correctamente a: {rutaXml}");

            break;
        }

    case "2":
        {
            Console.Write(
                "Introduce el nombre del archivo XML para importar (ej. clientes.xml): ");

            string? nombreArchivo = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(nombreArchivo))
            {
                Console.WriteLine("Debes introducir un nombre de archivo.");
                break;
            }

            // Evitar que se utilicen rutas de carpetas.
            if (!string.Equals(
                    Path.GetFileName(nombreArchivo),
                    nombreArchivo,
                    StringComparison.Ordinal))
            {
                Console.WriteLine(
                    "Introduce solamente el nombre del archivo, sin carpetas.");
                break;
            }

            if (!nombreArchivo.EndsWith(
                    ".xml",
                    StringComparison.OrdinalIgnoreCase))
            {
                nombreArchivo += ".xml";
            }

            string rutaImportacion = Path.Combine(
                Environment.CurrentDirectory,
                nombreArchivo
            );

            if (!File.Exists(rutaImportacion))
            {
                Console.WriteLine(
                    $"No se encuentra el archivo XML: {rutaImportacion}");
                break;
            }

            XDocument xmlImportacion;

            try
            {
                xmlImportacion = XDocument.Load(rutaImportacion);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No se ha podido leer el XML: {ex.Message}");
                break;
            }

            var clientesImportados = xmlImportacion
                .Descendants("Cliente")
                .Select(elemento => new Cliente
                {
                    Name = (string?)elemento.Element("Name") ?? string.Empty,
                    Surname = (string?)elemento.Element("Surname") ?? string.Empty,
                    Username = (string?)elemento.Element("Username") ?? string.Empty,
                    Password = (string?)elemento.Element("Password") ?? string.Empty,
                    Email = (string?)elemento.Element("Email") ?? string.Empty,
                    Phone = (string?)elemento.Element("Phone") ?? string.Empty,
                    Active = (bool?)elemento.Element("Active") ?? true,
                    DateCreated = (DateTime?)elemento.Element("DateCreated")
                        ?? DateTime.UtcNow,
                    DNI = (string?)elemento.Element("DNI") ?? string.Empty,
                    BirthDate = (DateTime?)elemento.Element("BirthDate")
                        ?? DateTime.MinValue,
                    Address = (string?)elemento.Element("Address") ?? string.Empty,
                    City = (string?)elemento.Element("City") ?? string.Empty,
                    PostalCode = (string?)elemento.Element("PostalCode") ?? string.Empty,
                    IBAN = (string?)elemento.Element("IBAN") ?? string.Empty
                })
                .ToList();

            Console.WriteLine(
                $"Clientes leídos del XML: {clientesImportados.Count}");

            var clientesExistentes =
                await clienteService.ConsultarTodasAsync();

            Console.WriteLine(
                $"Clientes que ya existen en SQL Server: {clientesExistentes.Count}");

            var dniProcesados = clientesExistentes
                .Select(cliente => cliente.DNI)
                .Where(dni => !string.IsNullOrWhiteSpace(dni))
                .Select(dni => dni.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var clientesNuevos = new List<Cliente>();
            int clientesDuplicados = 0;

            foreach (var cliente in clientesImportados)
            {
                string dni = cliente.DNI.Trim();

                if (string.IsNullOrWhiteSpace(dni) ||
                    !dniProcesados.Add(dni))
                {
                    clientesDuplicados++;
                    continue;
                }

                clientesNuevos.Add(cliente);
            }

            Console.WriteLine(
                $"Clientes nuevos: {clientesNuevos.Count}");
            Console.WriteLine(
                $"Clientes omitidos por duplicidad o falta de DNI: {clientesDuplicados}");

            int importadosCorrectamente = 0;
            int erroresImportacion = 0;

            foreach (var cliente in clientesNuevos)
            {
                try
                {
                    await clienteService.CrearAsync(cliente);
                    importadosCorrectamente++;

                    Console.WriteLine(
                        $"Importado: {cliente.Name} {cliente.Surname}");
                }
                catch (Exception ex)
                {
                    erroresImportacion++;

                    Console.WriteLine(
                        $"Error al importar el DNI {cliente.DNI}: {ex.Message}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Resumen de la importación:");
            Console.WriteLine(
                $"Importados correctamente: {importadosCorrectamente}");
            Console.WriteLine(
                $"Omitidos por duplicidad o falta de DNI: {clientesDuplicados}");
            Console.WriteLine(
                $"Errores de importación: {erroresImportacion}");

            break;
        }

    case "0":
        Console.WriteLine("Saliendo de GenteFit.");
        break;

    default:
        Console.WriteLine("Opción no válida.");
        break;
}
