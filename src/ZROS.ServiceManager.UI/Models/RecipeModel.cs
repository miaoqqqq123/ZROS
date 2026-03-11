using System;
using ZROS.ServiceManager.Models;

namespace ZROS.ServiceManager.UI.Models
{
    /// <summary>UI-layer wrapper for a <see cref="Recipe"/>.</summary>
    public class RecipeModel : ViewModels.ViewModelBase
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _description = string.Empty;
        private string _version = "1.0.0";
        private DateTime _created = DateTime.UtcNow;
        private DateTime _lastModified = DateTime.UtcNow;
        private int _serviceCount;
        private string _filePath = string.Empty;

        public string Id          { get => _id;          set => SetProperty(ref _id, value); }
        public string Name        { get => _name;        set => SetProperty(ref _name, value); }
        public string Description { get => _description; set => SetProperty(ref _description, value); }
        public string Version     { get => _version;     set => SetProperty(ref _version, value); }
        public DateTime Created   { get => _created;     set => SetProperty(ref _created, value); }
        public DateTime LastModified { get => _lastModified; set => SetProperty(ref _lastModified, value); }
        public int ServiceCount   { get => _serviceCount; set => SetProperty(ref _serviceCount, value); }
        public string FilePath    { get => _filePath;    set => SetProperty(ref _filePath, value); }

        public static RecipeModel FromRecipe(Recipe recipe, string filePath = "")
        {
            return new RecipeModel
            {
                Id           = recipe.Id,
                Name         = recipe.Name,
                Description  = recipe.Description,
                Version      = recipe.Version,
                Created      = recipe.Created,
                LastModified = recipe.LastModified,
                ServiceCount = recipe.Services?.Count ?? 0,
                FilePath     = filePath
            };
        }

        public Recipe ToRecipe() => new Recipe
        {
            Id           = Id,
            Name         = Name,
            Description  = Description,
            Version      = Version,
            Created      = Created,
            LastModified = LastModified
        };
    }
}
