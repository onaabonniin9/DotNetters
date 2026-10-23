namespace GenteFit.Models;

public class Sesion
{
    // ATRIBUTOS

    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly HourStart { get; set; }
    public TimeOnly HourEnd { get; set; }
    public int MaxParticipants { get; set; } = 16;
    public EstadoSesion Status { get; set; } = EstadoSesion.PROGRAMADA;
    public Monitor Monitor { get; set; } = null!;
    public Sala Sala { get; set; } = null!;
    public Actividad Actividad { get; set; } = null!;

    /* MÉTODOS PROPIOS

    public int ConsultarPlazasLibres(){ }

    public List<Reserva> ConsultarReservasConfirmadas() { }

    public List<ListaEspera> ConsultarListaEspera() { }

    public Monitor ConsultarMonitor() { }

    public bool ComprobarDisponibilidad() { }

    public bool AsignarMonitor(int monitorId) { }

    public bool CambiarSala(int salaId) { }

    public bool ModificarHorario(DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin) { }

    public bool CancelarSesion(string motivo) { }

    */
}
