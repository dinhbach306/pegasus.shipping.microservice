using Identity.Application.DTOs;
using Identity.Domain;
using Mapster;

namespace Identity.Application.Mappings;

/// <summary>
/// Mapster configuration for Identity service mappings
/// </summary>
public class IdentityMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // UserProfile -> UserProfileDto
        config.NewConfig<UserProfile, UserProfileDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Auth0UserId, src => src.Auth0UserId)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.FullName, src => src.FullName)
            .Map(dest => dest.Role, src => src.Role)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.IsActive, src => src.IsActive);

        // CreateUserProfileRequest -> UserProfile
        config.NewConfig<CreateUserProfileRequest, UserProfile>()
            .MapWith(src => new UserProfile(
                src.Auth0UserId,
                src.Email,
                src.FullName,
                src.Role
            ));

        // Support for updating UserProfile from UpdateUserProfileRequest
        // Note: This is used to adapt properties, actual update should be done via domain methods
        config.NewConfig<UpdateUserProfileRequest, UserProfile>()
            .IgnoreNonMapped(true);
    }
}

