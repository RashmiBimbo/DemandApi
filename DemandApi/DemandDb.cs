using Microsoft.EntityFrameworkCore;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
class DemandDb : DbContext
{
    private readonly IConfiguration _configuration;

    public DemandDb(DbContextOptions<DemandDb> options)
        : base(options) { }

    public DbSet<Demand> Demands => Set<Demand>();

}