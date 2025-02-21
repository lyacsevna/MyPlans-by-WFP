using Microsoft.Win32;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MyPlans_by_WFP
{
    public partial class UserProfile : Window
    {
        private bool isEditing = false;
        private int userId;
        private string connectionString = "Data Source=LYACSEVNA;Initial Catalog=MyPlansCourseWork;Integrated Security=True;Encrypt=False";

        public UserProfile(int userId)
        {
            InitializeComponent();
            this.userId = userId;
            LoadUserProfile(userId);
        }

        private void LoadUserProfile(int userId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT username, email, photo FROM Users WHERE user_id = @userId";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string username = reader["username"].ToString();
                                string email = reader["email"].ToString();
                                byte[] photoData = reader["photo"] as byte[];
                                if (photoData != null && photoData.Length > 0)
                                {
                                    using (MemoryStream ms = new MemoryStream(photoData))
                                    {
                                        BitmapImage bitmap = new BitmapImage();
                                        bitmap.BeginInit();
                                        bitmap.StreamSource = ms;
                                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                        bitmap.EndInit();
                                        bitmap.Freeze();
                                        ProfileImage.Source = bitmap;
                                    }
                                }
                                else
                                {
                                    string defaultImagePath = @"E:\MyPlans by WFP\Model\ava.jpg";
                                    ProfileImage.Source = new BitmapImage(new Uri(defaultImagePath));
                                }

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    UsernameTextBox.Text = username;
                                    EmailTextBox.Text = email;
                                });
                            }
                            else
                            {
                                MessageBox.Show("Пользователь не найден.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке профиля: {ex.Message}");
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            isEditing = !isEditing;


            UsernameTextBox.IsReadOnly = !isEditing;
            EmailTextBox.IsReadOnly = !isEditing;

            CancelButton.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            EditButton.Content = isEditing ? "Сохранить" : "Редактировать";
            if (!isEditing)
            {
                SaveButton_Click(sender, e);
            }
        }

        private void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите фото профиля"
            };

            if (openFileDialog.ShowDialog() == true)
            {

                ProfileImage.Source = new BitmapImage(new Uri(openFileDialog.FileName));

                SavePhotoToDatabase(openFileDialog.FileName);
            }
        }

        private void SavePhotoToDatabase(string filePath)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE Users SET photo = @photo WHERE user_id = @userId";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);

                        byte[] photoData;
                        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            using (BinaryReader br = new BinaryReader(fs))
                            {
                                photoData = br.ReadBytes((int)fs.Length);
                            }
                        }

                        command.Parameters.AddWithValue("@photo", photoData);
                        command.ExecuteNonQuery();
                        MessageBox.Show("Фото успешно сохранено в базе данных!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении фото в базе данных: {ex.Message}");
            }
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE Users SET username = @username, email = @email, photo = @photo WHERE user_id = @userId";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", UsernameTextBox.Text);
                        command.Parameters.AddWithValue("@email", EmailTextBox.Text);
                        command.Parameters.AddWithValue("@userId", userId);

                        if (ProfileImage.Source is BitmapImage bitmapImage)
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                BitmapEncoder encoder = new PngBitmapEncoder();
                                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                                encoder.Save(ms);
                                command.Parameters.AddWithValue("@photo", ms.ToArray());
                            }
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@photo", DBNull.Value);
                        }

                        command.ExecuteNonQuery();
                        MessageBox.Show("Профиль успешно обновлен!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении профиля: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Изменения отменены!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            isEditing = false;
            EditButton_Click(sender, e);
            LoadUserProfile(userId);
        }

        public void UpdatePassword(int userId, string newPassword)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE Users SET password = @password WHERE user_id = @userId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@password", newPassword); 
                    command.Parameters.AddWithValue("@userId", userId);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        throw new Exception("Не удалось обновить пароль. Пользователь не найден.");
                    }
                }
            }
        }

        private void ChangePasswordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            PasswordInputDialog passwordDialog = new PasswordInputDialog();
            if (passwordDialog.ShowDialog() == true)
            {
                string oldPassword = passwordDialog.OldPassword;
                string newPassword = passwordDialog.NewPassword;

                if (IsOldPasswordCorrect(userId, oldPassword))
                {
                    try
                    {
                        UpdatePassword(userId, newPassword);
                        MessageBox.Show("Пароль успешно обновлен.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при обновлении пароля: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Старый пароль неверен. Попробуйте снова.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private bool IsOldPasswordCorrect(int userId, string oldPassword)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM Users WHERE user_id = @userId AND password = @oldPassword";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@oldPassword", oldPassword);
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        private void DeleteAccountMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить свой аккаунт?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    DeleteUser(userId);
                    MessageBox.Show("Аккаунт успешно удален.");
                    CloseAllWindowsAndOpenMain();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении аккаунта: {ex.Message}");
                }
            }
        }

        public void DeleteUser(int userId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "DELETE FROM Users WHERE user_id = @userId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        throw new Exception("Не удалось удалить пользователя. Пользователь не найден.");
                    }
                }
            }
        }

        private void CloseAllWindowsAndOpenMain()
        {
            Application.Current.Shutdown();
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private void Back_Button(object sender, RoutedEventArgs e)
        {
            Close();
            TasksWindowView tasksWindowView = new TasksWindowView();
            tasksWindowView.Show();
        }
    }
}

