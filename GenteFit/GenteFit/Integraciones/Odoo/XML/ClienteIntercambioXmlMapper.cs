using System;
using GenteFit.Models;

namespace GenteFit.Integraciones.Odoo.Xml;

public static class ClienteIntercambioXmlMapper
{
    public static ClienteIntercambioXml DesdeGenteFit(Cliente cliente)
    {
        ArgumentNullException.ThrowIfNull(cliente);

        if (cliente.Id <= 0)
            throw new ArgumentException("El cliente debe estar guardado y tener un ID válido.", nameof(cliente));

        return new ClienteIntercambioXml
        {
            IdGenteFit = cliente.Id,
            ClaveExterna = ClienteIntercambioXml.CrearClaveExterna(cliente.Id),
            Nombre = cliente.Name,
            Apellidos = cliente.Surname,
            CorreoElectronico = cliente.Email,
            Telefono = cliente.Phone,
            Direccion = cliente.Address,
            Ciudad = cliente.City,
            CodigoPostal = cliente.PostalCode,
            Activo = cliente.Active
        };
    }

    public static void AplicarDatosDeOdoo(Cliente cliente, ClienteIntercambioXml datosOdoo)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        ArgumentNullException.ThrowIfNull(datosOdoo);

        if (cliente.Id <= 0 || datosOdoo.IdGenteFit != cliente.Id)
            throw new ArgumentException("El registro XML no corresponde al cliente de GenteFit indicado.", nameof(datosOdoo));

        string claveEsperada = ClienteIntercambioXml.CrearClaveExterna(cliente.Id);
        if (!string.Equals(datosOdoo.ClaveExterna, claveEsperada, StringComparison.Ordinal))
            throw new ArgumentException("La clave externa del XML no coincide con el cliente de GenteFit.", nameof(datosOdoo));

        if (datosOdoo.IdOdoo is null or <= 0)
            throw new ArgumentException("La respuesta de Odoo debe incluir un IdOdoo válido.", nameof(datosOdoo));

        // Solo se copian campos de contacto definidos en el contrato de vuelta.
        cliente.Email = datosOdoo.CorreoElectronico ?? string.Empty;
        cliente.Phone = datosOdoo.Telefono ?? string.Empty;
        cliente.Address = datosOdoo.Direccion ?? string.Empty;
        cliente.City = datosOdoo.Ciudad ?? string.Empty;
        cliente.PostalCode = datosOdoo.CodigoPostal ?? string.Empty;
        cliente.Active = datosOdoo.Activo;
    }
}