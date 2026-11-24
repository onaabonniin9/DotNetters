using GenteFit.Models;

namespace GenteFit.Services;

//Resultado de la pantalla 12: Muestra el cliente, el plan activo y los pagos.

public record DetalleClienteRecepcion(
    Cliente Cliente,
    string? PlanMensual,
    List<Pago> Pagos);
