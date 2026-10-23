
namespace GenteFit.Models;

public class Cliente : Usuario
{
    // ATRIBUTOS

    public string DNI { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty; // Definir cómo lo protegemos??

    /* MÉTODOS PROPIOS

    public List<Sesion> ConsultarHorario(DateTime fecha) { }

    public Sesion ConsultarDetalleSesion(int sesionId) { }

    public Reserva ReservarSesion(int sesionId) { }

    public List<Reserva> ConsultarReservasActivas() { }

    public bool CancelarReserva(int reservaId) {}

    public ListaEspera ConsultarListaEspera(int sesionId) { }

    public int ConsultarPosicionEspera(int sesionId) { }

    public Suscripcion ConsultarSuscripcionActiva() { }

    public Suscripcion ContratarPlan(int planId) { }

    public bool CambiarPlan(int planId) { }

    public bool SolicitarBajaSuscripcion(){ }

    public List<Pago> ConsultarHistorialPagos() { }

    public List<Factura> ConsultarFacturas() { }

    public bool ActualizarDatosPersonales(string address, string city, string postalCode) { }

    */
}
