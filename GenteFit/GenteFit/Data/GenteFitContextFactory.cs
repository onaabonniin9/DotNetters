
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GenteFit.Data;

public class GenteFitContextFactory : IDesignTimeDbContextFactory<GenteFitContext>
{
    public GenteFitContext CreateDbContext(string[] args)
    {
        string servidor = Environment.GetEnvironmentVariable("GENTEFIT_SQL_SERVER")
            ?? @"localhost\SQLEXPRESS";

        string cadenaConexion =
            $"Server={servidor};Database=GenteFitDB;" +
            "Trusted_Connection=True;TrustServerCertificate=True;";

        var opciones = new DbContextOptionsBuilder<GenteFitContext>()
            .UseSqlServer(cadenaConexion)
            .Options;

        return new GenteFitContext(opciones);
    }
}
