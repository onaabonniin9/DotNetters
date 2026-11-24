using System;
using System.Threading.Tasks;
using GenteFit.Data;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.WPF.Services;

public class ConexionDatosService
{
    public async Task<bool> ComprobarConexionAsync()
    {
        var factory = new GenteFitContextFactory();
        await using var contexto = factory.CreateDbContext(Array.Empty<string>());

        return await contexto.Database.CanConnectAsync();
    }
}