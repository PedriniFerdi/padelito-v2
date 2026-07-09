namespace Padelito.Application.DTOs.Catalogs;

public sealed record ClientListDto(int Id, string FirstName, string LastName, string? Dni, string? Phone, string? Email, bool IsActive);
public sealed record ClientDetailDto(int Id, string FirstName, string LastName, string? Dni, string? Phone, string? Email, bool IsActive);
public sealed record ClientCreateDto(string FirstName, string LastName, string? Dni, string? Phone, string? Email);
public sealed record ClientUpdateDto(string FirstName, string LastName, string? Dni, string? Phone, string? Email);

public sealed record EmployeeListDto(int Id, string FirstName, string LastName, string? Dni, string? Phone, string? Email, bool IsActive, bool HasUser);
public sealed record EmployeeDetailDto(int Id, string FirstName, string LastName, string? Dni, string? Phone, string? Email, bool IsActive, bool HasUser);
public sealed record EmployeeCreateDto(string FirstName, string LastName, string? Dni, string? Phone, string? Email);
public sealed record EmployeeUpdateDto(string FirstName, string LastName, string? Dni, string? Phone, string? Email);

public sealed record RoleDto(int Id, string Name);
public sealed record UserListDto(int Id, string Username, int EmployeeId, string EmployeeName, int RoleId, string Role, bool IsActive);
public sealed record UserDetailDto(int Id, string Username, int EmployeeId, string EmployeeName, int RoleId, string Role, bool IsActive);
public sealed record UserCreateDto(string Username, string Password, int EmployeeId, int RoleId);
public sealed record UserUpdateDto(string Username, int RoleId);
public sealed record ChangePasswordDto(string Password);

public sealed record CourtTypeDto(int Id, string Description);
public sealed record CourtTypeCreateDto(string Description);
public sealed record CourtTypeUpdateDto(string Description);

public sealed record CourtListDto(int Id, string Name, int CourtTypeId, string CourtType, decimal HourPrice, bool IsActive);
public sealed record CourtDetailDto(int Id, string Name, int CourtTypeId, string CourtType, decimal HourPrice, bool IsActive);
public sealed record CourtCreateDto(string Name, int CourtTypeId, decimal HourPrice);
public sealed record CourtUpdateDto(string Name, int CourtTypeId, decimal HourPrice);

public sealed record AvailableTurnListDto(int Id, int CourtId, string CourtName, TimeOnly StartTime, TimeOnly EndTime, bool IsActive);
public sealed record AvailableTurnCreateDto(int CourtId, TimeOnly StartTime, TimeOnly EndTime);
public sealed record AvailableTurnUpdateDto(int CourtId, TimeOnly StartTime, TimeOnly EndTime);

public sealed record PromotionListDto(int Id, string Name, string? Description, decimal DiscountPercentage, DateOnly DateFrom, DateOnly DateTo, bool IsActive);
public sealed record PromotionCreateDto(string Name, string? Description, decimal DiscountPercentage, DateOnly DateFrom, DateOnly DateTo);
public sealed record PromotionUpdateDto(string Name, string? Description, decimal DiscountPercentage, DateOnly DateFrom, DateOnly DateTo);
