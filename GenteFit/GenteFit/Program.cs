
using GenteFit.Data;
using GenteFit.Models;
using GenteFit.Services;
using Microsoft.EntityFrameworkCore;

string servidor = Environment.GetEnvironmentVariable("GENTEFIT_SQL_SERVER")
    ?? @"localhost\SQLEXPRESS";

string cadenaConexion =
    $"Server={servidor};Database=GenteFitDB;Trusted_Connection=True;TrustServerCertificate=True;";

var opciones = new DbContextOptionsBuilder<GenteFitContext>()
    .UseSqlServer(cadenaConexion)
    .Options;

using var contexto = new GenteFitContext(opciones);

bool conectado = contexto.Database.CanConnect();

Console.WriteLine(conectado
    ? "Conexión con SQL Server correcta."
    : "No se ha podido conectar con SQL Server.");

if (conectado)
{
    const string usuarioPrueba = "clienteprueba";

    bool existe = await contexto.Usuarios
        .AnyAsync(u => u.Username == usuarioPrueba);

    if (!existe)
    {
        var cliente = new Cliente
        {
            Name = "Cliente",
            Surname = "Prueba",
            Username = usuarioPrueba,
            Password = SeguridadContrasenas.CrearHash("Prueba2026!"),
            DNI = "00000000T",
            Email = "cliente.prueba@example.com",
            BirthDate = new DateTime(2000, 1, 1),
            Active = true
        };

        var clienteService = new ClienteService(contexto);
        await clienteService.CrearAsync(cliente);

        Console.WriteLine("Cliente de prueba creado correctamente.");
    }
    else
    {
        Console.WriteLine("El cliente de prueba ya existe.");
    }
}
