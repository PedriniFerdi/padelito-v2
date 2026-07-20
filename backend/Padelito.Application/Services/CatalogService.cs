using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Padelito.Application.Common;
using Padelito.Application.DTOs.Catalogs;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Interfaces.Services;
using Padelito.Domain.Entities;

namespace Padelito.Application.Services;

public sealed class CatalogService(ICatalogRepository repository, IPasswordHasher<User> passwordHasher) : ICatalogService
{
    public async Task<IReadOnlyList<PaymentMethodDto>> GetPaymentMethodsAsync(CancellationToken cancellationToken) =>
        (await repository.GetPaymentMethodsAsync(cancellationToken)).Select(x => new PaymentMethodDto(x.Id, x.Description)).ToList();

    public async Task<IReadOnlyList<ClientListDto>> GetClientsAsync(CancellationToken cancellationToken)
    {
        return (await repository.GetClientsAsync(cancellationToken)).Select(ToClientListDto).ToList();
    }

    public async Task<ClientDetailDto> GetClientAsync(int id, CancellationToken cancellationToken)
    {
        return ToClientDetailDto(await RequireClientAsync(id, cancellationToken));
    }

    public async Task<ClientDetailDto> CreateClientAsync(ClientCreateDto request, CancellationToken cancellationToken)
    {
        await ValidateDniAsync(request.Dni, null, cancellationToken);
        var client = new Client { Person = CreatePerson(request.FirstName, request.LastName, request.Dni, request.Phone, request.Email) };
        await repository.AddClientAsync(client, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToClientDetailDto(client);
    }

    public async Task<ClientDetailDto> UpdateClientAsync(int id, ClientUpdateDto request, CancellationToken cancellationToken)
    {
        var client = await RequireClientAsync(id, cancellationToken);
        await ValidateDniAsync(request.Dni, client.PersonId, cancellationToken);
        UpdatePerson(client.Person, request.FirstName, request.LastName, request.Dni, request.Phone, request.Email);
        await repository.SaveChangesAsync(cancellationToken);
        return ToClientDetailDto(client);
    }

    public async Task SetClientActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
    {
        var client = await RequireClientAsync(id, cancellationToken);
        client.Person.IsActive = isActive;
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeListDto>> GetEmployeesAsync(int clubId, CancellationToken cancellationToken)
    {
        return (await repository.GetEmployeesAsync(clubId, cancellationToken)).Select(ToEmployeeListDto).ToList();
    }

    public async Task<EmployeeDetailDto> GetEmployeeAsync(int id, CancellationToken cancellationToken)
    {
        return ToEmployeeDetailDto(await RequireEmployeeAsync(id, cancellationToken));
    }

    public async Task<EmployeeDetailDto> CreateEmployeeAsync(int clubId, EmployeeCreateDto request, CancellationToken cancellationToken)
    {
        await ValidateDniAsync(request.Dni, null, cancellationToken);
        var employee = new Employee
        {
            ClubId = clubId,
            Person = CreatePerson(request.FirstName, request.LastName, request.Dni, request.Phone, request.Email)
        };
        await repository.AddEmployeeAsync(employee, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToEmployeeDetailDto(employee);
    }

    public async Task<EmployeeDetailDto> UpdateEmployeeAsync(int id, EmployeeUpdateDto request, CancellationToken cancellationToken)
    {
        var employee = await RequireEmployeeAsync(id, cancellationToken);
        await ValidateDniAsync(request.Dni, employee.PersonId, cancellationToken);
        UpdatePerson(employee.Person, request.FirstName, request.LastName, request.Dni, request.Phone, request.Email);
        await repository.SaveChangesAsync(cancellationToken);
        return ToEmployeeDetailDto(employee);
    }

    public async Task SetEmployeeActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
    {
        var employee = await RequireEmployeeAsync(id, cancellationToken);
        employee.Person.IsActive = isActive;
        if (!isActive && employee.User is not null)
        {
            employee.User.IsActive = false;
        }

        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserListDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        return (await repository.GetUsersAsync(cancellationToken)).Select(ToUserListDto).ToList();
    }

    public async Task<UserDetailDto> GetUserAsync(int id, CancellationToken cancellationToken)
    {
        return ToUserDetailDto(await RequireUserAsync(id, cancellationToken));
    }

    public async Task<UserDetailDto> CreateUserAsync(UserCreateDto request, CancellationToken cancellationToken)
    {
        var username = RequireText(request.Username, "El username es obligatorio.", 50);
        ValidatePassword(request.Password);

        if (await repository.UsernameExistsAsync(username, null, cancellationToken))
        {
            throw new BusinessException("Ya existe un usuario con ese username.");
        }

        var employee = await RequireEmployeeAsync(request.EmployeeId, cancellationToken);
        if (!employee.Person.IsActive)
        {
            throw new BusinessException("No se puede crear un usuario para un empleado inactivo.");
        }

        _ = await repository.GetRoleAsync(request.RoleId, cancellationToken) ?? throw new BusinessException("El rol indicado no existe.");
        if (await repository.EmployeeHasUserAsync(request.EmployeeId, null, cancellationToken))
        {
            throw new BusinessException("El empleado ya tiene un usuario asociado.");
        }

        var user = new User
        {
            Username = username,
            PasswordHash = string.Empty,
            EmployeeId = request.EmployeeId,
            RoleId = request.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Employee = employee
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await repository.AddUserAsync(user, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToUserDetailDto(await RequireUserAsync(user.Id, cancellationToken));
    }

    public async Task<UserDetailDto> UpdateUserAsync(int id, UserUpdateDto request, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(id, cancellationToken);
        var username = RequireText(request.Username, "El username es obligatorio.", 50);
        if (await repository.UsernameExistsAsync(username, id, cancellationToken))
        {
            throw new BusinessException("Ya existe un usuario con ese username.");
        }

        _ = await repository.GetRoleAsync(request.RoleId, cancellationToken) ?? throw new BusinessException("El rol indicado no existe.");
        user.Username = username;
        user.RoleId = request.RoleId;
        await repository.SaveChangesAsync(cancellationToken);
        return ToUserDetailDto(await RequireUserAsync(id, cancellationToken));
    }

    public async Task ChangeUserPasswordAsync(int id, ChangePasswordDto request, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(id, cancellationToken);
        ValidatePassword(request.Password);

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task SetUserActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(id, cancellationToken);
        if (isActive && !user.Employee.Person.IsActive)
        {
            throw new BusinessException("No se puede activar un usuario de un empleado inactivo.");
        }

        user.IsActive = isActive;
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken)
    {
        return (await repository.GetRolesAsync(cancellationToken)).Select(x => new RoleDto(x.Id, x.Name)).ToList();
    }

    public async Task<IReadOnlyList<CourtTypeDto>> GetCourtTypesAsync(CancellationToken cancellationToken)
    {
        return (await repository.GetCourtTypesAsync(cancellationToken)).Select(ToCourtTypeDto).ToList();
    }

    public async Task<CourtTypeDto> CreateCourtTypeAsync(CourtTypeCreateDto request, CancellationToken cancellationToken)
    {
        var description = RequireText(request.Description, "La descripcion es obligatoria.", 80);
        if (await repository.CourtTypeExistsAsync(description, null, cancellationToken))
        {
            throw new BusinessException("Ya existe un tipo de cancha con esa descripcion.");
        }

        var courtType = new CourtType { Description = description };
        await repository.AddCourtTypeAsync(courtType, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToCourtTypeDto(courtType);
    }

    public async Task<CourtTypeDto> UpdateCourtTypeAsync(int id, CourtTypeUpdateDto request, CancellationToken cancellationToken)
    {
        var courtType = await repository.GetCourtTypeAsync(id, cancellationToken) ?? throw new BusinessException("El tipo de cancha no existe.");
        var description = RequireText(request.Description, "La descripcion es obligatoria.", 80);
        if (await repository.CourtTypeExistsAsync(description, id, cancellationToken))
        {
            throw new BusinessException("Ya existe un tipo de cancha con esa descripcion.");
        }

        courtType.Description = description;
        await repository.SaveChangesAsync(cancellationToken);
        return ToCourtTypeDto(courtType);
    }

    public async Task<IReadOnlyList<CourtListDto>> GetCourtsAsync(int clubId, CancellationToken cancellationToken)
    {
        return (await repository.GetCourtsAsync(clubId, cancellationToken)).Select(ToCourtListDto).ToList();
    }

    public async Task<CourtDetailDto> GetCourtAsync(int id, CancellationToken cancellationToken)
    {
        return ToCourtDetailDto(await RequireCourtAsync(id, cancellationToken));
    }

    public async Task<CourtDetailDto> CreateCourtAsync(int clubId, CourtCreateDto request, CancellationToken cancellationToken)
    {
        ValidateCourt(request.Name, request.HourPrice);
        var name = request.Name.Trim();
        var courtType = await repository.GetCourtTypeAsync(request.CourtTypeId, cancellationToken) ?? throw new BusinessException("El tipo de cancha indicado no existe.");
        if (await repository.CourtNameExistsAsync(clubId, name, null, cancellationToken))
        {
            throw new BusinessException("Ya existe una cancha con ese nombre.");
        }

        var court = new Court { ClubId = clubId, Name = name, CourtTypeId = request.CourtTypeId, CourtType = courtType, HourPrice = request.HourPrice, IsActive = true };
        await repository.AddCourtAsync(court, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToCourtDetailDto(court);
    }

    public async Task<CourtDetailDto> UpdateCourtAsync(int id, CourtUpdateDto request, CancellationToken cancellationToken)
    {
        var court = await RequireCourtAsync(id, cancellationToken);
        ValidateCourt(request.Name, request.HourPrice);
        var name = request.Name.Trim();
        var courtType = await repository.GetCourtTypeAsync(request.CourtTypeId, cancellationToken) ?? throw new BusinessException("El tipo de cancha indicado no existe.");
        if (await repository.CourtNameExistsAsync(court.ClubId, name, id, cancellationToken))
        {
            throw new BusinessException("Ya existe una cancha con ese nombre.");
        }

        court.Name = name;
        court.CourtTypeId = request.CourtTypeId;
        court.CourtType = courtType;
        court.HourPrice = request.HourPrice;
        await repository.SaveChangesAsync(cancellationToken);
        return ToCourtDetailDto(court);
    }

    public async Task SetCourtActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
    {
        var court = await RequireCourtAsync(id, cancellationToken);
        court.IsActive = isActive;
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AvailableTurnListDto>> GetAvailableTurnsAsync(int clubId, CancellationToken cancellationToken)
    {
        return (await repository.GetAvailableTurnsAsync(clubId, cancellationToken)).Select(ToTurnListDto).ToList();
    }

    public async Task<AvailableTurnListDto> CreateAvailableTurnAsync(AvailableTurnCreateDto request, CancellationToken cancellationToken)
    {
        ValidateTurn(request.StartTime, request.EndTime);
        var court = await RequireCourtAsync(request.CourtId, cancellationToken);
        if (await repository.AvailableTurnExistsAsync(request.CourtId, request.StartTime, request.EndTime, null, cancellationToken))
        {
            throw new BusinessException("Ya existe ese turno para la cancha indicada.");
        }

        var turn = new AvailableTurn { CourtId = request.CourtId, Court = court, StartTime = request.StartTime, EndTime = request.EndTime, IsActive = true };
        await repository.AddAvailableTurnAsync(turn, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToTurnListDto(turn);
    }

    public async Task<AvailableTurnListDto> UpdateAvailableTurnAsync(int id, AvailableTurnUpdateDto request, CancellationToken cancellationToken)
    {
        var turn = await repository.GetAvailableTurnAsync(id, cancellationToken) ?? throw new BusinessException("El turno no existe.");
        ValidateTurn(request.StartTime, request.EndTime);
        var court = await RequireCourtAsync(request.CourtId, cancellationToken);
        if (await repository.AvailableTurnExistsAsync(request.CourtId, request.StartTime, request.EndTime, id, cancellationToken))
        {
            throw new BusinessException("Ya existe ese turno para la cancha indicada.");
        }

        turn.CourtId = request.CourtId;
        turn.Court = court;
        turn.StartTime = request.StartTime;
        turn.EndTime = request.EndTime;
        await repository.SaveChangesAsync(cancellationToken);
        return ToTurnListDto(turn);
    }

    public async Task SetAvailableTurnActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
    {
        var turn = await repository.GetAvailableTurnAsync(id, cancellationToken) ?? throw new BusinessException("El turno no existe.");
        turn.IsActive = isActive;
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PromotionListDto>> GetPromotionsAsync(CancellationToken cancellationToken)
    {
        return (await repository.GetPromotionsAsync(cancellationToken)).Select(ToPromotionDto).ToList();
    }

    public async Task<PromotionListDto> GetPromotionAsync(int id, CancellationToken cancellationToken)
    {
        return ToPromotionDto(await RequirePromotionAsync(id, cancellationToken));
    }

    public async Task<PromotionListDto> CreatePromotionAsync(PromotionCreateDto request, CancellationToken cancellationToken)
    {
        ValidatePromotion(request.Name, request.DiscountPercentage, request.DateFrom, request.DateTo);
        var promotion = new Promotion
        {
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description, 255),
            DiscountPercentage = request.DiscountPercentage,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            IsActive = true
        };
        await repository.AddPromotionAsync(promotion, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToPromotionDto(promotion);
    }

    public async Task<PromotionListDto> UpdatePromotionAsync(int id, PromotionUpdateDto request, CancellationToken cancellationToken)
    {
        var promotion = await RequirePromotionAsync(id, cancellationToken);
        ValidatePromotion(request.Name, request.DiscountPercentage, request.DateFrom, request.DateTo);
        promotion.Name = request.Name.Trim();
        promotion.Description = NormalizeOptional(request.Description, 255);
        promotion.DiscountPercentage = request.DiscountPercentage;
        promotion.DateFrom = request.DateFrom;
        promotion.DateTo = request.DateTo;
        await repository.SaveChangesAsync(cancellationToken);
        return ToPromotionDto(promotion);
    }

    public async Task SetPromotionActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
    {
        var promotion = await RequirePromotionAsync(id, cancellationToken);
        promotion.IsActive = isActive;
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Client> RequireClientAsync(int id, CancellationToken cancellationToken)
    {
        return await repository.GetClientAsync(id, cancellationToken) ?? throw new BusinessException("El cliente no existe.");
    }

    private async Task<Employee> RequireEmployeeAsync(int id, CancellationToken cancellationToken)
    {
        return await repository.GetEmployeeAsync(id, cancellationToken) ?? throw new BusinessException("El empleado no existe.");
    }

    private async Task<User> RequireUserAsync(int id, CancellationToken cancellationToken)
    {
        return await repository.GetUserAsync(id, cancellationToken) ?? throw new BusinessException("El usuario no existe.");
    }

    private async Task<Court> RequireCourtAsync(int id, CancellationToken cancellationToken)
    {
        return await repository.GetCourtAsync(id, cancellationToken) ?? throw new BusinessException("La cancha no existe.");
    }

    private async Task<Promotion> RequirePromotionAsync(int id, CancellationToken cancellationToken)
    {
        return await repository.GetPromotionAsync(id, cancellationToken) ?? throw new BusinessException("La promocion no existe.");
    }

    private async Task ValidateDniAsync(string? dni, int? excludingPersonId, CancellationToken cancellationToken)
    {
        var normalized = NormalizeDni(dni);
        if (await repository.PersonDniExistsAsync(normalized, excludingPersonId, cancellationToken))
        {
            throw new BusinessException("Ya existe una persona con ese DNI.");
        }
    }

    private static Person CreatePerson(string firstName, string lastName, string? dni, string? phone, string? email)
    {
        return new Person
        {
            FirstName = RequireText(firstName, "El nombre es obligatorio.", 60),
            LastName = RequireText(lastName, "El apellido es obligatorio.", 60),
            Dni = NormalizeDni(dni),
            Phone = NormalizePhone(phone),
            Email = NormalizeEmail(email),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static void UpdatePerson(Person person, string firstName, string lastName, string? dni, string? phone, string? email)
    {
        person.FirstName = RequireText(firstName, "El nombre es obligatorio.", 60);
        person.LastName = RequireText(lastName, "El apellido es obligatorio.", 60);
        person.Dni = NormalizeDni(dni);
        person.Phone = NormalizePhone(phone);
        person.Email = NormalizeEmail(email);
    }

    private static void ValidateCourt(string name, decimal hourPrice)
    {
        _ = RequireText(name, "El nombre de la cancha es obligatorio.", 80);
        if (hourPrice <= 0)
        {
            throw new BusinessException("El precio por hora debe ser mayor a cero.");
        }
    }

    private static void ValidateTurn(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
        {
            throw new BusinessException("La hora de fin debe ser posterior a la hora de inicio.");
        }
    }

    private static void ValidatePromotion(string name, decimal discountPercentage, DateOnly dateFrom, DateOnly dateTo)
    {
        _ = RequireText(name, "El nombre de la promocion es obligatorio.", 80);
        if (discountPercentage is <= 0 or > 100)
        {
            throw new BusinessException("El descuento debe ser mayor a cero y menor o igual a 100.");
        }

        if (dateFrom == default || dateTo == default)
        {
            throw new BusinessException("Las fechas de vigencia son obligatorias.");
        }

        if (dateTo < dateFrom)
        {
            throw new BusinessException("La fecha hasta debe ser mayor o igual a la fecha desde.");
        }
    }

    private static string RequireText(string? value, string message, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessException(message);
        }

        var normalized = value.Trim();
        if (maxLength.HasValue && normalized.Length > maxLength.Value)
        {
            throw new BusinessException($"El campo no puede superar los {maxLength.Value} caracteres.");
        }

        return normalized;
    }

    private static string NormalizeDni(string? value)
    {
        var normalized = RequireText(value, "El DNI es obligatorio.").Replace(".", string.Empty).Replace(" ", string.Empty);
        if (!Regex.IsMatch(normalized, @"^\d{7,8}$"))
        {
            throw new BusinessException("El DNI debe tener 7 u 8 digitos.");
        }

        return normalized;
    }

    private static string NormalizePhone(string? value)
    {
        var normalized = RequireText(value, "El telefono es obligatorio.", 40);
        if (normalized.Length < 8 || !Regex.IsMatch(normalized, @"^\+?[0-9 ()-]+$") || normalized.Count(char.IsDigit) < 8)
        {
            throw new BusinessException("El telefono tiene un formato invalido.");
        }

        return normalized;
    }

    private static string NormalizeEmail(string? value)
    {
        var normalized = RequireText(value, "El email es obligatorio.", 120).ToLowerInvariant();
        if (!new EmailAddressAttribute().IsValid(normalized))
        {
            throw new BusinessException("El email tiene un formato invalido.");
        }

        return normalized;
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new BusinessException("La password es obligatoria.");
        }

        if (password.Length is < 8 or > 100)
        {
            throw new BusinessException("La password debe tener entre 8 y 100 caracteres.");
        }
    }

    private static string? NormalizeOptional(string? value, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (maxLength.HasValue && normalized.Length > maxLength.Value)
        {
            throw new BusinessException($"El campo no puede superar los {maxLength.Value} caracteres.");
        }

        return normalized;
    }

    private static ClientListDto ToClientListDto(Client client)
    {
        return new ClientListDto(client.Id, client.Person.FirstName, client.Person.LastName, client.Person.Dni, client.Person.Phone, client.Person.Email, client.Person.IsActive);
    }

    private static ClientDetailDto ToClientDetailDto(Client client)
    {
        return new ClientDetailDto(client.Id, client.Person.FirstName, client.Person.LastName, client.Person.Dni, client.Person.Phone, client.Person.Email, client.Person.IsActive);
    }

    private static EmployeeListDto ToEmployeeListDto(Employee employee)
    {
        return new EmployeeListDto(employee.Id, employee.Person.FirstName, employee.Person.LastName, employee.Person.Dni, employee.Person.Phone, employee.Person.Email, employee.Person.IsActive, employee.User is not null);
    }

    private static EmployeeDetailDto ToEmployeeDetailDto(Employee employee)
    {
        return new EmployeeDetailDto(employee.Id, employee.Person.FirstName, employee.Person.LastName, employee.Person.Dni, employee.Person.Phone, employee.Person.Email, employee.Person.IsActive, employee.User is not null);
    }

    private static UserListDto ToUserListDto(User user)
    {
        return new UserListDto(user.Id, user.Username, user.EmployeeId, FullName(user.Employee.Person), user.RoleId, user.Role.Name, user.IsActive);
    }

    private static UserDetailDto ToUserDetailDto(User user)
    {
        return new UserDetailDto(user.Id, user.Username, user.EmployeeId, FullName(user.Employee.Person), user.RoleId, user.Role.Name, user.IsActive);
    }

    private static CourtTypeDto ToCourtTypeDto(CourtType courtType)
    {
        return new CourtTypeDto(courtType.Id, courtType.Description);
    }

    private static CourtListDto ToCourtListDto(Court court)
    {
        return new CourtListDto(court.Id, court.Name, court.CourtTypeId, court.CourtType.Description, court.HourPrice, court.IsActive);
    }

    private static CourtDetailDto ToCourtDetailDto(Court court)
    {
        return new CourtDetailDto(court.Id, court.Name, court.CourtTypeId, court.CourtType.Description, court.HourPrice, court.IsActive);
    }

    private static AvailableTurnListDto ToTurnListDto(AvailableTurn turn)
    {
        return new AvailableTurnListDto(turn.Id, turn.CourtId, turn.Court.Name, turn.StartTime, turn.EndTime, turn.IsActive);
    }

    private static PromotionListDto ToPromotionDto(Promotion promotion)
    {
        return new PromotionListDto(promotion.Id, promotion.Name, promotion.Description, promotion.DiscountPercentage, promotion.DateFrom, promotion.DateTo, promotion.IsActive);
    }

    private static string FullName(Person person)
    {
        return $"{person.FirstName} {person.LastName}";
    }
}
