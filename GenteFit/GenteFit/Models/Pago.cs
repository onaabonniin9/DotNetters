
namespace GenteFit.Models;

public class Pago
{
    // ATRIBUTOS

    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public EstadoPago Status { get; set; } = EstadoPago.PENDIENTE;
    public MetodoPago PaymentMethod { get; set; } = MetodoPago.OTRO;
    public Suscripcion Suscripcion { get; set; } = null!;
    public Factura Factura { get; set; } = null!;

    /* MÉTODOS PROPIOS

    public bool IniciarCobro() { }

    public bool ConfirmarPago(int pagoId) { }

    public EstadoPago ConsultarEstado() { }

    public decimal ConsultarImporte() { }

    public Suscripcion ConsultarSuscripcion() { }

    public Factura ConsultarFactura() { }

    */
}
