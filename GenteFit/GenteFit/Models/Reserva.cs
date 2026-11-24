namespace GenteFit.Models;

public class Reserva
{

    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public EstadoReserva Status { get; set; } = EstadoReserva.CONFIRMADA;
    public Cliente Cliente { get; set; } = null!;
    public Sesion Sesion { get; set; } = null!;
}
