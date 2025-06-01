using System.Collections.ObjectModel;
using COMPASS.Common.Models;
using COMPASS.Common.Operations;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels;
using NUnit.Framework.Constraints;

namespace Tests.DataGenerators
{
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
            collection.AllCodices.AddRange(RandomGenerator.GetRandomList<Codex>());

            //assign tags to codices
            foreach (var c in collection.AllCodices)
            {
                c.Tags = new ObservableCollection<Tag>(RandomGenerator.GetRandomElements(collection.AllTags, Random.Next(0, 4)));
            }

            //TODO add settings


            return collection;
        }
    }
}
