using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ZROS.ServiceManager.Models;
using ZROS.ServiceManager.UI.Models;
using ZROS.ServiceManager.UI.Services;

namespace ZROS.ServiceManager.UI.ViewModels
{
    /// <summary>ViewModel for the recipe management view.</summary>
    public class RecipeManagementViewModel : ViewModelBase
    {
        private readonly IServiceManagerService _serviceManager;
        private readonly RecipeService _recipeService;

        private ObservableCollection<RecipeModel> _recipes = new ObservableCollection<RecipeModel>();
        private RecipeModel? _selectedRecipe;

        public ObservableCollection<RecipeModel> Recipes
        {
            get => _recipes;
            private set => SetProperty(ref _recipes, value);
        }

        public RecipeModel? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                SetProperty(ref _selectedRecipe, value);
                ((RelayCommand)LoadRecipeCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteRecipeCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand RefreshCommand      { get; }
        public ICommand LoadRecipeCommand   { get; }
        public ICommand SaveAsRecipeCommand { get; }
        public ICommand DeleteRecipeCommand { get; }
        public ICommand NewRecipeCommand    { get; }

        public RecipeManagementViewModel(IServiceManagerService serviceManager, RecipeService? recipeService = null)
        {
            _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
            _recipeService  = recipeService  ?? new RecipeService();

            RefreshCommand      = new RelayCommand(_ => LoadRecipes());
            LoadRecipeCommand   = new RelayCommand(_ => ExecuteLoadRecipe(),   _ => SelectedRecipe != null);
            SaveAsRecipeCommand = new RelayCommand(_ => ExecuteSaveAsRecipe());
            DeleteRecipeCommand = new RelayCommand(_ => ExecuteDeleteRecipe(), _ => SelectedRecipe != null);
            NewRecipeCommand    = new RelayCommand(_ => ExecuteNewRecipe());

            LoadRecipes();
        }

        public void LoadRecipes()
        {
            _recipes.Clear();
            foreach (var r in _recipeService.ListRecipes())
                _recipes.Add(r);
        }

        private void ExecuteLoadRecipe()
        {
            if (SelectedRecipe == null) return;
            try
            {
                _serviceManager.LoadRecipe(SelectedRecipe.FilePath);
                MessageBox.Show($"Recipe '{SelectedRecipe.Name}' loaded successfully.", "Recipe Loaded",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load Recipe Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteSaveAsRecipe()
        {
            var name = PromptForInput("Enter recipe name:", "Save As Recipe", "My Recipe");
            if (string.IsNullOrWhiteSpace(name)) return;

            var recipe = new Recipe
            {
                Id   = Guid.NewGuid().ToString("N")[..8],
                Name = name,
                Services = _serviceManager.GetAllServiceStatus().Keys
                    .Select(n => new ServiceDefinition { Name = n })
                    .ToList()
            };

            try
            {
                var path = _recipeService.SaveRecipe(recipe);
                LoadRecipes();
                MessageBox.Show($"Recipe saved to:\n{path}", "Recipe Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDeleteRecipe()
        {
            if (SelectedRecipe == null) return;
            var result = MessageBox.Show($"Delete recipe '{SelectedRecipe.Name}'?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                _recipeService.DeleteRecipe(SelectedRecipe.FilePath);
                LoadRecipes();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteNewRecipe()
        {
            var name = PromptForInput("Enter new recipe name:", "New Recipe", "New Recipe");
            if (string.IsNullOrWhiteSpace(name)) return;

            var recipe = new Recipe
            {
                Id      = Guid.NewGuid().ToString("N")[..8],
                Name    = name,
                Version = "1.0.0"
            };

            try
            {
                var path = _recipeService.SaveRecipe(recipe);
                LoadRecipes();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Create Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Simple input dialog using a WPF window.</summary>
        private static string? PromptForInput(string prompt, string title, string defaultValue)
        {
            var win = new Window
            {
                Title = title, Width = 360, Height = 140,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize
            };
            var panel  = new StackPanel { Margin = new Thickness(12) };
            var label  = new System.Windows.Controls.TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 6) };
            var input  = new System.Windows.Controls.TextBox   { Text = defaultValue };
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            var ok     = new System.Windows.Controls.Button { Content = "OK",     Width = 70, Margin = new Thickness(0, 0, 4, 0), IsDefault = true };
            var cancel = new System.Windows.Controls.Button { Content = "Cancel", Width = 70, IsCancel = true };

            string? result = null;
            ok.Click     += (_, _) => { result = input.Text; win.Close(); };
            cancel.Click += (_, _) => { win.Close(); };

            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            panel.Children.Add(label);
            panel.Children.Add(input);
            panel.Children.Add(buttons);
            win.Content = panel;
            win.ShowDialog();
            return result;
        }
    }
}
