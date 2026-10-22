
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Data;

public class GenteFitContext : DbContext
{
    public GenteFitContext(DbContextOptions<GenteFitContext> options)
        : base(options)
    {
    }
}