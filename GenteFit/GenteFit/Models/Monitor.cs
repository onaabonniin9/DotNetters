
namespace GenteFit.Models;

public class Monitor : Usuario
{
    // ATRIBUTOS

    public string DNI { get; set; } = string.Empty;
    public string Speciality { get; set; } = string.Empty;

    /* MÉTODOS PROPIOS

    public List<Sesion> ConsultarHorarioPersonal(DateTime fechaInicio,  DateTime fechaFin) { }

    public Sesion ConsultarDetalleSesion(int sesionId) { }

    */
}
