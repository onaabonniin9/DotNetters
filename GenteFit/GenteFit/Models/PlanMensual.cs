
namespace GenteFit.Models;

public class PlanMensual
{
    // ATRIBUTOS

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ClassesPerWeek { get; set; }
    public decimal MonthlyFee { get; set; }
    public bool Active { get; set; } = true;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    /* MÉTODOS PROPIOS

    public PlanMensual ConsultarDatos() { }

    public decimal CalcularCuotaMensual() { }

    public bool ComprobarLimiteSemanal(int reservasUtilizadas) { }

    public bool ActualizarDatos(string name, string description, int classesPerWeek, decimal monthlyFee) { }

    public void Activar() { }

    public void Desactivar() { }

    */
}
