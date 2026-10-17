using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Domain;

namespace COMPASS.Common.Features.Tabs;

/// <summary>
/// Read-only access to the tab the user is currently working in.
/// Inject this instead of <see cref="TabsViewModel"/> when only the active tab is needed.
/// </summary>
public interface IActiveTabContext
{
    CollectionTabVM? ActiveTab { get; }

    /// <summary>
    /// The collection of the active tab.
    /// </summary>
    /// <exception cref="NoTabException">When there is no active tab.</exception>
    CodexCollection ActiveCollection { get; }
}
