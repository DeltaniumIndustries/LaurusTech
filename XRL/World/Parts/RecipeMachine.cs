using System;
using System.Collections.Generic;
using LaurusTech.net.laurus.model;
using LaurusTech.net.laurus.model.service;

namespace XRL.World.Parts
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
            if (Recipes.TryGetValue(obj.Blueprint, out var recipe))
            {
                output = recipe.Output;
                ticks = recipe.Turns;
                return true;
            }

            output = null;
            ticks = 0;
            return false;
        }

        /// <summary>
        /// Subclasses specify where to load recipes from.
        /// </summary>
        protected abstract string GetRecipeFile();
    }
}
