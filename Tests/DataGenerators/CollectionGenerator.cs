//using COMPASS.Models;
//using COMPASS.Tools;

namespace Tests.DataGenerators
{
    public static class CollectionGenerator
    {
        private static Random random = new();

        /// <summary>
        /// Get a complete collection, meaning that it has everything a collection can have
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        //public static CodexCollection GetCompleteCollection(string name)
        //{
        //    var collection = new CodexCollection(name);
        //    collection.InitAsNew();

        //    //create tags and codices
        //    collection.AddTags(RandomGenerator.GetRandomList<Tag>());
        //    collection.AllCodices.AddRange(RandomGenerator.GetRandomList<Codex>());

        //    //assign tags to codices
        //    foreach (var c in collection.AllCodices)
        //    {
        //        c.Tags = new(RandomGenerator.GetRandomElements(collection.AllTags, random.Next(0, 4)));
        //    }

        //    //TODO add settings


        //    return collection;
        //}
    }
}
