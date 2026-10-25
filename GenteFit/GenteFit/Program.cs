
using GenteFit.Data;
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
