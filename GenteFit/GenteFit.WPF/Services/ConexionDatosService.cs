
using GenteFit.Data;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.WPF.Services;

public class ConexionDatosService
{
    public async Task<bool> ComprobarConexionAsync()
    {
        string servidor =
            Environment.GetEnvironmentVariable("GENTEFIT_SQL_SERVER")
            ?? @"localhost\SQLEXPRESS";

        string cadenaConexion =
            $"Server={servidor};Database=GenteFitDB;" +
            "Trusted_Connection=True;TrustServerCertificate=True;";

        var opciones = new DbContextOptionsBuilder<GenteFitContext>()
            .UseSqlServer(cadenaConexion)
            .Options;

        await using var contexto = new GenteFitContext(opciones);

        return await contexto.Database.CanConnectAsync();
    }
}
