using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Domain;

namespace COMPASS.Common.Features.Tabs;

/// <summary>
/// Kept up to date by <see cref="TabsViewModel"/>. Separate from it so that view models
/// created as part of a tab can depend on the active tab without a circular dependency.
/// </summary>
public class ActiveTabContext : IActiveTabContext
{
    public CollectionTabVM? ActiveTab { get; set; }

    public CodexCollection ActiveCollection =>
        (ActiveTab ?? throw new NoTabException("No collection is active because there is no active tab"))
        .CollectionVM.Collection;
}
