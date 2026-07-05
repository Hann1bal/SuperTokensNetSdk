namespace SuperTokensSDK.Net.Core;

/// <summary>
/// Base class for recipe overrides. Each property is a nullable delegate.
/// If set, it replaces the default implementation. If null, the default is used.
/// </summary>
public class RecipeOverrides
{
}

/// <summary>
/// Interface that recipes implement to support overrides.
/// </summary>
public interface IOverridableRecipe
{
    RecipeOverrides? Overrides { get; set; }
}
