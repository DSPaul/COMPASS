using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls.Documents;

namespace COMPASS.Common.Models
{
    /// <summary>
    /// Wrapper for functions that should be tried in a set order until one succeeds,
    /// supporting both synchronous and asynchronous operations.
    /// </summary>
    /// <typeparam name="T">Type of argument of the function</typeparam>
    public class PreferableFunction<T> : IHasID
    {
        // For synchronous functions
        public PreferableFunction(string name, Func<T, bool> func, int id = -1)
        {
            Name = name;
            SyncFunction = func;
            ID = id;
            IsAsync = false;
        }

        // For asynchronous functions
        public PreferableFunction(string name, Func<T, Task<bool>> asyncFunc, int id = -1)
        {
            Name = name;
            AsyncFunction = asyncFunc;
            ID = id;
            IsAsync = true;
        }
        
        public string Name { get; }
        
        // Implement IHasID
        public int ID { get; set; }

        // Function properties
        public Func<T, bool>? SyncFunction { get; }
        public Func<T, Task<bool>>? AsyncFunction { get; }
        public bool IsAsync { get; }

        // Execute the function (either sync or async)
        public async Task<bool> ExecuteAsync(T arg)
        {
            if (IsAsync && AsyncFunction != null)
            {
                return await AsyncFunction(arg);
            }
            else if (!IsAsync && SyncFunction != null)
            {
                return SyncFunction(arg);
            }
            
            return false; // Default if no function is set
        }
        
        public bool Execute(T arg)
        {
            if (IsAsync)
            {
                throw new InvalidOperationException("Cannot execute an async function synchronously. Use ExecuteAsync instead.");
            }
            
            return SyncFunction?.Invoke(arg) ?? false;
        }

        // Try functions in order determined by list of preferable functions until one succeeds
        public static bool TryFunctions<A>(IEnumerable<PreferableFunction<A>> toTry, A arg, bool throwOnAsync)
        {
            bool success = false;
            
            foreach (var func in toTry)
            {
                if (!func.IsAsync)
                {
                    success = func.Execute(arg);
                }
                else
                {
                    if (throwOnAsync)
                    {
                        throw new InvalidOperationException("Cannot execute async functions with TryFunctions. Use TryFunctionsAsync instead.");
                    }
                    else
                    {
                        success = func.ExecuteAsync(arg).Result;
                    }
                }

                if (success) break;
            }
            
            return success;
        }

        // Async version of TryFunctions
        public static async Task<bool> TryFunctionsAsync<A>(IEnumerable<PreferableFunction<A>> toTry, A arg)
        {
            bool success = false;
            foreach (var func in toTry)
            {
                success = await func.ExecuteAsync(arg);
                if (success) break;
            }
            return success;
        }
    }
}