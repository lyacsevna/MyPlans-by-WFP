using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace MyPlans_by_WFP
{
    public partial class ArchiveTasks : Window
    {
        private readonly DatabaseHelper databaseHelper;
        private readonly string connectionString = "Data Source=LYACSEVNA;Initial Catalog=MyPlansCourseWork;Integrated Security=True;Encrypt=False";
        private bool isEditing = false;
        private int? TaskID;
        private int userId;

        public ArchiveTasks(int userId)
        {
            InitializeComponent();
            this.userId = userId;
            databaseHelper = new DatabaseHelper();
            LoadCategories();
            LoadTasks(DateTime.Now);
        }

        private void LoadCategories()
        {
            var categories = databaseHelper.GetCategories(userId);
            TaskCategoryComboBox.ItemsSource = categories;
            TaskCategoryComboBox.DisplayMemberPath = "CategoryName";
            TaskCategoryComboBox.SelectedValuePath = "CategoryId";
        }

        private void LoadTasks(DateTime selectedDate)
        {
            var tasks = databaseHelper.GetTasks(selectedDate, userId);
            TaskDataGrid.ItemsSource = tasks;
            ClearTaskFields();
        }

        private void TaskDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (TaskDataGrid.SelectedItem is TaskModel selectedTask)
            {
                TaskID = selectedTask.TaskId;
                TaskTitleTextBox.Text = selectedTask.TaskTitle;
                TaskPriorityTextBox.Text = selectedTask.TaskPriority;
                TaskCategoryComboBox.SelectedValue = selectedTask.TaskCategoryId;
                TaskDescriptionTextBox.Text = selectedTask.TaskDescription;
                TaskStatusTextBox.Text = selectedTask.TaskStatus;
                DeadlineTextBox.Text = selectedTask.Deadline?.ToString("d");
                ToggleEditing(true);
            }
            else
            {
                ClearTaskFields();
            }
        }

        private void ToggleEditing(bool editing)
        {
            isEditing = editing;

            TaskTitleTextBox.IsReadOnly = !isEditing;
            TaskStatusTextBox.IsReadOnly = !isEditing;
            TaskDescriptionTextBox.IsReadOnly = !isEditing;
            TaskPriorityTextBox.IsReadOnly = !isEditing;
            TaskCategoryComboBox.IsEnabled = isEditing;
            DeadlineTextBox.IsReadOnly = !isEditing;

            EditButton.Content = isEditing ? "Восстановить задачу" : "Восстановить задачу";
        }

        private void ClearTaskFields()
        {
            TaskID = null;
            TaskTitleTextBox.Clear();
            TaskPriorityTextBox.Clear();
            TaskCategoryComboBox.SelectedValue = null;
            TaskDescriptionTextBox.Clear();
            TaskStatusTextBox.Clear();
            DeadlineTextBox.Clear();
        }

        private void SaveTask()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    if (!DateTime.TryParse(DeadlineTextBox.Text, out DateTime deadline))
                    {
                        MessageBox.Show("Некорректный формат даты.");
                        return;
                    }

                    string taskStatus = deadline > DateTime.Now ? "в работе" : "просрочена";

                    if (TaskID.HasValue)
                    {

                        string updateQuery = "UPDATE Tasks SET task_title = @task_title, task_priority = @task_priority, task_category_id = @task_category_id, task_description = @task_description, task_status = @task_status, task_updated_at = @task_updated_at, deadline = @deadline WHERE task_id = @task_id";
                        using (var updateCommand = new SqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@task_title", TaskTitleTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@task_priority", TaskPriorityTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@task_category_id", TaskCategoryComboBox.SelectedValue);
                            updateCommand.Parameters.AddWithValue("@task_description", TaskDescriptionTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@task_status", taskStatus);
                            updateCommand.Parameters.AddWithValue("@task_updated_at", DateTime.Now);
                            updateCommand.Parameters.AddWithValue("@deadline", deadline);
                            updateCommand.Parameters.AddWithValue("@task_id", TaskID.Value);
                            updateCommand.ExecuteNonQuery();
                            MessageBox.Show("Задача успешно восстановлена");
                        }
                    }
                    else
                    {

                        string insertQuery = "INSERT INTO Tasks (task_title, task_priority, task_category_id, task_description, task_status, task_created_at, deadline) VALUES (@task_title, @task_priority, " +
                            "@task_category_id, @task_description, @task_status, @task_created_at, @deadline)";
                        using (var insertCommand = new SqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@task_title", TaskTitleTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@task_priority", TaskPriorityTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@task_category_id", TaskCategoryComboBox.SelectedValue);
                            insertCommand.Parameters.AddWithValue("@task_description", TaskDescriptionTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@task_status", taskStatus);
                            insertCommand.Parameters.AddWithValue("@task_created_at", DateTime.Now);
                            insertCommand.Parameters.AddWithValue("@deadline", deadline);
                            insertCommand.ExecuteNonQuery();
                            MessageBox.Show("Задача успешно восстановлена");
                        }
                    }

                    LoadTasks(DateTime.Now);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            UserProfile userProfile = new UserProfile(userId);
            userProfile.Show();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskID.HasValue)
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    try
                    {
                        string deleteQuery = "DELETE FROM Tasks WHERE task_id = @task_id";
                        using (var deleteCommand = new SqlCommand(deleteQuery, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@task_id", TaskID.Value);
                            int rowsAffected = deleteCommand.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Задача успешно удалена");
                            }
                            else
                            {
                                MessageBox.Show("Задача не найдена или уже удалена");
                            }
                        }

                        LoadTasks(DateTime.Now);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Не выбрана задача для удаления");
            }
        }

        public class CategoryModel
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; }
        }

        public class TaskModel : INotifyPropertyChanged
        {
            private bool isSelected;
            public int TaskId { get; set; }
            public string TaskTitle { get; set; }
            public string TaskPriority { get; set; }
            public int TaskCategoryId { get; set; }
            public string TaskDescription { get; set; }
            public string CategoryName { get; set; }
            public string TaskStatus { get; set; }
            public DateTime? Deadline { get; set; }
            public bool IsChecked
            {
                get => isSelected;
                set
                {
                    if (isSelected != value)
                    {
                        isSelected = value;
                        OnPropertyChanged(nameof(IsChecked));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public class User
        {
            public int UserId { get; set; }
            public string Username { get; set; }
        }

        public class DatabaseHelper
        {
            private readonly string connectionString = "Data Source=LYACSEVNA;Initial Catalog=MyPlansCourseWork;Integrated Security=True;Encrypt=False";

            public List<TaskModel> GetTasks(DateTime date, int userId)
            {
                var tasks = new List<TaskModel>();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT t.task_id, t.task_title, t.task_priority, t.task_category_id, t.task_description, t.task_status, t.deadline, c.category_name 
                FROM Tasks t
                JOIN Categories c ON t.task_category_id = c.category_id
                WHERE t.task_author_id = @user_id
                AND t.task_status IN ('выполнена', 'отменена')";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@user_id", userId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tasks.Add(new TaskModel
                                {
                                    TaskId = (int)reader["task_id"],
                                    TaskTitle = reader["task_title"].ToString(),
                                    TaskPriority = reader["task_priority"].ToString(),
                                    TaskCategoryId = (int)reader["task_category_id"],
                                    TaskDescription = reader["task_description"].ToString(),
                                    TaskStatus = reader["task_status"].ToString(),
                                    CategoryName = reader["category_name"].ToString(),
                                    Deadline = reader["deadline"] as DateTime?
                                });
                            }
                        }
                    }
                }
                return tasks;
            }

            public List<CategoryModel> GetCategories(int userId)
            {
                var categories = new List<CategoryModel>();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT category_id, category_name FROM Categories WHERE user_id = @userId";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                categories.Add(new CategoryModel
                                {
                                    CategoryId = (int)reader["category_id"],
                                    CategoryName = reader["category_name"].ToString()
                                });
                            }
                        }
                    }
                }
                return categories;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (isEditing)
            {
                SaveTask();
                ToggleEditing(false); 
            }
            else
            {
                ToggleEditing(true);
            }
        }
    }
}


