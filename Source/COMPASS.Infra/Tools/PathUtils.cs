using Microsoft.Extensions.FileSystemGlobbing;

namespace COMPASS.Infra.Tools;

public static class PathUtils
{
    /// <summary>
        /// Get the longest common path shared by all the given paths
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string GetCommonFolder(List<string> paths)
        {
            if (paths is null) throw new ArgumentNullException(nameof(paths));
            if (paths.Count == 0) return string.Empty;

            string reference = paths.First();
            string[] folders = reference.Split(Path.DirectorySeparatorChar);
            string commonFolder = "";
            foreach (string folder in folders)
            {
                string nextFolderToTest = Path.Combine(commonFolder, folder);
                if (paths.All(path => path.StartsWith(nextFolderToTest)))
                {
                    commonFolder = nextFolderToTest;
                }
                else
                {
                    break;
                }
            }

            return commonFolder;
        }

        /// <summary>
        /// Returns the paths without the longest common end
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        public static (string, string) GetDifferingRoot(string path1, string path2)
        {
            if (path1 is null)
            {
                throw new ArgumentNullException(nameof(path1));
            }

            if (path2 is null)
            {
                throw new ArgumentNullException(nameof(path2));
            }

            if (path1 == path2)
            {
                return (path1, path2);
            }

            var dirs1 = path1.Split(Path.DirectorySeparatorChar);
            var dirs2 = path2.Split(Path.DirectorySeparatorChar);

            int shortestLength = Math.Min(dirs1.Length, dirs2.Length);

            int commonDirs = 0;
            while (commonDirs < shortestLength //while we have not iterated over the entire shortest array
                   && dirs1.TakeLast(commonDirs + 1)
                       .SequenceEqual(dirs2.TakeLast(commonDirs + 1))) //and the last x elements are the same
            {
                commonDirs++;
            }

            string remainingPath1 = string.Join(Path.DirectorySeparatorChar, dirs1.Take(dirs1.Length - commonDirs));
            string remainingPath2 = string.Join(Path.DirectorySeparatorChar, dirs2.Take(dirs2.Length - commonDirs));

            return (remainingPath1, remainingPath2);
        }

        public static bool MatchesAnyGlob(string filePath, IList<string> globs )
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            
            // Check for exact matches first
            if (globs.Any(glob => glob == filePath))
            {
                return true;
            }

            var matcher = new Matcher();
            matcher.AddIncludePatterns(globs);
            
            string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
            string fileName = Path.GetFileName(filePath);
            
            return matcher.Match(directory, fileName).HasMatches;
        }
}