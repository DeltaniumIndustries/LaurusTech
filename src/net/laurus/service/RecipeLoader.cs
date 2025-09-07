using System;
using System.Collections.Generic;
using System.IO;
using LaurusCoreLib.Net.Laurus.Enums;
using LaurusCoreLib.Net.Laurus.Logging;
using Newtonsoft.Json;
using XRL;
using XRL.Collections;

namespace LaurusTech.net.laurus.model.service
{
    public static class RecipeLoader
    {
        /// <summary>
        /// Folder where all recipe JSON files are stored.
        /// </summary>
        private static readonly string RecipeFolder = "json";

        /// <summary>
        /// Loads recipes from a JSON file in the recipe folder.
        /// Automatically appends ".json" if missing.
        /// </summary>
        public static List<Recipe> Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                LL.Info("RecipeLoader: Load called with empty fileName", LogCategory.Warning);
                return new List<Recipe>();
            }

            fileName = EnsureJsonExtension(fileName);

            string jsonDir = GetCurrentModJsonDir();
            if (jsonDir == null)
            {
                LL.Info("RecipeLoader: Could not find current mod's JSON folder", LogCategory.Warning);
                return new List<Recipe>();
            }


            var fullPath = Path.Combine(jsonDir, fileName);
            var absolutePath = Path.GetFullPath(fullPath);

            if (!File.Exists(absolutePath))
            {
                LL.Info($"RecipeLoader: File not found at '{absolutePath}'", LogCategory.Warning);
                return new List<Recipe>();
            }

            try
            {
                var json = File.ReadAllText(absolutePath);
                return DeserializeRecipes(json, absolutePath);
            }
            catch (Exception ex)
            {
                LL.Info($"RecipeLoader: Failed to read file '{absolutePath}': {ex}", LogCategory.Error);
                return new List<Recipe>();
            }
        }

        /// <summary>
        /// Returns the full path to the JSON data directory of the current active mod.
        /// </summary>
        private static string GetCurrentModJsonDir()
        {
            try
            {
                foreach (ModInfo activeMod in (Container<ModInfo>)ModManager.ActiveMods)
                {
                    // Only consider active mods
                    if (!activeMod.Active)
                        continue;

                    // Check that Manifest exists and ID matches
                    if (activeMod.Manifest == null)
                        continue;

                    if (!string.Equals(activeMod.Manifest.ID, Constants.MODID, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Ensure the mod directory exists
                    if (activeMod.Directory == null || !activeMod.Directory.Exists)
                        continue;

                    // Check for the "json" folder
                    string jsonFolder = Path.Combine(activeMod.Directory.FullName, RecipeFolder);
                    if (Directory.Exists(jsonFolder))
                        return jsonFolder;
                }
            }
            catch (Exception ex)
            {
                LL.Info($"RecipeLoader: Error locating current mod's JSON folder: {ex}", LogCategory.Error);
            }

            LL.Info("RecipeLoader: No matching mod JSON folder found.", LogCategory.Warning);
            return null;
        }



        /// <summary>
        /// Ensures the filename ends with ".json".
        /// </summary>
        private static string EnsureJsonExtension(string fileName)
        {
            return fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? fileName : fileName + ".json";
        }

        /// <summary>
        /// Deserializes JSON into a list of Recipe objects with error logging.
        /// </summary>
        private static List<Recipe> DeserializeRecipes(string json, string filePath)
        {
            try
            {
                return JsonConvert.DeserializeObject<List<Recipe>>(json) ?? new List<Recipe>();
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
