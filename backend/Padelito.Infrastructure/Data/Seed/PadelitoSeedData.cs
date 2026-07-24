using Microsoft.EntityFrameworkCore;
using Padelito.Domain.Entities;

namespace Padelito.Infrastructure.Data.Seed;

internal static class PadelitoSeedData
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "Reception" },
            new Role { Id = 3, Name = "Staff" });

        modelBuilder.Entity<ReservationStatus>().HasData(
            new ReservationStatus { Id = 1, Name = "Pending" },
            new ReservationStatus { Id = 2, Name = "Confirmed" },
            new ReservationStatus { Id = 3, Name = "Canceled" },
            new ReservationStatus { Id = 4, Name = "Completed" });

        modelBuilder.Entity<PaymentMethod>().HasData(
            new PaymentMethod { Id = 1, Description = "Cash" },
            new PaymentMethod { Id = 2, Description = "Debit Card" },
            new PaymentMethod { Id = 3, Description = "Credit Card" },
            new PaymentMethod { Id = 4, Description = "Bank Transfer" },
            new PaymentMethod { Id = 5, Description = "Venmo" });

        modelBuilder.Entity<CourtType>().HasData(
            new CourtType { Id = 1, Description = "Concrete" },
            new CourtType { Id = 2, Description = "Synthetic Turf" },
            new CourtType { Id = 3, Description = "Indoor" },
            new CourtType { Id = 4, Description = "Premium" });
    }
}
