
namespace GenteFit.Models;

public class IntegracionOdoo
{
    // ATRIBUTOS

    public int Id { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime UltimaSincronizacion { get; set; }
    public EstadoIntegracion Estado { get; set; } = EstadoIntegracion.DESCONECTADA;
    public string UltimoError { get; set; } = string.Empty;

    /* MÉTODOS PROPIOS

    public EstadoIntegracion ConsultarEstado() {}

    public ResultadoSincronizacion Sincronizar() { }

    public void Activar() { }

    public void Desactivar() { }

    */
}
