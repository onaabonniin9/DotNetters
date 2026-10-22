
using GenteFit.Data;
using Microsoft.EntityFrameworkCore;


// Ona, Víctor y Andreu, si vuestro SQL Server tiene otro nombre, configurad la variable
// GENTEFIT_SQL_SERVER con vuestra instancia.
// Si usáis localhost\SQLEXPRESS, no tenéis que tocar nada.

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
