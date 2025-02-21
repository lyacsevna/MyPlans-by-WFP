using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPlans_by_WFP
{
    public class DataBaseHelper
    {
        private readonly string connectionString = "Data Source=LYACSEVNA;Initial Catalog=MyPlansCourseWork;Integrated Security=True;Encrypt=False";

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
    

    public class TaskModel
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
                WHERE t.task_author_id = @user_id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@user_id", userId);
                    SqlDataReader reader = command.ExecuteReader();
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
            return tasks;
        }
        public List<TaskModel> GetTasksByDeadline(DateTime deadline, int userId)
        {
            var tasks = new List<TaskModel>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
        SELECT t.task_id, t.task_title, t.task_priority, t.task_category_id, t.task_description, t.task_status, t.deadline, c.category_name 
        FROM Tasks t
        JOIN Categories c ON t.task_category_id = c.category_id
        WHERE t.task_author_id = @user_id AND t.deadline = @deadline
        AND t.task_status IN ('просрочена', 'отложена', 'в работе')";


                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@deadline", deadline.Date);
                    command.Parameters.AddWithValue("@user_id", userId);
                    SqlDataReader reader = command.ExecuteReader();
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
                    SqlDataReader reader = command.ExecuteReader();
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
            return categories;
        }

        public List<DateTime> GetDeadlines(int userId)
        {
            var deadlines = new List<DateTime>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT deadline FROM Tasks WHERE task_author_id = @user_id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@user_id", userId);
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["deadline"] != DBNull.Value)
                        {
                            deadlines.Add((DateTime)reader["deadline"]);

                        }
                    }
                }
            }
            return deadlines;
        }

        public User GetUserById(int userId)
        {
            User user = null;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT user_id, username FROM Users WHERE user_id = @user_id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@user_id", userId);
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        user = new User
                        {
                            UserId = (int)reader["user_id"],
                            Username = reader["username"].ToString()
                        };
                    }
                }
            }
            return user;
        }
    }
}




