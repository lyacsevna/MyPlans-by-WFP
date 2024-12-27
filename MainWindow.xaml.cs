using MyPlans_by_WFP.Model;
using System.Data.SqlClient;
using System;
using System.Windows;

namespace MyPlans_by_WFP
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        private int IdFromDatabase(string email, string password)
        {
            int userId = -1;

            using (SqlConnection connection = new SqlConnection("Data Source=LYACSEVNA;Initial Catalog=MyPlansCourseWork;Integrated Security=True;Encrypt=False"))
            {
                connection.Open();
                string query = "SELECT user_id FROM Users WHERE email = @Email AND password = @Password";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Password", password);

                    object result = command.ExecuteScalar();

                    if (result != null)
                    {
                        userId = Convert.ToInt32(result);
                    }
                }
            }
            return userId;
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            
            string email = textBoxLogin.Text.Trim();
            string password = passBox.Password.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, введите email и пароль.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int userId = IdFromDatabase(email, password);

                if (userId > 0)
                {
                    TasksWindowView tasksWindow = new TasksWindowView(userId);
                    tasksWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Неверный email или пароль.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SignUpButton_Click(object sender, RoutedEventArgs e)
        {
            SignInWindow tasksWindow = new SignInWindow();
            tasksWindow.Show();
            this.Close();
        }
    }
}
