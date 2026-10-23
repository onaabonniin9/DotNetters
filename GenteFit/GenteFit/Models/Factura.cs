
namespace GenteFit.Models;

public class Factura
{
    // ATRIBUTOS

    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public EstadoFactura Status { get; set; } = EstadoFactura.BORRADOR;
    public Suscripcion Suscripcion { get; set; } = null!;
    public List<Pago> Pagos { get; set; } = new();

    /* MÉTODOS PROPIOS

    public bool Generar() { }

    public decimal CalcularTotal() { }

    public bool MarcarPagada() { }

    public EstadoFactura ConsultarEstado(){ }

    public List<Pago> ConsultarPagos() { }

    public bool Anular() { }

    public string GenerarDocumento() {}

    */
}
