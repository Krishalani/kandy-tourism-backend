using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Domain.Entities;

namespace KandyTourism.Api.Application.Mapping;

public static class AdminMapping
{
    public static AdminProfileDto ToProfileDto(this AdminUser user) => new(user.Id, user.Email, user.DisplayName, user.Role);
}
