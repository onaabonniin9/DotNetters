
namespace GenteFit.Models;

public enum TipoCuentaUsuario // tipo de cuenta en la app, no hace falta almacenarlo en la bbdd, se obtiene del tipo real del user
{
    Cliente,
    Administrador
}

public sealed record UsuarioAutenticado(
    int UsuarioId,
    string Username,
    string Name,
    string Surname,
    TipoCuentaUsuario Tipo,
    int? ClienteId
)
{
    public void ExigirAdministrador()
    {
        if (Tipo != TipoCuentaUsuario.Administrador)
        {
            throw new UnauthorizedAccessException(
                "No tienes permisos para realizar esta operación.");
        }
    }

    public void ExigirCliente()
    {
        _ = ObtenerClienteId();
    }

    public int ObtenerClienteId()
    {
        if (Tipo != TipoCuentaUsuario.Cliente ||
            ClienteId is null ||
            ClienteId.Value <= 0 ||
            ClienteId.Value != UsuarioId)
        {
            throw new UnauthorizedAccessException(
                "No tienes permisos para acceder a los datos de este cliente.");
        }

        return ClienteId.Value;
    }
}
