using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace GenteFit.Integraciones.Odoo.Xml;

public static class IntercambioXmlSerializer
{
    private const string VersionSoportada = "1.0";
    private const string TipoDatosSoportado = "CLIENTES";
    private const long MaximoCaracteresXml = 5_000_000;

    private static readonly XmlSerializer Serializer =
        new(typeof(IntercambioGenteFitOdooXml));

    public static string Serializar(IntercambioGenteFitOdooXml intercambio)
    {
        ArgumentNullException.ThrowIfNull(intercambio);
        ArgumentNullException.ThrowIfNull(intercambio.Cabecera);
        ArgumentNullException.ThrowIfNull(intercambio.Clientes);

        PrepararCabecera(intercambio);
        Validar(intercambio, intercambio.Cabecera.Direccion);

        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add(string.Empty, string.Empty);

        using var escritor = new Utf8StringWriter();
        using (var xmlWriter = XmlWriter.Create(escritor, new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            NewLineChars = Environment.NewLine
        }))
        {
            Serializer.Serialize(xmlWriter, intercambio, namespaces);
        }

        return escritor.ToString();
    }

    public static IntercambioGenteFitOdooXml Deserializar(
        string contenidoXml,
        DireccionIntercambioXml? direccionEsperada = null)
    {
        if (string.IsNullOrWhiteSpace(contenidoXml))
            throw new InvalidDataException("El contenido XML está vacío.");

        using var texto = new StringReader(contenidoXml);
        using var lector = XmlReader.Create(texto, CrearConfiguracionLector());

        if (Serializer.Deserialize(lector) is not IntercambioGenteFitOdooXml intercambio)
            throw new InvalidDataException("El documento no tiene la estructura de intercambio GenteFit-Odoo.");

        Validar(intercambio, direccionEsperada);
        return intercambio;
    }

    public static void GuardarArchivo(
        string rutaArchivo,
        IntercambioGenteFitOdooXml intercambio)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
            throw new ArgumentException("La ruta del archivo es obligatoria.", nameof(rutaArchivo));

        string contenido = Serializar(intercambio);
        File.WriteAllText(rutaArchivo, contenido, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public static IntercambioGenteFitOdooXml LeerArchivo(
        string rutaArchivo,
        DireccionIntercambioXml? direccionEsperada = null)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
            throw new ArgumentException("La ruta del archivo es obligatoria.", nameof(rutaArchivo));

        using var flujo = new FileStream(rutaArchivo, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var lector = XmlReader.Create(flujo, CrearConfiguracionLector());

        if (Serializer.Deserialize(lector) is not IntercambioGenteFitOdooXml intercambio)
            throw new InvalidDataException("El documento no tiene la estructura de intercambio GenteFit-Odoo.");

        Validar(intercambio, direccionEsperada);
        return intercambio;
    }

    public static void Validar(
        IntercambioGenteFitOdooXml intercambio,
        DireccionIntercambioXml? direccionEsperada = null)
    {
        ArgumentNullException.ThrowIfNull(intercambio);

        if (intercambio.Version != VersionSoportada)
            throw new InvalidDataException($"Versión XML no soportada: '{intercambio.Version}'. Se esperaba '{VersionSoportada}'.");

        if (intercambio.Cabecera is null)
            throw new InvalidDataException("Falta la cabecera del intercambio.");

        if (intercambio.Clientes is null)
            throw new InvalidDataException("Falta la colección Clientes.");

        CabeceraIntercambioXml cabecera = intercambio.Cabecera;

        if (!Guid.TryParse(cabecera.IdLote, out _))
            throw new InvalidDataException("IdLote debe contener un GUID válido.");

        if (cabecera.Direccion != DireccionIntercambioXml.GenteFitAOdoo &&
            cabecera.Direccion != DireccionIntercambioXml.OdooAGenteFit)
            throw new InvalidDataException("La dirección del intercambio no es válida.");

        if (direccionEsperada.HasValue && cabecera.Direccion != direccionEsperada.Value)
            throw new InvalidDataException($"Dirección incorrecta. Se esperaba '{direccionEsperada.Value}'.");

        if (!string.Equals(cabecera.TipoDatos, TipoDatosSoportado, StringComparison.Ordinal))
            throw new InvalidDataException($"TipoDatos debe ser '{TipoDatosSoportado}'.");

        if (cabecera.CantidadRegistros != intercambio.Clientes.Count)
            throw new InvalidDataException("CantidadRegistros no coincide con el número de clientes del XML.");

        bool esEnvioAOdoo = cabecera.Direccion == DireccionIntercambioXml.GenteFitAOdoo;
        string origenEsperado = esEnvioAOdoo ? "GENTEFIT" : "ODOO";
        string destinoEsperado = esEnvioAOdoo ? "ODOO" : "GENTEFIT";

        if (!string.Equals(cabecera.SistemaOrigen, origenEsperado, StringComparison.Ordinal) ||
            !string.Equals(cabecera.SistemaDestino, destinoEsperado, StringComparison.Ordinal))
            throw new InvalidDataException("SistemaOrigen y SistemaDestino no corresponden con la dirección declarada.");

        if (!esEnvioAOdoo && !Guid.TryParse(cabecera.IdLoteRelacionado, out _))
            throw new InvalidDataException("La respuesta Odoo -> GenteFit debe indicar un IdLoteRelacionado válido.");

        var idsGenteFit = new HashSet<int>();
        var clavesExternas = new HashSet<string>(StringComparer.Ordinal);

        foreach (ClienteIntercambioXml cliente in intercambio.Clientes)
        {
            if (cliente is null)
                throw new InvalidDataException("La colección contiene un cliente nulo.");

            if (cliente.IdGenteFit <= 0)
                throw new InvalidDataException("IdGenteFit debe ser mayor que cero.");

            string claveEsperada = ClienteIntercambioXml.CrearClaveExterna(cliente.IdGenteFit);
            if (!string.Equals(cliente.ClaveExterna, claveEsperada, StringComparison.Ordinal))
                throw new InvalidDataException($"ClaveExterna incorrecta para el cliente {cliente.IdGenteFit}.");

            if (!idsGenteFit.Add(cliente.IdGenteFit) || !clavesExternas.Add(cliente.ClaveExterna))
                throw new InvalidDataException($"El cliente {cliente.IdGenteFit} aparece duplicado en el lote.");

            if (esEnvioAOdoo)
            {
                if (string.IsNullOrWhiteSpace(cliente.Nombre) || string.IsNullOrWhiteSpace(cliente.Apellidos))
                    throw new InvalidDataException($"El cliente {cliente.IdGenteFit} debe incluir Nombre y Apellidos.");

                if (cliente.IdOdoo.HasValue)
                    throw new InvalidDataException($"El envío a Odoo no debe incluir IdOdoo para el cliente {cliente.IdGenteFit}.");
            }
            else if (cliente.IdOdoo is null or <= 0)
            {
                throw new InvalidDataException($"La respuesta de Odoo debe incluir un IdOdoo válido para el cliente {cliente.IdGenteFit}.");
            }
        }
    }

    private static void PrepararCabecera(IntercambioGenteFitOdooXml intercambio)
    {
        CabeceraIntercambioXml cabecera = intercambio.Cabecera;

        if (!Guid.TryParse(cabecera.IdLote, out _))
            cabecera.IdLote = Guid.NewGuid().ToString("D");

        cabecera.FechaGeneracionUtc = DateTime.UtcNow;
        cabecera.TipoDatos = TipoDatosSoportado;
        cabecera.CantidadRegistros = intercambio.Clientes.Count;

        if (cabecera.Direccion == DireccionIntercambioXml.GenteFitAOdoo)
        {
            cabecera.SistemaOrigen = "GENTEFIT";
            cabecera.SistemaDestino = "ODOO";
        }
        else
        {
            cabecera.SistemaOrigen = "ODOO";
            cabecera.SistemaDestino = "GENTEFIT";
        }
    }

    private static XmlReaderSettings CrearConfiguracionLector() => new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersInDocument = MaximoCaracteresXml,
        IgnoreComments = true,
        IgnoreWhitespace = true
    };

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}
