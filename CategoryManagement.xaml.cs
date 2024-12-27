using MyPlans_by_WFP.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MyPlans_by_WFP
{
    public partial class CategoryManagement : Window
    {
        private ObservableCollection<Category> categories;
        private string connectionString = "Data Source=LYACSEVNA;Initial Catalog=MyPlansCourseWork;Integrated Security=True;Encrypt=False";
        private int currentUserId;

        public CategoryManagement(int userId)
        {
            InitializeComponent();
            currentUserId = userId;
            categories = new ObservableCollection<Category>();
            CategoryDataGrid.ItemsSource = categories;
            LoadCategories();
        }

        private void LoadCategories()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("SELECT category_id, category_name FROM Categories WHERE user_id = @userId", connection);
                    command.Parameters.AddWithValue("@userId", currentUserId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new Category
                            {
                                CategoryId = reader.GetInt32(0),
                                CategoryName = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке категорий: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryDataGrid.SelectedItem is Category selectedCategory)
            {
                string newCategoryName = CategoryNameTextBox.Text;

                if (!string.IsNullOrWhiteSpace(newCategoryName))
                {
                    selectedCategory.CategoryName = newCategoryName;
                    UpdateCategoryInDatabase(selectedCategory);
                    CategoryDataGrid.Items.Refresh();
                    CategoryNameTextBox.Clear();
                }
                else
                {
                    MessageBox.Show("Введите новое название категории.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите категорию для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCategoryInDatabase(Category category)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("UPDATE Categories SET category_name = @CategoryName WHERE category_id = @CategoryId AND user_id = @userId", connection);
                    command.Parameters.AddWithValue("@CategoryName", category.CategoryName);
                    command.Parameters.AddWithValue("@CategoryId", category.CategoryId);
                    command.Parameters.AddWithValue("@userId", currentUserId);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении категории: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryDataGrid.SelectedItem is Category selectedCategory)
            {
                categories.Remove(selectedCategory);
                DeleteCategoryFromDatabase(selectedCategory);
                CategoryNameTextBox.Clear(); 
            }
            else
            {
                MessageBox.Show("Выберите категорию для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCategoryFromDatabase(Category category)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("DELETE FROM Categories WHERE category_id = @CategoryId AND user_id = @userId", connection);
                    command.Parameters.AddWithValue("@CategoryId", category.CategoryId);
                    command.Parameters.AddWithValue("@userId", currentUserId);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении категории: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CategoryDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CategoryDataGrid.SelectedItem is Category selectedCategory)
            {
                CategoryNameTextBox.Text = selectedCategory.CategoryName; 
            }
        }

        public class Category
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; }
        }

        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            string newCategoryName = NewCategoryNameTextBox.Text;

            if (!string.IsNullOrWhiteSpace(newCategoryName))
            {
                if (!IsCategoryExists(newCategoryName))
                {
                    Category newCategory = new Category { CategoryName = newCategoryName };
                    categories.Add(newCategory);
                    AddCategoryToDatabase(newCategory);
                    NewCategoryNameTextBox.Clear();
                }
                else
                {
                    MessageBox.Show("Категория с таким названием уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Введите название категории.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsCategoryExists(string categoryName)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM Categories WHERE category_name = @CategoryName AND user_id = @userId", connection);
                    command.Parameters.AddWithValue("@CategoryName", categoryName);
                    command.Parameters.AddWithValue("@userId", currentUserId);
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке существования категории: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false; 
            }
        }

        private void AddCategoryToDatabase(Category category)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("INSERT INTO Categories (category_name, user_id) VALUES (@CategoryName, @userId)", connection);
                    command.Parameters.AddWithValue("@CategoryName", category.CategoryName);
                    command.Parameters.AddWithValue("@userId", currentUserId);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении категории: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoToMainButton_Click(object sender, RoutedEventArgs e)
        {
            TasksWindowView taskWindow = new TasksWindowView();
            taskWindow.Show();
            this.Close();
        }
    }
}