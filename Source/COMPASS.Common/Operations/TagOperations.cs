using COMPASS.Common.Models;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;

namespace COMPASS.Common.Operations;

/// <summary>
/// Domain operations for <see cref="Tag"/>
/// </summary>
public class TagOperations(INotificationService notificationService)
{
    /// <summary>
    /// Deletes a tag, prompting the user if the tag is in use. Mirrors the former
    /// <see cref="CodexCollection.DeleteTag"/> service-aware path.
    /// </summary>
    public async Task<bool> Delete(CodexCollection collection, Tag toDelete)
    {
        var inUseBy = collection.AllCodices.Where(c => c.Tags.Contains(toDelete)).ToList();
        if (inUseBy.Any())
        {
            var codexNames = string.Join("\n - ", inUseBy.Select(c => c.Title));
            Notification confirm = Notification.AreYouSureNotification;
            confirm.Body = $"The tag '{toDelete.Name}' is currently in use by {inUseBy.Count} items.\n Are you sure you want to delete it?";
            confirm.Details = $"{toDelete.LongName} is currently assigned to:\n\n - {codexNames}";
            await notificationService.ShowDialog(confirm);

            if (confirm.Result != NotificationAction.Confirm)
            {
                return false;
            }
        }

        InnerDelete(collection, toDelete);
        return true;
    }

    /// <summary>
    /// Deep-clones a tag tree without copying ids or parent references.
    /// </summary>
    public static Tag DeepClone(Tag source, out Dictionary<Tag, Tag> origToClone) =>
        DeepCloneTags(new List<Tag> { source }, out origToClone).Single();
    

    /// <summary>
    /// Deep-clones multiple root tags, sharing one source-to-clone map.
    /// </summary>
    public static List<Tag> DeepCloneTags(IEnumerable<Tag> roots, out Dictionary<Tag, Tag> origToClone)
    {
        //can't pass out var to lambda so we need to create a local dictionary and then assign it to the out parameter at the end
        var localOrigToClone = new Dictionary<Tag, Tag>();

        var clonedTags = roots.Select(root => InnerDeepClone(root, localOrigToClone)).ToList();
        origToClone = localOrigToClone;
        return clonedTags;
    }

    private static Tag InnerDeepClone(Tag source, Dictionary<Tag, Tag> origToClone)
    {
        Tag clone = source.Clone();

        //clear links to other tags, will become links to their clones
        clone.Children.Clear();
        clone.Parent = null;
        clone.Id = -1;

        origToClone[source] = clone;
        foreach (Tag child in source.Children)
        {
            Tag childCopy = InnerDeepClone(child, origToClone);
            childCopy.Parent = clone;
            clone.Children.Add(childCopy);
        }
        return clone;
    }

    /// <summary>
    /// Pure removal logic, extracted for testability.
    /// </summary>
    private static void InnerDelete(CodexCollection collection, Tag toDelete)
    {
        //Remove from all codices
        foreach (Codex codex in collection.AllCodices)
        {
            codex.Tags.Remove(toDelete);
        }

        //Recursive loop to delete all children
        if (toDelete.Children.Count > 0)
        {
            InnerDelete(collection, toDelete.Children[0]);
            InnerDelete(collection, toDelete);
            return;
        }

        //Remove the tag from AllTags
        collection.AllTags.Remove(toDelete);

        //Remove the tags from parent's children list
        if (toDelete.Parent is null)
        {
            collection.RootTags.Remove(toDelete);
        }
        else
        {
            toDelete.Parent.Children.Remove(toDelete);
        }
    }
}
