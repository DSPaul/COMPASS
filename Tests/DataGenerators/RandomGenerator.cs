﻿using COMPASS.Common.Models;
using COMPASS.Common.Models.Preferences;
using System.Text;
using Avalonia.Media;

namespace Tests.DataGenerators
{
    public static class RandomGenerator
    {
        private static readonly Random Random = new();

        private static readonly Dictionary<Type, Func<object>> DefaultGenerators = new()
        {
            { typeof(int)   ,() => Random.Next() },
            { typeof(float) ,() => Random.NextSingle() },
            { typeof(double),() => Random.NextDouble() },
            { typeof(bool)  ,() => GetRandomBool() },
            { typeof(string),() => GetRandomString() },
            { typeof(Color) ,() => GetRandomColor() },
            { typeof(Tag)   ,() => GetRandomTag() },
        };

        #region Primitives

        public static string GetRandomString(int minLength = 5, int maxLength = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            int length = Random.Next(minLength, maxLength + 1);
            StringBuilder stringBuilder = new(length);

            for (int i = 0; i < length; i++)
            {
                stringBuilder.Append(chars[Random.Next(chars.Length)]);
            }

            return stringBuilder.ToString();
        }

        public static bool GetRandomBool(int falseFreq = 1, int trueFreq = 1) => Random.Next(falseFreq + trueFreq) < trueFreq;

        public static Color GetRandomColor()
        {
            byte[] colorBytes = new byte[3];
            Random.NextBytes(colorBytes);
            return Color.FromRgb(colorBytes[0], colorBytes[1], colorBytes[2]);
        }

        public static DateTime GetRandomDate(DateTime startDate = default)
        {
            long range = DateTime.Now.Ticks - startDate.Ticks;
            long ticks = (long)(Random.NextDouble() * range);
            return startDate.AddTicks(ticks);
        }

        #endregion

        #region My Classes

        public static Tag GetRandomTag(int depth = 4) => new()
        {
            InternalBackgroundColor = GetRandomColor(),
            Name = GetRandomString(),
            ID = Random.Next(),
            IsGroup = GetRandomBool(falseFreq: 5),
            //Give it a random number of children, to a max of depth, and each level decrease depth
            Children = new(Enumerable.Range(0, Random.Next(depth)).Select(_ => GetRandomTag(depth - 1)))
        };

        public static Codex GetRandomCodex(CodexCollection collection)
        {
            Codex codex = new(collection)
            {
                ID = Random.Next(),
                Title = GetRandomString(),
                SortingTitle = GetRandomString(),
                Authors = new(GetRandomList<string>(maxLength: 3)),
                Publisher = GetRandomString(),
                Description = GetRandomString(10, 500),
                ReleaseDate = GetRandomDate(),
                PageCount = Random.Next(1, 400),
                Version = GetRandomString(minLength: 1, maxLength: 3),
                PhysicallyOwned = GetRandomBool(),
                Rating = Random.Next(0, 6),
                Favorite = GetRandomBool(falseFreq: 10),
                OpenedCount = Random.Next(100),
                Sources = new()
                {
                    ISBN = GetRandomISBN(),
                    Path = GetRandomBool() ? GetRandomPath() : string.Empty,       //randomly decide if it has a path
                    SourceURL = GetRandomBool() ? GetRandomUrl() : string.Empty,   //randomly decide if it has a URL
                }
            };

            codex.DateAdded = GetRandomDate((DateTime)codex.ReleaseDate);
            codex.LastOpened = GetRandomDate(codex.DateAdded);

            return codex;
        }

        public static Preferences GetRandomPreferences() => new()
        {
            AutoLinkFolderTagSameName = true,
            CodexProperties = Codex.MetadataProperties,
            CardLayoutPreferences = new CardLayoutPreferences()
            {
                ShowAuthor = GetRandomBool(),
                ShowVersion = GetRandomBool(),
                ShowRating = GetRandomBool(),
                ShowFileIcons = GetRandomBool(),
                ShowPublisher = GetRandomBool(),
                ShowReleaseDate = GetRandomBool(),
                ShowTags = GetRandomBool(),
                ShowTitle = GetRandomBool()
            },
            HomeLayoutPreferences = new HomeLayoutPreferences()
            {
                ShowTitle = GetRandomBool(),
                TileWidth = 123.456
            },
            ListLayoutPreferences = new ListLayoutPreferences()
            {
                ShowAuthor = GetRandomBool(),
                ShowVersion = GetRandomBool(),
                ShowRating = GetRandomBool(),
                ShowFileIcons = GetRandomBool(),
                ShowPublisher = GetRandomBool(),
                ShowReleaseDate = GetRandomBool(),
                ShowTags = GetRandomBool(),
                ShowTitle = GetRandomBool(),
                ShowDateAdded = GetRandomBool(),
                ShowEditIcon = GetRandomBool(),
                ShowISBN = GetRandomBool()
            },
            OpenCodexPriority = new(Preferences.OpenCodexFunctions),
            TileLayoutPreferences = new TileLayoutPreferences()
            {
                DisplayedData = TileLayoutPreferences.DataOption.Title,
                ShowExtraData = true,
                TileWidth = 654.321
            },
            UIState = new UIState()
            {
                AutoHideCodexInfoPanel = GetRandomBool(),
                ShowCodexInfoPanel = GetRandomBool(),
                SortDirection = System.ComponentModel.ListSortDirection.Descending,
                SortProperty = "Title",
                StartupCollection = GetRandomString(),
                StartupLayout = COMPASS.Common.Models.Enums.CodexLayout.Card,
                StartupTab = 3
            }
        };

        #endregion

        #region Data

        public static string GetRandomISBN()
        {
            // The standard format for ISBN is "XXX-X-XX-XXXXXX-X" where X represents a digit.
            // Generate random digits for each part of the ISBN.
            int[] parts = Enumerable.Range(0, 10).Select(_ => Random.Next(10)).ToArray();

            // Calculate the last digit (checksum) based on the first 9 digits.
            int checksum = ((parts[0] * 10) + (parts[1] * 9) + (parts[2] * 8) + (parts[3] * 7) + (parts[4] * 6) +
                            (parts[5] * 5) + (parts[6] * 4) + (parts[7] * 3) + (parts[8] * 2)) % 11;
            parts[9] = checksum == 10 ? 'X' : checksum; // If the checksum is 10, represent it as 'X'.

            // Format the ISBN string using string.Join().
            return string.Join("-", parts);
        }

        public static string GetRandomPath()
        {
            // Generate random number of directories
            var path = Path.Combine(GetRandomList<string>().ToArray());
            //Add a random file name
            return Path.Combine(path, Path.GetRandomFileName());
        }

        public static string GetRandomUrl()
        {
            string protocol = "https";
            string domain = GetRandomString();
            string path = GetRandomPath();
            return $"{protocol}://www.{domain}/{path}";
        }

        #endregion

        #region Helpers

        public static List<T> GetRandomList<T>(int minLength = 0, int maxLength = 10)
        {
            var result = new List<T>();
            var generator = DefaultGenerators[typeof(T)];
            int length = Random.Next(minLength, maxLength + 1);
            for (int i = 0; i < length; i++)
            {
                result.Add((T)generator());
            }
            return result;
        }

        public static List<T> GetRandomElements<T>(List<T> list, int count)
        {
            // Shuffle the list using Fisher-Yates algorithm
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Next(0, i + 1);
                (list[j], list[i]) = (list[i], list[j]);
            }

            return list.Take(count).ToList();
        }
        #endregion
    }
}
