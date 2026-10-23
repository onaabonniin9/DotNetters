
namespace GenteFit.Models;

public class ListaEspera
{
    // ATRIBUTOS

    public int Id { get; set; }
    public int Position { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public EstadoListaEspera Status { get; set; } = EstadoListaEspera.EN_ESPERA;
    public Cliente Cliente { get; set; } = null!;
    public Sesion Sesion { get; set; } = null!;

    /* MÉTODOS PROPIOS

    public int ConsultarPosicion() { }

    public bool ActualizarPosicion(int nuevaPosicion){}

    public Reserva PromocionarAReserva() { }

    public bool AbandonarLista() {}

    public Cliente ConsultarCliente() { }

    public Sesion ConsultarSesion() { }

    */
}
