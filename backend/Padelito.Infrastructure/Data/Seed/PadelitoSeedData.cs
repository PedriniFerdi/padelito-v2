using Microsoft.EntityFrameworkCore;
using Padelito.Domain.Entities;

namespace Padelito.Infrastructure.Data.Seed;

internal static class PadelitoSeedData
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Administrador" },
            new Role { Id = 2, Name = "Recepcion" },
            new Role { Id = 3, Name = "Empleado" });

        modelBuilder.Entity<ReservationStatus>().HasData(
            new ReservationStatus { Id = 1, Name = "Pendiente" },
            new ReservationStatus { Id = 2, Name = "Confirmada" },
            new ReservationStatus { Id = 3, Name = "Cancelada" },
            new ReservationStatus { Id = 4, Name = "Finalizada" });

        modelBuilder.Entity<PaymentMethod>().HasData(
            new PaymentMethod { Id = 1, Description = "Efectivo" },
            new PaymentMethod { Id = 2, Description = "Tarjeta de debito" },
            new PaymentMethod { Id = 3, Description = "Tarjeta de credito" },
            new PaymentMethod { Id = 4, Description = "Transferencia" },
            new PaymentMethod { Id = 5, Description = "Mercado Pago" });

        modelBuilder.Entity<CourtType>().HasData(
            new CourtType { Id = 1, Description = "Cemento" },
            new CourtType { Id = 2, Description = "Sintetico" },
            new CourtType { Id = 3, Description = "Indoor" },
            new CourtType { Id = 4, Description = "Premium" });
    }
}
