
namespace GenteFit.Models;

public class Cliente : Usuario
{

    public string DNI { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
}
