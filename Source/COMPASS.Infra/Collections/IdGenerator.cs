namespace COMPASS.Infra.Collections;

public static class IdGenerator
{
    public static int GetAvailableId<T>(IEnumerable<T> collection) where T : IHasId
    {
        int tempId = 0;
        IList<int> usedIds = collection.Select(x => x.Id).ToList();
        while (usedIds.Contains(tempId))
        {
            tempId++;
        }
        return tempId;
    }
}
