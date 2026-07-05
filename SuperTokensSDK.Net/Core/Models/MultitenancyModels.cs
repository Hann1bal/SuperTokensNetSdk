namespace SuperTokensSDK.Net.Core.Models;

public sealed class TenantConfig
{
    public string TenantId { get; set; } = "";
    public Dictionary<string, object> CoreConfig { get; set; } = new();
    public Dictionary<string, object>? EmailPassword { get; set; }
    public Dictionary<string, object>? Passwordless { get; set; }
    public Dictionary<string, object>? ThirdParty { get; set; }
}

public sealed class CreateOrUpdateTenantRequest
{
    public string TenantId { get; set; } = "";
    public TenantConfig Config { get; set; } = new();
}

public sealed class CreateOrUpdateTenantResponse
{
    public string? Status { get; set; }
    public bool CreatedNew { get; set; }
}

public sealed class DeleteTenantRequest
{
    public string TenantId { get; set; } = "";
    public bool? DeleteConditional { get; set; }
}

public sealed class DeleteTenantResponse
{
    public string? Status { get; set; }
    public bool DidExist { get; set; }
}

public sealed class GetTenantResponse
{
    public string? Status { get; set; }
    public TenantConfig? TenantConfig { get; set; }
}

public sealed class ListTenantsResponse
{
    public string? Status { get; set; }
    public List<TenantConfig> Tenants { get; set; } = new();
}

public sealed class AssociateUserRequest
{
    public string UserId { get; set; } = "";
}

public sealed class AssociateUserResponse
{
    public string? Status { get; set; }
    public bool DidTenantExist { get; set; }
}

public sealed class DisassociateUserFromTenantRequest
{
    public string UserId { get; set; } = "";
}
