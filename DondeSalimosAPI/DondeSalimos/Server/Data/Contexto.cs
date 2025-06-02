using DondeSalimos.Shared.Modelos;
using Microsoft.EntityFrameworkCore;

namespace DondeSalimos.Server.Data
{
    public class Contexto : DbContext
    {
        public DbSet<Cliente> Cliente { get; set; }
        public DbSet<Comercio> Comercio { get; set; }
        public DbSet<Publicidad> Publicidad { get; set; }
        public DbSet<Resenia> Resenia { get; set; }
        public DbSet<Reserva> Reserva { get; set; }
        public DbSet<RolUsuario> RolUsuario { get; set; }
        public DbSet<TipoComercio> TipoComercio { get; set; }
        public DbSet<Usuario> Usuario { get; set; }

        public Contexto(DbContextOptions<Contexto> options) : base(options)
        {

        }
    }
}
