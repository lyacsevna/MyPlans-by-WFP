using MyPlans_by_WFP.Model;
using System.Windows;

using System.Linq;

using System.Windows.Media;
using System.Data.SqlClient;
using System;

namespace MyPlans_by_WFP
{

    public partial class SignInWindow : Window
    {
        public SignInWindow()
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

        public void Sign_In_Button_Click(object sender, RoutedEventArgs e) {
            Close();
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private void Button_Sign_Up_Click(object sender, RoutedEventArgs e)
        {
            string login = textBoxLogin.Text.Trim();
            string pass = passBox.Password.Trim();
            string pass_2 = passBox2.Password.Trim();
            string email = textBoxEmail.Text.Trim().ToLower();

            bool isValid = true; 

            
            if (login.Length < 5)
            {
                textBoxLogin.ToolTip = "Логин должен содержать не менее 5 символов.";
                textBoxLogin.Background = Brushes.Red; 
                isValid = false;
            }
            else
            {
                textBoxLogin.ToolTip = "";
                textBoxLogin.Background = Brushes.Transparent;
            }

            // Проверка пароля
            if (pass.Length < 5)
            {
                passBox.ToolTip = "Пароль должен содержать не менее 5 символов.";
                passBox.Background = Brushes.Red; // Подсветка ошибки
                isValid = false;
            }
            else
            {
                passBox.ToolTip = "";
                passBox.Background = Brushes.Transparent;
            }

   
            if (pass != pass_2)
            {
                passBox2.ToolTip = "Пароли не совпадают.";
                passBox2.Background = Brushes.Red; 
                isValid = false;
            }
            else
            {
                passBox2.ToolTip = "";
                passBox2.Background = Brushes.Transparent;
            }


            if (email.Length < 5 || !email.Contains("@") || !email.Contains("."))
            {
                textBoxEmail.ToolTip = "Введите корректный email.";
                textBoxEmail.Background = Brushes.Red;
                isValid = false;
            }
            else
            {
                textBoxEmail.ToolTip = "";
                textBoxEmail.Background = Brushes.Transparent;
            }

            if (isValid)
            {
                try
                {
                    int userId = IdFromDatabase(email, pass);
                    if (userId > 0)
                    {
                        MessageBox.Show("Пользователь с таким email уже существует.");
                        return;
                    }

                    int newUserId = AddUserToDatabase(login, email, pass);
                    if (newUserId > 0)
            {
                        MessageBox.Show("Регистрация прошла успешно.");
                        TasksWindowView tasksWindow = new TasksWindowView(newUserId);
                        tasksWindow.Show();
                        this.Close();
                    }
            else
                    {
                        MessageBox.Show("Произошла ошибка при регистрации.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

       

        private int AddUserToDatabase(string login, string email, string password)
        {
            int userId = -1;

            using (SqlConnection connection = new SqlConnection("Data Source=LYACSEVNA;Initial Catalog=MyPlansCourseWork;Integrated Security=True;Encrypt=False"))
            {
                connection.Open();
                string query = "INSERT INTO Users (username, email, password) VALUES (@Login, @Email, @Password); SELECT SCOPE_IDENTITY()";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Login", login);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Password", password);

                    object result = command.ExecuteScalar();

                    if (result != null)
                    {
                        userId = Convert.ToInt32((decimal)result);
                    }
                }
            }
            return userId;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}