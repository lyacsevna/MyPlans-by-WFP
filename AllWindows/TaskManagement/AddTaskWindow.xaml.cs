using MyPlans_by_WFP.Model;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace MyPlans_by_WFP
{
    public partial class AddTaskWindow : Window
    {
        public event EventHandler TaskAdded;
        private readonly string connectionString = "Data Source=LYACSEVNA;Initial Catalog=MyPlansCourseWork;Integrated Security=True;Encrypt=False";
        private Dictionary<string, int> categories;
        private int userId;

        public AddTaskWindow(int userId)
        {
            InitializeComponent();
            this.userId = userId;
            LoadCategories();
        }

        private void LoadCategories()
        {
            categories = new Dictionary<string, int>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT category_id, category_name FROM Categories WHERE user_id = @userId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int categoryId = reader.GetInt32(0);
                            string categoryName = reader.GetString(1);
                            categories.Add(categoryName, categoryId);
                            TaskCategoryComboBox.Items.Add(new ComboBoxItem { Content = categoryName });
                        }
                    }
                }
            }
        }



    private void Add_Button_Click(object sender, RoutedEventArgs e)
    {
            var title = TaskTitleTextBox.Text.Trim();
            var priority = ((ComboBoxItem)TaskPriorityComboBox.SelectedItem)?.Content.ToString();
            var categoryName = ((ComboBoxItem)TaskCategoryComboBox.SelectedItem)?.Content.ToString();
            var description = TaskDescriptionTextBox.Text.Trim();
            var status = ((ComboBoxItem)TaskStatusComboBox.SelectedItem)?.Content.ToString();
            var deadline = DeadlineTextBox.Text.Trim();
            var created_at = DateTime.Now;

            
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(priority)){
                MessageBox.Show("Заполните все обязательные поля.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int? categoryId = null;
            if (categoryName != null && categories.TryGetValue(categoryName, out int id)){
                categoryId = id;
            }

            DateTime? parsedDeadline = null;
            if (!string.IsNullOrEmpty(deadline) && DateTime.TryParse(deadline, out DateTime tempDeadline)){
                parsedDeadline = tempDeadline;
            }

            using (var connection = new SqlConnection(connectionString)){
                string query = "INSERT INTO Tasks (task_title, task_priority, task_category_id, task_description, task_status, task_created_at, " +
                    "deadline, task_author_id) VALUES (@task_title, @task_priority, @task_category_id, @task_description, @task_status, @task_created_at, " +
                    "@deadline, @task_author_id)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@task_title", title);
                    command.Parameters.AddWithValue("@task_priority", priority);
                    command.Parameters.AddWithValue("@task_category_id", categoryId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@task_description", description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@task_status", status ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@task_created_at", created_at);
                    command.Parameters.AddWithValue("@deadline", parsedDeadline.HasValue ? (object)parsedDeadline.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@task_author_id", userId);

                    connection.Open();
                    try{
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex){
                        MessageBox.Show($"Ошибка при добавлении задачи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            MessageBox.Show("Задача успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            TaskAdded?.Invoke(this, EventArgs.Empty);
            Close();
        }

        private void Add_Category_Click(object sender, RoutedEventArgs e)
        {
            AddNewCategoryWindow window = new AddNewCategoryWindow(userId);
            window.ShowDialog();
            LoadCategories();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}