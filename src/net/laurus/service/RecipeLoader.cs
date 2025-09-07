using System;
using System.Collections.Generic;
using System.IO;
using LaurusCoreLib.Net.Laurus.Enums;
using LaurusCoreLib.Net.Laurus.Logging;
using Newtonsoft.Json;
using XRL;

namespace LaurusTech.net.laurus.model.service
{
    public static class RecipeLoader
    {
        // --- Cache keyed by recipe file name
        private static readonly Dictionary<string, List<Recipe>> RecipeCache = new Dictionary<string, List<Recipe>>();

        /// <summary>
        /// Loads recipes from a JSON file in the recipe folder.
        /// Automatically appends ".json" if missing.
        /// </summary>
        public static List<Recipe> Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return LogAndReturnEmpty("Load called with empty fileName");

            fileName = EnsureJsonExtension(fileName);

            // Return cached recipes if available
            if (RecipeCache.TryGetValue(fileName, out var cached))
                return LogAndReturnCached(fileName, cached);

            var foundRecipes = LoadRecipesFromAllMods(fileName);

            // Cache the result
            RecipeCache[fileName] = foundRecipes;

            LL.Info($"RecipeLoader: Total {foundRecipes.Count} recipes loaded for '{fileName}'", LogCategory.Info);
            return foundRecipes;
        }
        private static List<Recipe> LogAndReturnEmpty(string message)
        {
            LL.Info($"RecipeLoader: {message}", LogCategory.Warning);
            return new List<Recipe>();
        }

        private static List<Recipe> LogAndReturnCached(string fileName, List<Recipe> cached)
        {
            LL.Info($"RecipeLoader: Returning cached recipes for '{fileName}'", LogCategory.Debug);
            return cached;
        }
        private static List<Recipe> LoadRecipesFromAllMods(string fileName)
        {
            var recipes = new List<Recipe>();

            ModManager.ForEachFileIn(Constants.LaurusFolder, (filePath, mod) =>
            {
                if (!Path.GetFileName(filePath).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    return;

                try
                {
                    var loaded = LoadRecipesForMod(Path.GetDirectoryName(filePath), fileName, mod);
                    if (loaded.Count > 0)
                    {
                        LL.Info($"RecipeLoader: Loaded {loaded.Count} recipes from '{fileName}' in mod '{mod.ID}'", LogCategory.Info);
                        recipes.AddRange(loaded);
                    }
                }
                catch (Exception ex)
                {
                    LL.Info($"RecipeLoader: Error loading recipes from '{filePath}' in mod '{mod.ID}': {ex}", LogCategory.Error);
                }

            }, bIncludeBase: true, bIncludeDisabled: false);

            return recipes;
        }

        private static List<Recipe> LoadRecipesForMod(string recipeDirectory, string recipeFileName, ModInfo mod)
        {
            var fullPath = Path.Combine(recipeDirectory, recipeFileName);
            var absolutePath = Path.GetFullPath(fullPath);

            LL.Info($"RecipeLoader: Attempting to load file '{absolutePath}'", LogCategory.Debug);

            if (!File.Exists(absolutePath))
            {
                LL.Info($"RecipeLoader: File not found at '{absolutePath}'", LogCategory.Warning);
                return new List<Recipe>();
            }

            try
            {
                var json = File.ReadAllText(absolutePath);
                LL.Info($"RecipeLoader: Read {json.Length} bytes from '{absolutePath}'", LogCategory.Info);
                return DeserializeRecipes(json, absolutePath);
            }
            catch (Exception ex)
            {
                LL.Info($"RecipeLoader: Failed to read file '{absolutePath}': {ex}", LogCategory.Error);
                return new List<Recipe>();
            }
        }

        private static string EnsureJsonExtension(string fileName)
        {
            return fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? fileName : fileName + ".json";
        }

        private static List<Recipe> DeserializeRecipes(string json, string filePath)
        {
            try
            {
                var recipes = JsonConvert.DeserializeObject<List<Recipe>>(json) ?? new List<Recipe>();
                LL.Info($"RecipeLoader: Successfully deserialized {recipes.Count} recipes from '{filePath}'", LogCategory.Info);
                return recipes;
            }
            catch (Exception ex)
            {
                LL.Info($"RecipeLoader: Failed to deserialize JSON from '{filePath}': {ex}", LogCategory.Error);
                LL.Info($"RecipeLoader: JSON content from '{filePath}':\n{json}", LogCategory.Error);
                return new List<Recipe>();
            }
        }
    }
}
