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

        modelBuilder.Entity<Person>().HasData(
            new Person { Id = 1, FirstName = "Carlos", LastName = "Benitez", Dni = "30111222", Phone = "11-4000-1001", Email = "carlos.benitez@padelito.com", IsActive = true, CreatedAt = SeedDate },
            new Person { Id = 9001, FirstName = "Lucía", LastName = "Fernández", Dni = "35421678", Phone = "11-5821-4076", Email = "lucia.fernandez@example.com", IsActive = true, CreatedAt = SeedDate },
            new Person { Id = 9002, FirstName = "Martín", LastName = "Sosa", Dni = "32987412", Phone = "11-4962-1180", Email = "martin.sosa@example.com", IsActive = true, CreatedAt = SeedDate },
            new Person { Id = 9003, FirstName = "Valentina", LastName = "Ríos", Dni = "38741209", Phone = "11-6234-9041", Email = "valentina.rios@example.com", IsActive = true, CreatedAt = SeedDate },
            new Person { Id = 9004, FirstName = "Nicolás", LastName = "Acosta", Dni = "36109874", Phone = "11-4487-6620", Email = "nicolas.acosta@example.com", IsActive = true, CreatedAt = SeedDate });

        modelBuilder.Entity<Client>().HasData(
            new Client { Id = 9001, PersonId = 9001 }, new Client { Id = 9002, PersonId = 9002 },
            new Client { Id = 9003, PersonId = 9003 }, new Client { Id = 9004, PersonId = 9004 });

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

        modelBuilder.Entity<Court>().HasData(
            new Court { Id = 9001, ClubId = 1, CourtTypeId = 2, Name = "Central Demo", HourPrice = 18000m, IsActive = true },
            new Court { Id = 9002, ClubId = 1, CourtTypeId = 3, Name = "Norte Demo", HourPrice = 22000m, IsActive = true },
            new Court { Id = 9003, ClubId = 1, CourtTypeId = 4, Name = "Arena Demo", HourPrice = 26000m, IsActive = true });

        modelBuilder.Entity<AvailableTurn>().HasData(
            new AvailableTurn { Id = 9001, CourtId = 9001, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 30), IsActive = true },
            new AvailableTurn { Id = 9002, CourtId = 9001, StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 30), IsActive = true },
            new AvailableTurn { Id = 9003, CourtId = 9002, StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(17, 30), IsActive = true },
            new AvailableTurn { Id = 9004, CourtId = 9002, StartTime = new TimeOnly(20, 0), EndTime = new TimeOnly(21, 30), IsActive = true },
            new AvailableTurn { Id = 9005, CourtId = 9003, StartTime = new TimeOnly(19, 0), EndTime = new TimeOnly(20, 30), IsActive = true });

        modelBuilder.Entity<Promotion>().HasData(new Promotion
        {
            Id = 9001, Name = "Horario tranquilo demo", Description = "Beneficio demo para turnos seleccionados.",
            DiscountPercentage = 15m, DateFrom = new DateOnly(2026, 7, 1), DateTo = new DateOnly(2026, 12, 31), IsActive = true
        });

        modelBuilder.Entity<Reservation>().HasData(
            new Reservation { Id = 9001, ClientId = 9001, AvailableTurnId = 9001, EmployeeId = 1, PromotionId = 9001, ReservationDate = new DateOnly(2026, 7, 8), ReservationStatusId = 4, BasePrice = 27000m, FinalPrice = 22950m, CreatedAt = SeedDate.AddHours(10) },
            new Reservation { Id = 9002, ClientId = 9002, AvailableTurnId = 9003, EmployeeId = 1, ReservationDate = new DateOnly(2026, 7, 9), ReservationStatusId = 4, BasePrice = 33000m, FinalPrice = 33000m, CreatedAt = SeedDate.AddHours(12) },
            new Reservation { Id = 9003, ClientId = 9003, AvailableTurnId = 9002, EmployeeId = 1, ReservationDate = new DateOnly(2026, 7, 12), ReservationStatusId = 2, BasePrice = 27000m, FinalPrice = 27000m, CreatedAt = SeedDate.AddDays(2).AddHours(14) },
            new Reservation { Id = 9004, ClientId = 9004, AvailableTurnId = 9004, EmployeeId = 1, PromotionId = 9001, ReservationDate = new DateOnly(2026, 7, 13), ReservationStatusId = 1, BasePrice = 33000m, FinalPrice = 28050m, CreatedAt = SeedDate.AddDays(3).AddHours(16) },
            new Reservation { Id = 9005, ClientId = 9001, AvailableTurnId = 9005, EmployeeId = 1, ReservationDate = new DateOnly(2026, 7, 10), ReservationStatusId = 3, BasePrice = 39000m, FinalPrice = 39000m, CreatedAt = SeedDate.AddDays(1).AddHours(9) });

        modelBuilder.Entity<Payment>().HasData(
            new Payment { Id = 9001, ReservationId = 9001, PaymentMethodId = 4, Amount = 22950m, PaymentDate = SeedDate.AddDays(1).AddHours(11), Note = "Pago completo demo" },
            new Payment { Id = 9002, ReservationId = 9002, PaymentMethodId = 1, Amount = 15000m, PaymentDate = SeedDate.AddDays(1).AddHours(13), Note = "Seña en efectivo" },
            new Payment { Id = 9003, ReservationId = 9003, PaymentMethodId = 5, Amount = 27000m, PaymentDate = SeedDate.AddDays(3).AddHours(15), Note = "Mercado Pago" });

        modelBuilder.Entity<ReservationAudit>().HasData(
            new ReservationAudit { Id = 9001, ReservationId = 9001, Action = "Creacion", Description = "Reserva demo creada para Lucía Fernández en cancha Central Demo.", Username = "admin", CreatedAt = SeedDate.AddHours(10) },
            new ReservationAudit { Id = 9002, ReservationId = 9001, Action = "CambioEstado", Description = "Estado cambiado de Confirmada a Finalizada.", Username = "admin", CreatedAt = SeedDate.AddDays(1).AddHours(12) },
            new ReservationAudit { Id = 9003, ReservationId = 9002, Action = "Creacion", Description = "Reserva demo creada para Martín Sosa en cancha Norte Demo.", Username = "admin", CreatedAt = SeedDate.AddHours(12) },
            new ReservationAudit { Id = 9004, ReservationId = 9003, Action = "Creacion", Description = "Reserva demo creada para Valentina Ríos en cancha Central Demo.", Username = "admin", CreatedAt = SeedDate.AddDays(2).AddHours(14) },
            new ReservationAudit { Id = 9005, ReservationId = 9004, Action = "Creacion", Description = "Reserva demo creada para Nicolás Acosta en cancha Norte Demo.", Username = "admin", CreatedAt = SeedDate.AddDays(3).AddHours(16) },
            new ReservationAudit { Id = 9006, ReservationId = 9005, Action = "Creacion", Description = "Reserva demo creada para Lucía Fernández en cancha Arena Demo.", Username = "admin", CreatedAt = SeedDate.AddDays(1).AddHours(9) },
            new ReservationAudit { Id = 9007, ReservationId = 9005, Action = "CambioEstado", Description = "Estado cambiado de Pendiente a Cancelada.", Username = "admin", CreatedAt = SeedDate.AddDays(1).AddHours(10) });
    }
}
