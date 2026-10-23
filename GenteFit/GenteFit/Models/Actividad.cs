
namespace GenteFit.Models;

public class Actividad
{
    // ATRIBUTOS

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NivelIntensidad Level { get; set; }
    public int Duration { get; set; } // Minutos de duración expresado en int
    public bool Active { get; set; } = true;

    /* MÉTODOS PROPIOS

    public List<Sesion> ConsultarSesiones(DateTime fecha) { }

    public bool ActualizarDatos(string name, string description, NivelIntensidad level, int duration){ }

    public void Activar() {}}

    public void Desactivar() { }

    */
}
