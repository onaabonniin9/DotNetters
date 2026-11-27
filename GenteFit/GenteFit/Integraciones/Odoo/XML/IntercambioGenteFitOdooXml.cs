
// Los campos Nombre y Apellidos solo se envíande GenteFit a Odoo: res.partner utiliza un único campo name y no permite recuperar de forma fiable la separación original entre nombre y apellidos. IdOdoo solo se incluye en la respuesta de Odoo s GenteFit.
public sealed class ClienteIntercambioXml
{
    [XmlElement("IdGenteFit", Order = 0)]
    public int IdGenteFit { get; set; }

    [XmlElement("ClaveExterna", Order = 1)]
    public string ClaveExterna { get; set; } = string.Empty;

    [XmlElement("IdOdoo", Order = 2)]
    public int? IdOdoo { get; set; }

    [XmlElement("Nombre", Order = 3)]
    public string? Nombre { get; set; }

    [XmlElement("Apellidos", Order = 4)]
    public string? Apellidos { get; set; }

    [XmlElement("CorreoElectronico", Order = 5)]
    public string? CorreoElectronico { get; set; }

    [XmlElement("Telefono", Order = 6)]
    public string? Telefono { get; set; }

    [XmlElement("Direccion", Order = 7)]
    public string? Direccion { get; set; }

    [XmlElement("Ciudad", Order = 8)]
    public string? Ciudad { get; set; }

    [XmlElement("CodigoPostal", Order = 9)]
    public string? CodigoPostal { get; set; }

    [XmlElement("Activo", Order = 10)]
    public bool Activo { get; set; } = true;

    // Identificador externo determinista que se guarda en res.partner.ref en Odoo y permitte localizar el mismo contacto sin añadir una columna a la BD de GenteFit.
    public static string CrearClaveExterna(int idGenteFit)
    {
        if (idGenteFit <= 0)
            throw new ArgumentOutOfRangeException(nameof(idGenteFit), "El ID de GenteFit debe ser mayor que cero.");

        return $"GENTEFIT-CLIENTE-{idGenteFit}";
    }
}
