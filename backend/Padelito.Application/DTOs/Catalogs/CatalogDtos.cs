using System.ComponentModel.DataAnnotations;

namespace Padelito.Application.DTOs.Catalogs;

public sealed record PaymentMethodDto(int Id, string Description);

public sealed record ClientListDto(int Id, string FirstName, string LastName, string Dni, string Phone, string Email, bool IsActive);
public sealed record ClientDetailDto(int Id, string FirstName, string LastName, string Dni, string Phone, string Email, bool IsActive);
public sealed record ClientCreateDto(
    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(60, ErrorMessage = "El nombre no puede superar los 60 caracteres.")] string FirstName,
    [Required(ErrorMessage = "El apellido es obligatorio."), StringLength(60, ErrorMessage = "El apellido no puede superar los 60 caracteres.")] string LastName,
    [Required(ErrorMessage = "El DNI es obligatorio."), RegularExpression(@"^[\d. ]{7,10}$", ErrorMessage = "El DNI debe tener 7 u 8 digitos.")] string Dni,
    [Required(ErrorMessage = "El telefono es obligatorio."), StringLength(40, MinimumLength = 8, ErrorMessage = "El telefono debe tener entre 8 y 40 caracteres."), RegularExpression(@"^\+?[0-9 ()-]+$", ErrorMessage = "El telefono tiene un formato invalido.")] string Phone,
    [Required(ErrorMessage = "El email es obligatorio."), StringLength(120, ErrorMessage = "El email no puede superar los 120 caracteres."), EmailAddress(ErrorMessage = "El email tiene un formato invalido.")] string Email);
public sealed record ClientUpdateDto(
    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(60, ErrorMessage = "El nombre no puede superar los 60 caracteres.")] string FirstName,
    [Required(ErrorMessage = "El apellido es obligatorio."), StringLength(60, ErrorMessage = "El apellido no puede superar los 60 caracteres.")] string LastName,
    [Required(ErrorMessage = "El DNI es obligatorio."), RegularExpression(@"^[\d. ]{7,10}$", ErrorMessage = "El DNI debe tener 7 u 8 digitos.")] string Dni,
    [Required(ErrorMessage = "El telefono es obligatorio."), StringLength(40, MinimumLength = 8, ErrorMessage = "El telefono debe tener entre 8 y 40 caracteres."), RegularExpression(@"^\+?[0-9 ()-]+$", ErrorMessage = "El telefono tiene un formato invalido.")] string Phone,
    [Required(ErrorMessage = "El email es obligatorio."), StringLength(120, ErrorMessage = "El email no puede superar los 120 caracteres."), EmailAddress(ErrorMessage = "El email tiene un formato invalido.")] string Email);

public sealed record EmployeeListDto(int Id, string FirstName, string LastName, string Dni, string Phone, string Email, bool IsActive, bool HasUser);
public sealed record EmployeeDetailDto(int Id, string FirstName, string LastName, string Dni, string Phone, string Email, bool IsActive, bool HasUser);
public sealed record EmployeeCreateDto(
    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(60)] string FirstName,
    [Required(ErrorMessage = "El apellido es obligatorio."), StringLength(60)] string LastName,
    [Required(ErrorMessage = "El DNI es obligatorio."), RegularExpression(@"^[\d. ]{7,10}$", ErrorMessage = "El DNI debe tener 7 u 8 digitos.")] string Dni,
    [Required(ErrorMessage = "El telefono es obligatorio."), StringLength(40, MinimumLength = 8), RegularExpression(@"^\+?[0-9 ()-]+$", ErrorMessage = "El telefono tiene un formato invalido.")] string Phone,
    [Required(ErrorMessage = "El email es obligatorio."), StringLength(120), EmailAddress(ErrorMessage = "El email tiene un formato invalido.")] string Email);
public sealed record EmployeeUpdateDto(
    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(60)] string FirstName,
    [Required(ErrorMessage = "El apellido es obligatorio."), StringLength(60)] string LastName,
    [Required(ErrorMessage = "El DNI es obligatorio."), RegularExpression(@"^[\d. ]{7,10}$", ErrorMessage = "El DNI debe tener 7 u 8 digitos.")] string Dni,
    [Required(ErrorMessage = "El telefono es obligatorio."), StringLength(40, MinimumLength = 8), RegularExpression(@"^\+?[0-9 ()-]+$", ErrorMessage = "El telefono tiene un formato invalido.")] string Phone,
    [Required(ErrorMessage = "El email es obligatorio."), StringLength(120), EmailAddress(ErrorMessage = "El email tiene un formato invalido.")] string Email);

public sealed record RoleDto(int Id, string Name);
public sealed record UserListDto(int Id, string Username, int EmployeeId, string EmployeeName, int RoleId, string Role, bool IsActive);
public sealed record UserDetailDto(int Id, string Username, int EmployeeId, string EmployeeName, int RoleId, string Role, bool IsActive);
public sealed record UserCreateDto(
    [Required(ErrorMessage = "El username es obligatorio."), StringLength(50)] string Username,
    [Required(ErrorMessage = "La password es obligatoria."), StringLength(100, MinimumLength = 8, ErrorMessage = "La password debe tener entre 8 y 100 caracteres.")] string Password,
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un empleado.")] int EmployeeId,
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un rol.")] int RoleId);
public sealed record UserUpdateDto(
    [Required(ErrorMessage = "El username es obligatorio."), StringLength(50)] string Username,
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un rol.")] int RoleId);
public sealed record ChangePasswordDto(
    [Required(ErrorMessage = "La password es obligatoria."), StringLength(100, MinimumLength = 8, ErrorMessage = "La password debe tener entre 8 y 100 caracteres.")] string Password);

public sealed record CourtTypeDto(int Id, string Description);
public sealed record CourtTypeCreateDto([Required(ErrorMessage = "La descripcion es obligatoria."), StringLength(80)] string Description);
public sealed record CourtTypeUpdateDto([Required(ErrorMessage = "La descripcion es obligatoria."), StringLength(80)] string Description);

public sealed record CourtListDto(int Id, string Name, int CourtTypeId, string CourtType, decimal HourPrice, bool IsActive);
public sealed record CourtDetailDto(int Id, string Name, int CourtTypeId, string CourtType, decimal HourPrice, bool IsActive);
public sealed record CourtCreateDto(
    [Required(ErrorMessage = "El nombre de la cancha es obligatorio."), StringLength(80)] string Name,
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un tipo de cancha.")] int CourtTypeId,
    [Range(0.01, 99999999.99, ErrorMessage = "El precio por hora debe ser mayor a cero.")] decimal HourPrice);
public sealed record CourtUpdateDto(
    [Required(ErrorMessage = "El nombre de la cancha es obligatorio."), StringLength(80)] string Name,
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un tipo de cancha.")] int CourtTypeId,
    [Range(0.01, 99999999.99, ErrorMessage = "El precio por hora debe ser mayor a cero.")] decimal HourPrice);

public sealed record AvailableTurnListDto(int Id, int CourtId, string CourtName, TimeOnly StartTime, TimeOnly EndTime, bool IsActive);
public sealed record AvailableTurnCreateDto([Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una cancha.")] int CourtId, TimeOnly StartTime, TimeOnly EndTime);
public sealed record AvailableTurnUpdateDto([Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una cancha.")] int CourtId, TimeOnly StartTime, TimeOnly EndTime);

public sealed record PromotionListDto(int Id, string Name, string? Description, decimal DiscountPercentage, DateOnly DateFrom, DateOnly DateTo, bool IsActive);
public sealed record PromotionCreateDto(
    [Required(ErrorMessage = "El nombre de la promocion es obligatorio."), StringLength(80)] string Name,
    [StringLength(255)] string? Description,
    [Range(0.01, 100, ErrorMessage = "El descuento debe ser mayor a cero y menor o igual a 100.")] decimal DiscountPercentage,
    DateOnly DateFrom,
    DateOnly DateTo);
public sealed record PromotionUpdateDto(
    [Required(ErrorMessage = "El nombre de la promocion es obligatorio."), StringLength(80)] string Name,
    [StringLength(255)] string? Description,
    [Range(0.01, 100, ErrorMessage = "El descuento debe ser mayor a cero y menor o igual a 100.")] decimal DiscountPercentage,
    DateOnly DateFrom,
    DateOnly DateTo);
