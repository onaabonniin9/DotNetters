
namespace GenteFit.Models;

public class Actividad
{


    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NivelIntensidad Level { get; set; }
    public int Duration { get; set; } 
    public bool Active { get; set; } = true;

}
