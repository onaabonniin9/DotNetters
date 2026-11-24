namespace GenteFit.Models;


public abstract class Usuario {

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

}