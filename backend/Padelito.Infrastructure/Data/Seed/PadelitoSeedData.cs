using Microsoft.EntityFrameworkCore;
using Padelito.Domain.Entities;

namespace Padelito.Infrastructure.Data.Seed;

internal static class PadelitoSeedData
{
    private static readonly DateTime SeedDate = new(2026, 7, 8, 0, 0, 0, DateTimeKind.Utc);

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Club>().HasData(new Club
        {
            Id = 1,
            Name = "Padelito",
            Address = "Buenos Aires",
            Phone = "11-4000-0000",
            Email = "admin@padelito.com",
            IsActive = true,
            CreatedAt = SeedDate
        });

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

        modelBuilder.Entity<Person>().HasData(new Person
        {
            Id = 1,
            FirstName = "Carlos",
            LastName = "Benitez",
            Dni = "30111222",
            Phone = "11-4000-1001",
            Email = "carlos.benitez@padelito.com",
            IsActive = true,
            CreatedAt = SeedDate
        });

        modelBuilder.Entity<Employee>().HasData(new Employee
        {
            Id = 1,
            PersonId = 1,
            ClubId = 1
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = "AQAAAAIAAYagAAAAED2SFjyZfFosfjAmmH1n5FHdE59w+9e6K96p468HR/FvY6jo4v94M+pMCLf/9mpNhA==",
            EmployeeId = 1,
            RoleId = 1,
            IsActive = true,
            CreatedAt = SeedDate
        });
    }
}
