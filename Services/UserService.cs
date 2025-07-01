using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using AzureFunctionIgga.Data;
using AzureFunctionIgga.Models;
using AzureFunctionIgga.Models.DTOs;
using AzureFunctionIgga.Services.Interfaces;

namespace AzureFunctionIgga.Services;

public class UserService : IUserService
{
    private readonly IggaDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(IggaDbContext context, IMapper mapper, ILogger<UserService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
            {
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Usuario no encontrado",
                    Data = null
                };
            }

            var userDto = _mapper.Map<UserDto>(user);
            return new ApiResponse<UserDto>
            {
                Success = true,
                Message = "Usuario obtenido exitosamente",
                Data = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario con ID {UserId}", id);
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Error interno del servidor",
                Data = null
            };
        }
    }

    public async Task<ApiResponse<UserDto>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

            if (user == null)
            {
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Usuario no encontrado",
                    Data = null
                };
            }

            var userDto = _mapper.Map<UserDto>(user);
            return new ApiResponse<UserDto>
            {
                Success = true,
                Message = "Usuario obtenido exitosamente",
                Data = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario con email {Email}", email);
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Error interno del servidor",
                Data = null
            };
        }
    }

    public async Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(PaginationQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var usersQuery = _context.Users.AsNoTracking();

            // Aplicar filtro de búsqueda
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var searchTerm = query.SearchTerm.ToLower();
                usersQuery = usersQuery.Where(u => 
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm));
            }

            // Aplicar ordenamiento
            usersQuery = query.SortBy?.ToLower() switch
            {
                "firstname" => query.SortDescending ? usersQuery.OrderByDescending(u => u.FirstName) : usersQuery.OrderBy(u => u.FirstName),
                "lastname" => query.SortDescending ? usersQuery.OrderByDescending(u => u.LastName) : usersQuery.OrderBy(u => u.LastName),
                "email" => query.SortDescending ? usersQuery.OrderByDescending(u => u.Email) : usersQuery.OrderBy(u => u.Email),
                "createdat" => query.SortDescending ? usersQuery.OrderByDescending(u => u.CreatedAt) : usersQuery.OrderBy(u => u.CreatedAt),
                _ => usersQuery.OrderBy(u => u.Id)
            };

            var totalCount = await usersQuery.CountAsync(cancellationToken);
            
            var users = await usersQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var userDtos = _mapper.Map<List<UserDto>>(users);

            var pagedResult = new PagedResult<UserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };

            return new ApiResponse<PagedResult<UserDto>>
            {
                Success = true,
                Message = "Usuarios obtenidos exitosamente",
                Data = pagedResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios");
            return new ApiResponse<PagedResult<UserDto>>
            {
                Success = false,
                Message = "Error interno del servidor",
                Data = null
            };
        }
    }

    public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verificar si el email ya existe
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == createUserDto.Email.ToLower(), cancellationToken);

            if (existingUser != null)
            {
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Ya existe un usuario con este email",
                    Data = null
                };
            }

            var user = _mapper.Map<User>(createUserDto);
            user.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            var userDto = _mapper.Map<UserDto>(user);

            _logger.LogInformation("Usuario creado exitosamente con ID {UserId}", user.Id);

            return new ApiResponse<UserDto>
            {
                Success = true,
                Message = "Usuario creado exitosamente",
                Data = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear usuario");
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Error interno del servidor",
                Data = null
            };
        }
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserDto updateUserDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);

            if (user == null)
            {
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Usuario no encontrado",
                    Data = null
                };
            }

            // Verificar email único si se está actualizando
            if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == updateUserDto.Email.ToLower() && u.Id != id, cancellationToken);

                if (existingUser != null)
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "Ya existe un usuario con este email",
                        Data = null
                    };
                }
            }

            // Aplicar actualizaciones
            if (!string.IsNullOrEmpty(updateUserDto.FirstName))
                user.FirstName = updateUserDto.FirstName;

            if (!string.IsNullOrEmpty(updateUserDto.LastName))
                user.LastName = updateUserDto.LastName;

            if (!string.IsNullOrEmpty(updateUserDto.Email))
                user.Email = updateUserDto.Email;

            if (!string.IsNullOrEmpty(updateUserDto.PhoneNumber))
                user.PhoneNumber = updateUserDto.PhoneNumber;

            if (!string.IsNullOrEmpty(updateUserDto.Role))
                user.Role = updateUserDto.Role;

            if (updateUserDto.IsActive.HasValue)
                user.IsActive = updateUserDto.IsActive.Value;

            if (!string.IsNullOrEmpty(updateUserDto.ProfileImageUrl))
                user.ProfileImageUrl = updateUserDto.ProfileImageUrl;

            if (!string.IsNullOrEmpty(updateUserDto.Notes))
                user.Notes = updateUserDto.Notes;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var userDto = _mapper.Map<UserDto>(user);

            _logger.LogInformation("Usuario actualizado exitosamente con ID {UserId}", user.Id);

            return new ApiResponse<UserDto>
            {
                Success = true,
                Message = "Usuario actualizado exitosamente",
                Data = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar usuario con ID {UserId}", id);
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Error interno del servidor",
                Data = null
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);

            if (user == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Usuario no encontrado",
                    Data = false
                };
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Usuario eliminado exitosamente con ID {UserId}", id);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Usuario eliminado exitosamente",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar usuario con ID {UserId}", id);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Error interno del servidor",
                Data = false
            };
        }
    }

    public async Task<ApiResponse<bool>> ActivateUserAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);

            if (user == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Usuario no encontrado",
                    Data = false
                };
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Usuario activado exitosamente con ID {UserId}", id);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Usuario activado exitosamente",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al activar usuario con ID {UserId}", id);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Error interno del servidor",
                Data = false
            };
        }
    }

    public async Task<ApiResponse<bool>> DeactivateUserAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);

            if (user == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Usuario no encontrado",
                    Data = false
                };
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Usuario desactivado exitosamente con ID {UserId}", id);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Usuario desactivado exitosamente",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar usuario con ID {UserId}", id);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Error interno del servidor",
                Data = false
            };
        }
    }
}