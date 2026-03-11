using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZROS.ServiceManager.Models;
using ZROS.ServiceManager.UI.Models;

namespace ZROS.ServiceManager.UI.Services
{
    /// <summary>Manages loading, saving, and listing Recipe files on disk.</summary>
    public class RecipeService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>Folder where recipe JSON files are stored.</summary>
        public string RecipeDirectory { get; set; }

        public RecipeService(string? recipeDirectory = null)
        {
            RecipeDirectory = recipeDirectory
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZROS", "Recipes");
        }

        /// <summary>Lists all recipe files found in the recipe directory.</summary>
        public List<RecipeModel> ListRecipes()
        {
            var result = new List<RecipeModel>();
            if (!Directory.Exists(RecipeDirectory)) return result;

            foreach (var file in Directory.GetFiles(RecipeDirectory, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var recipe = JsonSerializer.Deserialize<Recipe>(json, _jsonOptions);
                    if (recipe != null)
                        result.Add(RecipeModel.FromRecipe(recipe, file));
                }
                catch { /* skip malformed files */ }
            }
            return result.OrderByDescending(r => r.LastModified).ToList();
        }

        /// <summary>Loads and returns a Recipe from the given file path.</summary>
        public Recipe LoadRecipe(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("Recipe file not found.", filePath);
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Recipe>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize recipe.");
        }

        /// <summary>Saves a Recipe to the recipe directory, generating a file name from the recipe name.</summary>
        public string SaveRecipe(Recipe recipe)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            Directory.CreateDirectory(RecipeDirectory);

            if (string.IsNullOrWhiteSpace(recipe.Id))
                recipe.Id = Guid.NewGuid().ToString("N")[..8];

            var safeName = string.Join("_", recipe.Name.Split(Path.GetInvalidFileNameChars()));
            var filePath = Path.Combine(RecipeDirectory, $"{safeName}.json");
            recipe.LastModified = DateTime.UtcNow;

            var json = JsonSerializer.Serialize(recipe, _jsonOptions);
            File.WriteAllText(filePath, json);
            return filePath;
        }

        /// <summary>Deletes a recipe file from disk.</summary>
        public void DeleteRecipe(string filePath)
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }
}
