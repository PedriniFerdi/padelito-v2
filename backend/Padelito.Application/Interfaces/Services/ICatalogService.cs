using Padelito.Application.DTOs.Catalogs;

namespace Padelito.Application.Interfaces.Services;

public interface ICatalogService
{
    Task<IReadOnlyList<PaymentMethodDto>> GetPaymentMethodsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ClientListDto>> GetClientsAsync(CancellationToken cancellationToken);
    Task<ClientDetailDto> GetClientAsync(int id, CancellationToken cancellationToken);
    Task<ClientDetailDto> CreateClientAsync(ClientCreateDto request, CancellationToken cancellationToken);
    Task<ClientDetailDto> UpdateClientAsync(int id, ClientUpdateDto request, CancellationToken cancellationToken);
    Task SetClientActiveAsync(int id, bool isActive, CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployeeListDto>> GetEmployeesAsync(int clubId, CancellationToken cancellationToken);
    Task<EmployeeDetailDto> GetEmployeeAsync(int id, CancellationToken cancellationToken);
    Task<EmployeeDetailDto> CreateEmployeeAsync(int clubId, EmployeeCreateDto request, CancellationToken cancellationToken);
    Task<EmployeeDetailDto> UpdateEmployeeAsync(int id, EmployeeUpdateDto request, CancellationToken cancellationToken);
    Task SetEmployeeActiveAsync(int id, bool isActive, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserListDto>> GetUsersAsync(CancellationToken cancellationToken);
    Task<UserDetailDto> GetUserAsync(int id, CancellationToken cancellationToken);
    Task<UserDetailDto> CreateUserAsync(UserCreateDto request, CancellationToken cancellationToken);
    Task<UserDetailDto> UpdateUserAsync(int id, UserUpdateDto request, CancellationToken cancellationToken);
    Task ChangeUserPasswordAsync(int id, ChangePasswordDto request, CancellationToken cancellationToken);
    Task SetUserActiveAsync(int id, bool isActive, CancellationToken cancellationToken);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<CourtTypeDto>> GetCourtTypesAsync(CancellationToken cancellationToken);
    Task<CourtTypeDto> CreateCourtTypeAsync(CourtTypeCreateDto request, CancellationToken cancellationToken);
    Task<CourtTypeDto> UpdateCourtTypeAsync(int id, CourtTypeUpdateDto request, CancellationToken cancellationToken);

    Task<IReadOnlyList<CourtListDto>> GetCourtsAsync(int clubId, CancellationToken cancellationToken);
    Task<CourtDetailDto> GetCourtAsync(int id, CancellationToken cancellationToken);
    Task<CourtDetailDto> CreateCourtAsync(int clubId, CourtCreateDto request, CancellationToken cancellationToken);
    Task<CourtDetailDto> UpdateCourtAsync(int id, CourtUpdateDto request, CancellationToken cancellationToken);
    Task SetCourtActiveAsync(int id, bool isActive, CancellationToken cancellationToken);

    Task<IReadOnlyList<AvailableTurnListDto>> GetAvailableTurnsAsync(int clubId, CancellationToken cancellationToken);
    Task<AvailableTurnListDto> CreateAvailableTurnAsync(AvailableTurnCreateDto request, CancellationToken cancellationToken);
    Task<AvailableTurnListDto> UpdateAvailableTurnAsync(int id, AvailableTurnUpdateDto request, CancellationToken cancellationToken);
    Task SetAvailableTurnActiveAsync(int id, bool isActive, CancellationToken cancellationToken);

    Task<IReadOnlyList<PromotionListDto>> GetPromotionsAsync(CancellationToken cancellationToken);
    Task<PromotionListDto> GetPromotionAsync(int id, CancellationToken cancellationToken);
    Task<PromotionListDto> CreatePromotionAsync(PromotionCreateDto request, CancellationToken cancellationToken);
    Task<PromotionListDto> UpdatePromotionAsync(int id, PromotionUpdateDto request, CancellationToken cancellationToken);
    Task SetPromotionActiveAsync(int id, bool isActive, CancellationToken cancellationToken);
}
