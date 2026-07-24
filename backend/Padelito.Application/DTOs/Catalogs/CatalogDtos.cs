using System.ComponentModel.DataAnnotations;

namespace Padelito.Application.DTOs.Catalogs;

public sealed record PaymentMethodDto(int Id, string Description);

public sealed record ClientListDto(int Id, string FirstName, string LastName, string Dni, string Phone, string Email, bool IsActive);
public sealed record ClientDetailDto(int Id, string FirstName, string LastName, string Dni, string Phone, string Email, bool IsActive);
public sealed record ClientProfileDto(
    int ClientId,
    string ClientName,
    string Dni,
    string Phone,
    string Email,
    bool IsActive,
    int TotalReservations,
    decimal TotalPaid,
    decimal PendingBalance,
    string? FavoriteDayName,
    TimeOnly? FavoriteStartTime,
    DateOnly? LastVisitDate,
    int CancellationCount);
public sealed record ClientCreateDto(
    [Required(ErrorMessage = "First name is required."), StringLength(60, ErrorMessage = "First name cannot exceed 60 characters.")] string FirstName,
    [Required(ErrorMessage = "Last name is required."), StringLength(60, ErrorMessage = "Last name cannot exceed 60 characters.")] string LastName,
    [Required(ErrorMessage = "Customer ID is required."), RegularExpression(@"^[\d. ]{7,10}$", ErrorMessage = "Customer ID must contain 7 to 10 digits.")] string Dni,
    [Required(ErrorMessage = "Phone is required."), StringLength(40, MinimumLength = 8, ErrorMessage = "Phone must contain between 8 and 40 characters."), RegularExpression(@"^\+?[0-9 ()-]+$", ErrorMessage = "Phone has an invalid format.")] string Phone,
    [Required(ErrorMessage = "Email is required."), StringLength(120, ErrorMessage = "Email cannot exceed 120 characters."), EmailAddress(ErrorMessage = "Email has an invalid format.")] string Email);
public sealed record ClientUpdateDto(
    [Required(ErrorMessage = "First name is required."), StringLength(60, ErrorMessage = "First name cannot exceed 60 characters.")] string FirstName,
    [Required(ErrorMessage = "Last name is required."), StringLength(60, ErrorMessage = "Last name cannot exceed 60 characters.")] string LastName,
    [Required(ErrorMessage = "Customer ID is required."), RegularExpression(@"^[\d. ]{7,10}$", ErrorMessage = "Customer ID must contain 7 to 10 digits.")] string Dni,
    [Required(ErrorMessage = "Phone is required."), StringLength(40, MinimumLength = 8, ErrorMessage = "Phone must contain between 8 and 40 characters."), RegularExpression(@"^\+?[0-9 ()-]+$", ErrorMessage = "Phone has an invalid format.")] string Phone,
    [Required(ErrorMessage = "Email is required."), StringLength(120, ErrorMessage = "Email cannot exceed 120 characters."), EmailAddress(ErrorMessage = "Email has an invalid format.")] string Email);

public sealed record EmployeeListDto(int Id, string FirstName, string LastName, string Dni, string Phone, string Email, bool IsActive, bool HasUser);
public sealed record EmployeeDetailDto(int Id, string FirstName, string LastName, string Dni, string Phone, string Email, bool IsActive, bool HasUser);
public sealed record EmployeeCreateDto(
    [Required(ErrorMessage = "First name is required."), StringLength(60)] string FirstName,
    [Required(ErrorMessage = "Last name is required."), StringLength(60)] string LastName,
    [Required(ErrorMessage = "Customer ID is required."), RegularExpression(@"^[\d. ]{7,10}$", ErrorMessage = "Customer ID must contain 7 to 10 digits.")] string Dni,
    [Required(ErrorMessage = "Phone is required."), StringLength(40, MinimumLength = 8), RegularExpression(@"^\+?[0-9 ()-]+$", ErrorMessage = "Phone has an invalid format.")] string Phone,
    [Required(ErrorMessage = "Email is required."), StringLength(120), EmailAddress(ErrorMessage = "Email has an invalid format.")] string Email);
public sealed record EmployeeUpdateDto(
    [Required(ErrorMessage = "First name is required."), StringLength(60)] string FirstName,
    [Required(ErrorMessage = "Last name is required."), StringLength(60)] string LastName,
    [Required(ErrorMessage = "Customer ID is required."), RegularExpression(@"^[\d. ]{7,10}$", ErrorMessage = "Customer ID must contain 7 to 10 digits.")] string Dni,
    [Required(ErrorMessage = "Phone is required."), StringLength(40, MinimumLength = 8), RegularExpression(@"^\+?[0-9 ()-]+$", ErrorMessage = "Phone has an invalid format.")] string Phone,
    [Required(ErrorMessage = "Email is required."), StringLength(120), EmailAddress(ErrorMessage = "Email has an invalid format.")] string Email);

public sealed record RoleDto(int Id, string Name);
public sealed record UserListDto(int Id, string Username, int EmployeeId, string EmployeeName, int RoleId, string Role, bool IsActive);
public sealed record UserDetailDto(int Id, string Username, int EmployeeId, string EmployeeName, int RoleId, string Role, bool IsActive);
public sealed record UserCreateDto(
    [Required(ErrorMessage = "Username is required."), StringLength(50)] string Username,
    [Required(ErrorMessage = "Password is required."), StringLength(100, MinimumLength = 8, ErrorMessage = "Password must contain between 8 and 100 characters.")] string Password,
    [Range(1, int.MaxValue, ErrorMessage = "Select a staff member.")] int EmployeeId,
    [Range(1, int.MaxValue, ErrorMessage = "Select a role.")] int RoleId);
public sealed record UserUpdateDto(
    [Required(ErrorMessage = "Username is required."), StringLength(50)] string Username,
    [Range(1, int.MaxValue, ErrorMessage = "Select a role.")] int RoleId);
public sealed record ChangePasswordDto(
    [Required(ErrorMessage = "Password is required."), StringLength(100, MinimumLength = 8, ErrorMessage = "Password must contain between 8 and 100 characters.")] string Password);

public sealed record CourtTypeDto(int Id, string Description);
public sealed record CourtTypeCreateDto([Required(ErrorMessage = "Description is required."), StringLength(80)] string Description);
public sealed record CourtTypeUpdateDto([Required(ErrorMessage = "Description is required."), StringLength(80)] string Description);

public sealed record CourtListDto(int Id, string Name, int CourtTypeId, string CourtType, decimal HourPrice, bool IsActive);
public sealed record CourtDetailDto(int Id, string Name, int CourtTypeId, string CourtType, decimal HourPrice, bool IsActive);
public sealed record CourtCreateDto(
    [Required(ErrorMessage = "Court name is required."), StringLength(80)] string Name,
    [Range(1, int.MaxValue, ErrorMessage = "Select a court type.")] int CourtTypeId,
    [Range(0.01, 99999999.99, ErrorMessage = "Hourly price must be greater than zero.")] decimal HourPrice);
public sealed record CourtUpdateDto(
    [Required(ErrorMessage = "Court name is required."), StringLength(80)] string Name,
    [Range(1, int.MaxValue, ErrorMessage = "Select a court type.")] int CourtTypeId,
    [Range(0.01, 99999999.99, ErrorMessage = "Hourly price must be greater than zero.")] decimal HourPrice);

public sealed record AvailableTurnListDto(int Id, int CourtId, string CourtName, TimeOnly StartTime, TimeOnly EndTime, bool IsActive);
public sealed record AvailableTurnCreateDto([Range(1, int.MaxValue, ErrorMessage = "Select a court.")] int CourtId, TimeOnly StartTime, TimeOnly EndTime);
public sealed record AvailableTurnUpdateDto([Range(1, int.MaxValue, ErrorMessage = "Select a court.")] int CourtId, TimeOnly StartTime, TimeOnly EndTime);

public sealed record PromotionListDto(int Id, string Name, string? Description, decimal DiscountPercentage, DateOnly DateFrom, DateOnly DateTo, bool IsActive);
public sealed record PromotionCreateDto(
    [Required(ErrorMessage = "Promotion name is required."), StringLength(80)] string Name,
    [StringLength(255)] string? Description,
    [Range(0.01, 100, ErrorMessage = "Discount must be greater than zero and less than or equal to 100.")] decimal DiscountPercentage,
    DateOnly DateFrom,
    DateOnly DateTo);
public sealed record PromotionUpdateDto(
    [Required(ErrorMessage = "Promotion name is required."), StringLength(80)] string Name,
    [StringLength(255)] string? Description,
    [Range(0.01, 100, ErrorMessage = "Discount must be greater than zero and less than or equal to 100.")] decimal DiscountPercentage,
    DateOnly DateFrom,
    DateOnly DateTo);
