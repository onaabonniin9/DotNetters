using System;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Win32;

namespace GenteFit.Data;

public class GenteFitContextFactory : IDesignTimeDbContextFactory<GenteFitContext>
{
    public GenteFitContext CreateDbContext(string[] args)
    {
        string servidor = Environment.GetEnvironmentVariable("GENTEFIT_SQL_SERVER")
            ?? @"localhost\SQLEXPRESS";
        string baseDatos = "GenteFitDB";

        if (OperatingSystem.IsWindows())
        {
            using var registro = RegistryKey.OpenBaseKey(
                RegistryHive.LocalMachine,
                RegistryView.Registry32);

            using var configuracion = registro.OpenSubKey(@"Software\GenteFit");

            if (configuracion?.GetValue("Servidor") is string servidorInstalado &&
                configuracion.GetValue("BaseDatos") is string baseInstalada &&
                !string.IsNullOrWhiteSpace(servidorInstalado) &&
                !string.IsNullOrWhiteSpace(baseInstalada))
            {
                servidor = servidorInstalado;
                baseDatos = baseInstalada;
            }
        }

        var conexion = new SqlConnectionStringBuilder
        {
            DataSource = servidor,
            InitialCatalog = baseDatos,
            IntegratedSecurity = true,
            TrustServerCertificate = true
        };

        var opciones = new DbContextOptionsBuilder<GenteFitContext>()
            .UseSqlServer(conexion.ConnectionString)
            .Options;

        return new GenteFitContext(opciones);
    }
}