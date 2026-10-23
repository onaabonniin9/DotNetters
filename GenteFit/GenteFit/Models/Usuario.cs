namespace GenteFit.Models;


public abstract class Usuario {
   
   // ATRIBUTOS

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // Almacenar contraseñas en texto plano??
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    /* MÉTODOS PROPIOS

    public bool IniciarSesion(string username, string password) { }

    public void CerrarSesion() { }

    public Usuario ConsultarPerfil(){ }

    public bool ActualizarDatosContacto(string email, string phone) { }

    public bool CambiarContraseña(string passwordActual, string passwordNueva) { } */
}