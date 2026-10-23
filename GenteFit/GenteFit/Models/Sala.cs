namespace GenteFit.Models;

public class Sala
{
    // ATRIBUTOS

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool Active { get; set; } = true;

    /* MÉTODOS PROPIOS

    public bool ConsultarDisponibilidad(DateTime fecha, TimeOnly horaInicio, TimeOnly horaFin) { }

    public bool ActualizarDatos(string name, int capacity) {}

    public void Activar() { }

    public void Desactivar() { }

    */
}
