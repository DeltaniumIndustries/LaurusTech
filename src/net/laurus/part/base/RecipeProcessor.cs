using System;
using System.Collections.Generic;
using LaurusCoreLib.Net.Laurus.Enums;
using LaurusCoreLib.Net.Laurus.Logging;
using LaurusTech.net.laurus.model;
using LaurusTech.net.laurus.model.service;
using XRL.World;

namespace LaurusTech.Net.Laurus.Machine
{
    /// <summary>
    /// A TimedProcessor that works from a list of recipes.
    /// Handles loading recipes, lookup, and job matching.
    /// Subclasses provide recipe source, charge use, and optional messaging.
    /// </summary>
    [Serializable]
    public abstract class RecipeProcessor : TimedProcessor
    {
        private readonly Dictionary<string, Recipe> Recipes = new(StringComparer.OrdinalIgnoreCase);

        protected RecipeProcessor()
        {
            LoadRecipes(GetRecipeFile());
        }

        private void LoadRecipes(string path)
        {
            Recipes.Clear();
            foreach (var recipe in RecipeLoader.Load(path))
            {
                Recipes[recipe.Input] = recipe;
            }
        }

        protected override bool GetJob(GameObject obj, out string output, out int ticks)
        {
            LL.Info("Finding Job with params", LogCategory.Debug);
            if (Recipes.TryGetValue(obj.Blueprint, out var recipe))
            {
                output = recipe.Output;
                ticks = recipe.Turns;
                LL.Info("Found Job", LogCategory.Info);
                return true;
            }

            output = null;
            ticks = 0;
            LL.Info("No Job found", LogCategory.Debug);
            return false;
        }

        /// <summary>
        /// Subclasses specify where to load recipes from.
        /// </summary>
        protected abstract string GetRecipeFile();
    }
}
