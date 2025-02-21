using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Media;


namespace MyPlans_by_WFP
{
    public class Category // Определение класса Category
    {
        public string Name { get; set; }
        public string Color { get; set; }
    }

    public partial class AddNewCategoryWindow : Window
    {
        private Color selectedColor; 
        private string connectionString = "Data Source=LYACSEVNA;Initial Catalog=MyPlansCourseWork;Integrated Security=True;Encrypt=False";
        private int currentUserId; 
        public AddNewCategoryWindow(int userId) 
        {
            InitializeComponent();
            currentUserId = userId;
        }

        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {
            string categoryName = CategoryNameTextBox.Text;

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                MessageBox.Show("Пожалуйста, введите название категории.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            
            if (selectedColor == null)
            {
                MessageBox.Show("Пожалуйста, выберите цвет для категории.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveCategory(categoryName, selectedColor, currentUserId); 
            this.DialogResult = true;
            this.Close();
        }

        private void SaveCategory(string categoryName, Color color, int userId)
        {
            string hexColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("INSERT INTO Categories (category_name, color, user_id) " +
                    "VALUES (@name, @color, @userId)", connection);
                command.Parameters.AddWithValue("@name", categoryName);
                command.Parameters.AddWithValue("@color", hexColor);
                command.Parameters.AddWithValue("@userId", userId); 
                command.ExecuteNonQuery();
            }
        }

        private List<Category> GetCategoriesForUser(int userId)
        {
            var categories = new List<Category>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT category_name, color FROM Categories WHERE user_id = @userId", connection);
                command.Parameters.AddWithValue("@userId", userId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var categoryName = reader["category_name"].ToString();
                        var color = reader["color"].ToString();
                        categories.Add(new Category { Name = categoryName, Color = color });
                    }
                }
            }

            return categories;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
