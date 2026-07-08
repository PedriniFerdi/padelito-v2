using Microsoft.EntityFrameworkCore;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data.Seed;

namespace Padelito.Infrastructure.Data;

public sealed class PadelitoDbContext(DbContextOptions<PadelitoDbContext> options) : DbContext(options)
{
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<CourtType> CourtTypes => Set<CourtType>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<AvailableTurn> AvailableTurns => Set<AvailableTurn>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<ReservationStatus> ReservationStatuses => Set<ReservationStatus>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ReservationAudit> ReservationAudits => Set<ReservationAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureClub(modelBuilder);
        ConfigurePerson(modelBuilder);
        ConfigureClient(modelBuilder);
        ConfigureEmployee(modelBuilder);
        ConfigureRole(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureCourtType(modelBuilder);
        ConfigureCourt(modelBuilder);
        ConfigureAvailableTurn(modelBuilder);
        ConfigurePromotion(modelBuilder);
        ConfigureReservationStatus(modelBuilder);
        ConfigureReservation(modelBuilder);
        ConfigurePaymentMethod(modelBuilder);
        ConfigurePayment(modelBuilder);
        ConfigureReservationAudit(modelBuilder);

        PadelitoSeedData.Apply(modelBuilder);
    }

    private static void ConfigureClub(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Club>(entity =>
        {
            entity.ToTable("Clubs");
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(200);
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.Email).HasMaxLength(120);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
        });
    }

    private static void ConfigurePerson(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("People");
            entity.Property(x => x.FirstName).HasMaxLength(60).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Dni).HasMaxLength(20);
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.Email).HasMaxLength(120);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(x => x.Dni).IsUnique().HasFilter("[Dni] IS NOT NULL");
        });
    }

    private static void ConfigureClient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("Clients");
            entity.HasIndex(x => x.PersonId).IsUnique();
            entity.HasOne(x => x.Person)
                .WithOne(x => x.Client)
                .HasForeignKey<Client>(x => x.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureEmployee(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasIndex(x => x.PersonId).IsUnique();
            entity.HasOne(x => x.Person)
                .WithOne(x => x.Employee)
                .HasForeignKey<Employee>(x => x.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Club)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.ClubId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(x => x.Username).HasMaxLength(50).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.EmployeeId).IsUnique();
            entity.HasOne(x => x.Employee)
                .WithOne(x => x.User)
                .HasForeignKey<User>(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCourtType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CourtType>(entity =>
        {
            entity.ToTable("CourtTypes");
            entity.Property(x => x.Description).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Description).IsUnique();
        });
    }

    private static void ConfigureCourt(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Court>(entity =>
        {
            entity.ToTable(
                "Courts",
                table => table.HasCheckConstraint("CK_Courts_HourPrice", "[HourPrice] >= 0"));
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.HourPrice).HasColumnType("decimal(10,2)");
            entity.HasIndex(x => new { x.ClubId, x.Name }).IsUnique();
            entity.HasOne(x => x.Club)
                .WithMany(x => x.Courts)
                .HasForeignKey(x => x.ClubId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CourtType)
                .WithMany(x => x.Courts)
                .HasForeignKey(x => x.CourtTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAvailableTurn(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AvailableTurn>(entity =>
        {
            entity.ToTable(
                "AvailableTurns",
                table => table.HasCheckConstraint("CK_AvailableTurns_EndTime", "[EndTime] > [StartTime]"));
            entity.HasIndex(x => new { x.CourtId, x.StartTime, x.EndTime }).IsUnique();
            entity.HasOne(x => x.Court)
                .WithMany(x => x.AvailableTurns)
                .HasForeignKey(x => x.CourtId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePromotion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable(
                "Promotions",
                table =>
                {
                    table.HasCheckConstraint("CK_Promotions_DiscountPercentage", "[DiscountPercentage] >= 0 AND [DiscountPercentage] <= 100");
                    table.HasCheckConstraint("CK_Promotions_DateRange", "[DateTo] >= [DateFrom]");
                });
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(255);
            entity.Property(x => x.DiscountPercentage).HasColumnType("decimal(5,2)");
        });
    }

    private static void ConfigureReservationStatus(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReservationStatus>(entity =>
        {
            entity.ToTable("ReservationStatuses");
            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });
    }

    private static void ConfigureReservation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable(
                "Reservations",
                table =>
                {
                    table.HasCheckConstraint("CK_Reservations_BasePrice", "[BasePrice] >= 0");
                    table.HasCheckConstraint("CK_Reservations_FinalPrice", "[FinalPrice] >= 0");
                });
            entity.Property(x => x.BasePrice).HasColumnType("decimal(10,2)");
            entity.Property(x => x.FinalPrice).HasColumnType("decimal(10,2)");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(x => new { x.ReservationDate, x.AvailableTurnId }).IsUnique();
            entity.HasOne(x => x.Client)
                .WithMany(x => x.Reservations)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AvailableTurn)
                .WithMany(x => x.Reservations)
                .HasForeignKey(x => x.AvailableTurnId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Employee)
                .WithMany(x => x.Reservations)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Promotion)
                .WithMany(x => x.Reservations)
                .HasForeignKey(x => x.PromotionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReservationStatus)
                .WithMany(x => x.Reservations)
                .HasForeignKey(x => x.ReservationStatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePaymentMethod(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("PaymentMethods");
            entity.Property(x => x.Description).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Description).IsUnique();
        });
    }

    private static void ConfigurePayment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable(
                "Payments",
                table => table.HasCheckConstraint("CK_Payments_Amount", "[Amount] > 0"));
            entity.Property(x => x.Amount).HasColumnType("decimal(10,2)");
            entity.Property(x => x.PaymentDate).HasColumnType("datetime2");
            entity.Property(x => x.Note).HasMaxLength(255);
            entity.HasOne(x => x.Reservation)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PaymentMethod)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureReservationAudit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReservationAudit>(entity =>
        {
            entity.ToTable("ReservationAudits");
            entity.Property(x => x.Action).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Username).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
            entity.HasOne(x => x.Reservation)
                .WithMany(x => x.Audits)
                .HasForeignKey(x => x.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
