namespace GenteFit.Models;

public class Sesion
{

    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly HourStart { get; set; }
    public TimeOnly HourEnd { get; set; }
    public int MaxParticipants { get; set; } = 16;
    public EstadoSesion Status { get; set; } = EstadoSesion.PROGRAMADA;
    public Monitor Monitor { get; set; } = null!;
    public Sala Sala { get; set; } = null!;
    public Actividad Actividad { get; set; } = null!;
}
