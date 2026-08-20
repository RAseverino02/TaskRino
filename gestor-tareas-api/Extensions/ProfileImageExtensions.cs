using GestorTareas.Api.Models;

namespace GestorTareas.Api.Extensions;

public static class ProfileImageExtensions
{
    public static string? GetProfileImageUrl(this User user) => user.ProfileImagePath is null
        ? null
        : $"/api/v1/usuarios/{user.Id}/foto?v={Uri.EscapeDataString(user.ProfileImagePath)}";
}
