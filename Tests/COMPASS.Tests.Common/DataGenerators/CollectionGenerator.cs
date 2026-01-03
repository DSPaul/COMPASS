using System.Collections.ObjectModel;
using COMPASS.Common.Models;

namespace COMPASS.Tests.Common.DataGenerators;

public static class CollectionGenerator
{
    private static readonly Random Random = new();

    /// <summary>
    /// Get a complete collection, meaning that it has everything a collection can have
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static CodexCollection GetCompleteCollection(string name)
    {
        var collection = new CodexCollection(name);

        //create tags and codices
        collection.AddTags(RandomGenerator.GetRandomList<Tag>());
        int codexCount = Random.Next(1, 20);
        for (int i = 0; i < codexCount; i++)
        {
            Codex? codex = RandomGenerator.GetRandomCodex(collection);
            collection.AllCodices.Add(codex);
        }

        //assign tags to codices
        foreach (var c in collection.AllCodices)
        {
            c.Tags = new(RandomGenerator.GetRandomElements<Tag>(collection.AllTags.ToList(), Random.Next(0, 4)));
        }

        //TODO add settings


        return collection;
    }
}