namespace GenteFit.Models;

public class Suscripcion
{
    // ATRIBUTOS

    public int Id { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public EstadoSuscripcion Status { get; set; } = EstadoSuscripcion.PENDIENTE;
    public int BillingDay { get; set; } // int que representa el día de cada mes que se cobra
    public DateTime? CancellationRequestedAt { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public PlanMensual PlanMensual { get; set; } = null!;

    /* MÉTODOS PROPIOS

    public void Activar() { }

    public bool CambiarPlan(int nuevoPlanId) { }

    public bool SolicitarBaja() {  }

    public bool ComprobarLimiteSemanal(int reservasUtilizadas) { }

    public EstadoSuscripcion ConsultarEstado() { }
    
    public PlanMensual ConsultarPlan() {}

    public bool ComprobarVigencia(DateOnly fecha) { }

    public decimal CalcularProximaCuota() { }

    public List<Pago> ConsultarPagos() { }

    public List<Factura> ConsultarFacturas() { }

    */
}
