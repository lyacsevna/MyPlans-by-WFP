using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Xceed.Words.NET;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Linq;
using MyPlans_by_WFP.Model;
using System.Drawing;



namespace MyPlans_by_WFP
{
    public partial class TasksWindowView : Window
    {
        private readonly DatabaseHelper databaseHelper;
        private readonly string connectionString = "Data Source=LYACSEVNA;Initial Catalog=MyPlansCourseWork;Integrated Security=True;Encrypt=False";
        private bool isEditing = false;
        private int? TaskID;
        private int userId;
        private DateTime selectedDate;
        private HashSet<DateTime> selectedDates = new HashSet<DateTime>();
        private List<DateTime> deadlines;


        // конфигурация проекта
        //=====================================================================================
        public class LoadedConfig
        {
            public Dictionary<string, string> WindowStates { get; set; } = new Dictionary<string, string>();
            public Dictionary<string, List<string>> FormData { get; set; } = new Dictionary<string, List<string>>();
        }

        private void LoadConfiguration(string filePath)
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var loadedConfig = JsonConvert.DeserializeObject<LoadedConfig>(json);

                if (loadedConfig != null)
                {
                    MessageBox.Show("Конфигурация успешно загружена.");

                    // Восстановление состояния окон
                    if (loadedConfig.WindowStates.TryGetValue("MainForm", out string mainFormState))
                    {
                        this.WindowState = (WindowState)Enum.Parse(typeof(WindowState), mainFormState);
                    }

                    // Восстановление данных форм
                    if (loadedConfig.FormData.TryGetValue("TaskForm", out List<string> taskFormData))
                    {
                        // Пример восстановления данных для формы задач
                        TaskTitleTextBox.Text = taskFormData[0];
                        TaskPriorityTextBox.Text = taskFormData[1];
                        TaskCategoryComboBox.Text = taskFormData[2];
                        TaskDescriptionTextBox.Text = taskFormData[3];
                        TaskStatusTextBox.Text = taskFormData[4];
                        DeadlineTextBox.Text = taskFormData[5];
                    }
                }
            }
        }

        private void SaveConfiguration(string filePath)
        {
            var loadedConfig = new LoadedConfig
            {
                WindowStates = new Dictionary<string, string>
        {
            { "MainForm", this.WindowState.ToString() }
        },
                FormData = new Dictionary<string, List<string>>
        {
            { "TaskForm", new List<string>
                {
                    TaskTitleTextBox.Text,
                    TaskPriorityTextBox.Text,
                    TaskCategoryComboBox.Text,
                    TaskDescriptionTextBox.Text,
                    TaskStatusTextBox.Text,
                    DeadlineTextBox.Text
                }
            }
            
        }
            };

            var json = JsonConvert.SerializeObject(loadedConfig, Formatting.Indented);
            File.WriteAllText(filePath, json);
            MessageBox.Show("Конфигурация успешно сохранена.");
        }

        private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = "Сохранить конфигурацию"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                SaveConfiguration(saveFileDialog.FileName);
            }
        }

        private void LoadConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = "Открыть конфигурацию"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadConfiguration(openFileDialog.FileName);
            }
        }
        //=====================================================================================

        public TasksWindowView(int userId)
        {
            InitializeComponent();
            this.userId = userId;
            databaseHelper = new DatabaseHelper();
            LoadCategories();
            LoadTasks(DateTime.Now);
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            deadlines = databaseHelper.GetDeadlines(userId);
            HighlightDeadlines();
        }

        private void HighlightDeadlines() 
        {
            var tasks = databaseHelper.GetTasks(DateTime.Now, userId);
            foreach (var task in tasks)
            {
                if (task.TaskStatus != "Выполнена" && task.TaskStatus != "Отменена" && task.Deadline.HasValue)
                {
                    if (task.Deadline <= DateTime.Now && task.TaskStatus != "Просрочена")
                    {
                        task.TaskStatus = "Просрочена"; 
                        UpdateTaskStatusInDatabase(task);
                    }

                    TaskCalendar.SelectedDates.Add(task.Deadline.Value.Date);
                }
            }

        }

        public TasksWindowView()
        {
            InitializeComponent();
            databaseHelper = new DatabaseHelper();
            LoadTasks(DateTime.Now);
            HighlightDeadlines();

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        }

        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewCategoryWindow addCategoryWindow = new AddNewCategoryWindow(userId);
            addCategoryWindow.ShowDialog();
            LoadCategories();
        }
       
        private void LoadCategories()
        {
            var categories = databaseHelper.GetCategories(userId);
            TaskCategoryComboBox.ItemsSource = categories;
            TaskCategoryComboBox.DisplayMemberPath = "CategoryName";
            TaskCategoryComboBox.SelectedValuePath = "CategoryId"; 
        }


        private void TaskCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {

            var newSelectedDates = TaskCalendar.SelectedDates;

            selectedDates.Clear();
            foreach (var date in newSelectedDates)
            {
                selectedDates.Add(date.Date);
            }

            if (selectedDates.Count > 0)
            {
                DateTime selectedDate = selectedDates.First();
                SelectedDateTextBlock.Text = $"Задачи на {selectedDate.ToString("dd.MM.yyyy")}";
                var tasksForSelectedDate = databaseHelper.GetTasksByDeadline(selectedDate, userId);
                TaskDataGrid.ItemsSource = tasksForSelectedDate;
            }
            else
            {
                SelectedDateTextBlock.Text = "Все задачи:";
                LoadTasks(DateTime.MinValue);
            }
}

        private void UpdateCalendarSelection()
        {
            TaskCalendar.SelectedDates.Clear();

            foreach (var date in selectedDates)
            {
                TaskCalendar.SelectedDates.Add(date);
            }
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
            }
            else
            {
                ClearTaskFields();
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is TaskModel task)
            {
                task.TaskStatus = "Выполнена";
                UpdateTaskStatusInDatabase(task);
                LoadTasks(selectedDate);
                task.IsChecked = true;
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                TaskModel task = checkBox.DataContext as TaskModel;
                if (task != null)
                {
                    UpdateTaskStatusInDatabase(task);
                    LoadTasks(DateTime.Now);
                }
            }
        }

        private void StatusMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (TaskDataGrid.SelectedItem is TaskModel selectedTask)
            {
                MenuItem menuItem = sender as MenuItem;
                string newStatus = menuItem.Tag.ToString();
                selectedTask.TaskStatus = newStatus;
                UpdateTaskStatusInDatabase(selectedTask);
                LoadTasks(DateTime.Now);
            }
        }

        private void UpdateTaskStatusInDatabase(TaskModel task)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    string updateQuery = "UPDATE Tasks SET task_status = @task_status WHERE task_id = @task_id";
                    using (var updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@task_status", task.TaskStatus);
                        updateCommand.Parameters.AddWithValue("@task_id", task.TaskId);
                        updateCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении статуса задачи: {ex.Message}");
                }
            }

        }

        private void LoadTasks(DateTime selectedDate)
        {
            List<TaskModel> tasks;

            if (selectedDate == DateTime.MinValue)
            {
                tasks = databaseHelper.GetTasks(DateTime.Now, userId)
                    .Where(t => t.TaskStatus != "Выполнена" && t.TaskStatus != "Отменена")
                    .ToList();
            }
            else
            {
                tasks = databaseHelper.GetTasks(selectedDate, userId)
                    .Where(t => t.TaskStatus != "Выполнена" && t.TaskStatus != "Отменена")
                    .ToList();
            }

            TaskDataGrid.ItemsSource = tasks;
            ClearTaskFields();
            HighlightDeadlines();

        }

        private void ExportToExcel(){
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog{
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "Сохранить задачи как",
                FileName = "Tasks.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true){
                var tasks = TaskDataGrid.ItemsSource as List<TaskModel>;
                if (tasks == null || tasks.Count == 0){
                    MessageBox.Show("Нет данных для экспорта.");
                    return;
                }

                try{
                    using (ExcelPackage excelPackage = new ExcelPackage())
                    {
                        var worksheet = excelPackage.Workbook.Worksheets.Add("Tasks");
                        worksheet.Cells[1, 1].Value = "ID Задачи";
                        worksheet.Cells[1, 2].Value = "Название";
                        worksheet.Cells[1, 3].Value = "Приоритет";
                        worksheet.Cells[1, 4].Value = "Категория";
                        worksheet.Cells[1, 5].Value = "Описание";
                        worksheet.Cells[1, 6].Value = "Статус";
                        worksheet.Cells[1, 7].Value = "Срок исполнения";
                        for (int i = 0; i < tasks.Count; i++){
                            var task = tasks[i];
                            worksheet.Cells[i + 2, 1].Value = task.TaskId;
                            worksheet.Cells[i + 2, 2].Value = task.TaskTitle;
                            worksheet.Cells[i + 2, 3].Value = task.TaskPriority;
                            worksheet.Cells[i + 2, 4].Value = task.TaskCategoryId;
                            worksheet.Cells[i + 2, 5].Value = task.TaskDescription;
                            worksheet.Cells[i + 2, 6].Value = task.TaskStatus;
                            worksheet.Cells[i + 2, 7].Value = task.Deadline.ToString();
                        }
                        FileInfo excelFile = new FileInfo(saveFileDialog.FileName);
                        excelPackage.SaveAs(excelFile);
                    }
                    MessageBox.Show($"Задачи успешно экспортированы в {saveFileDialog.FileName}");
                }
                catch (Exception ex){
                    MessageBox.Show($"Ошибка при экспорте: {ex.Message}");
                }
            }
        }

        private void ExportToExcel_Button_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void ExportToDocx(TaskModel selectedTask)
        {
            if (selectedTask == null)
            {
                MessageBox.Show("Выберите задачу для экспорта.");
                return;
            }

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Word Documents (*.docx)|*.docx",
                Title = "Сохранить задачу как",
                FileName = $"Task_{selectedTask.TaskId}.docx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var document = DocX.Create(saveFileDialog.FileName))
                {
                    document.InsertParagraph("Данные о задаче").FontSize(20).Bold().SpacingAfter(20);
                    document.InsertParagraph($"ID Задачи: {selectedTask.TaskId}").FontSize(14).SpacingAfter(10);
                    document.InsertParagraph($"Название: {selectedTask.TaskTitle}").FontSize(14).SpacingAfter(10);
                    document.InsertParagraph($"Приоритет: {selectedTask.TaskPriority}").FontSize(14).SpacingAfter(10);
                    document.InsertParagraph($"Категория: {selectedTask.TaskCategoryId}").FontSize(14).SpacingAfter(10);
                    document.InsertParagraph($"Описание: {selectedTask.TaskDescription}").FontSize(14).SpacingAfter(10);
                    document.InsertParagraph($"Статус: {selectedTask.TaskStatus}").FontSize(14).SpacingAfter(10);
                    document.InsertParagraph($"Срок исполнения: {selectedTask.Deadline:d}").FontSize(14).SpacingAfter(10);
                    document.Save();
                }

                MessageBox.Show($"Задача успешно экспортирована в {saveFileDialog.FileName}");
            }
        }

        private void Export_Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedTask = (TaskModel)TaskDataGrid.SelectedItem;
            ExportToDocx(selectedTask);
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


        private void Add_Task_Button_Click(object sender, RoutedEventArgs e)
        {
            AddTaskWindow window = new AddTaskWindow(userId);
            window.TaskAdded += (s, args) => LoadTasks(DateTime.Now);
            window.Show();
            HighlightDeadlines();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleEditing();
            if (!isEditing)
            {
                SaveTask();
                HighlightDeadlines();

            }
        }

        private void ExportToXml(TaskModel selectedTask)
        {
            if (selectedTask == null)
            {
                MessageBox.Show("Выберите задачу для экспорта.");
                return;
            }

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml",
                Title = "Сохранить задачу как",
                FileName = $"Task_{selectedTask.TaskId}.xml"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var xmlDoc = new XDocument(
                        new XElement("Task",
                            new XElement("TaskId", selectedTask.TaskId),
                            new XElement("TaskTitle", selectedTask.TaskTitle),
                            new XElement("TaskPriority", selectedTask.TaskPriority),
                            new XElement("TaskCategoryId", selectedTask.TaskCategoryId),
                            new XElement("TaskDescription", selectedTask.TaskDescription),
                            new XElement("TaskStatus", selectedTask.TaskStatus),
                            new XElement("Deadline", selectedTask.Deadline.ToString()))
                    );

                    xmlDoc.Save(saveFileDialog.FileName);
                    MessageBox.Show($"Задача успешно экспортирована в {saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте задачи: {ex.Message}");
                }
            }
        }

        private void ToggleEditing()
        {
            isEditing = !isEditing;

            TaskTitleTextBox.IsReadOnly = !isEditing;
            TaskStatusTextBox.IsReadOnly = !isEditing;
            TaskDescriptionTextBox.IsReadOnly = !isEditing;
            TaskPriorityTextBox.IsReadOnly = !isEditing;
            TaskCategoryComboBox.IsEnabled = isEditing;

            
            AddCategoryButton.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;

            DeadlineTextBox.IsReadOnly = !isEditing;
            EditButton.Content = isEditing ? "Сохранить" : "Редактировать";
        }

        private void SaveTask(){
            using (var connection = new SqlConnection(connectionString)){
                connection.Open();
                try {
                    if (TaskID.HasValue){
                        string updateQuery = "UPDATE Tasks SET task_title = @task_title, task_priority = @task_priority, " +
                            "task_category_id = @task_category_id, task_description = @task_description, task_status = @task_status," +
                            " task_updated_at = @task_updated_at, deadline = @deadline WHERE task_id = @task_id";
                        using (var updateCommand = new SqlCommand(updateQuery, connection)){
                            updateCommand.Parameters.AddWithValue("@task_title", TaskTitleTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@task_priority", TaskPriorityTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@task_category_id", TaskCategoryComboBox.SelectedValue);
                            updateCommand.Parameters.AddWithValue("@task_description", TaskDescriptionTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@task_status", TaskStatusTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@task_updated_at", DateTime.Now);

                            if (!DateTime.TryParse(DeadlineTextBox.Text, out DateTime deadline)){
                                MessageBox.Show("Некорректный формат даты.");
                                return;
                            }
                            updateCommand.Parameters.AddWithValue("@deadline", deadline);
                            updateCommand.Parameters.AddWithValue("@task_id", TaskID.Value);
                            updateCommand.ExecuteNonQuery();
                            MessageBox.Show("Задача успешно отредактирована");
                        }
                    }
                    else{
                        string insertQuery = "INSERT INTO Tasks (task_title, task_priority, task_category_id, task_description, task_status," +
                            " task_created_at, deadline) VALUES (@task_title, @task_priority, @task_category_id, @task_description, @task_status," +
                            " @task_created_at, @deadline)";
                        using (var insertCommand = new SqlCommand(insertQuery, connection)){
                            insertCommand.Parameters.AddWithValue("@task_title", TaskTitleTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@task_priority", TaskPriorityTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@task_category_id", TaskCategoryComboBox.SelectedValue); 
                            insertCommand.Parameters.AddWithValue("@task_description", TaskDescriptionTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@task_status", TaskStatusTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@task_created_at", DateTime.Now);

                            if (!DateTime.TryParse(DeadlineTextBox.Text, out DateTime deadline)){
                                MessageBox.Show("Некорректный формат даты.");
                                return;
                            }
                            insertCommand.Parameters.AddWithValue("@deadline", deadline);
                            insertCommand.ExecuteNonQuery();
                            MessageBox.Show("Задача успешно добавлена");
                        }
                    }

                    LoadTasks(DateTime.Now);
                    LoadCategories();
                }
                catch (Exception ex){
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }


        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var dataContext = DataContext as dynamic;
            var username = dataContext?.Username;

            if (userId <= 0)
            {
                MessageBox.Show("Ошибка: недопустимый идентификатор пользователя.");
                return;
            }


            UserProfile userProfile = new UserProfile(userId);


            userProfile.Show();


            LoadCategories();
            LoadTasks(DateTime.Now);
            HighlightDeadlines();


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
        
        private void ImportFromExcel()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog{
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "Выберите файл для импорта задач"
            };

            if (openFileDialog.ShowDialog() == true){
                try{
                    using (var package = new ExcelPackage(new FileInfo(openFileDialog.FileName))){
                        var worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;
                        List<TaskModel> tasks = new List<TaskModel>();
                        for (int row = 2; row <= rowCount; row++) {
                            try{
                                TaskModel task = new TaskModel{
                                    TaskId = Convert.ToInt32(worksheet.Cells[row, 1].Value),
                                    TaskTitle = worksheet.Cells[row, 2].Value?.ToString() ?? string.Empty,
                                    TaskPriority = worksheet.Cells[row, 3].Value?.ToString() ?? string.Empty,
                                    TaskCategoryId = Convert.ToInt32(worksheet.Cells[row, 4].Value),
                                    TaskDescription = worksheet.Cells[row, 5].Value?.ToString() ?? string.Empty,
                                    TaskStatus = worksheet.Cells[row, 6].Value?.ToString() ?? string.Empty,
                                    Deadline = DateTime.TryParse(worksheet.Cells[row, 7].Value?.ToString(), out DateTime deadline) ? deadline : DateTime.MinValue
                                };
                                tasks.Add(task);
                            }
                            catch (Exception ex){
                                MessageBox.Show($"Ошибка при чтении строки {row}: {ex.Message}");
                            }
                        }
                        TaskDataGrid.ItemsSource = tasks;
                        MessageBox.Show("Задачи успешно импортированы из файла.");
                    }
                }
                catch (Exception ex){
                    MessageBox.Show($"Ошибка при импорте: {ex.Message}");
                }
            }
        }

        private void Import_Button_Click(object sender, RoutedEventArgs e)
        {
            ImportFromExcel();
        }

        private void ExportXMLButton_Click(object sender, RoutedEventArgs e)
        {
            TaskModel selectedTask = (TaskModel)TaskDataGrid.SelectedItem;
            ExportToXml(selectedTask);
        }

        public User GetUserById(int userId)
        {
            User user = null;
            string connectionString = "Data Source=LYACSEVNA;Initial Catalog=MyPlansCourseWork;Integrated Security=True;Encrypt=False";
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

        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            CategoryManagement categoryManagement = new CategoryManagement(userId);
            categoryManagement.Show();
            LoadCategories();
            HighlightDeadlines();
        }

        private void ArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            ArchiveTasks archiveTasks = new ArchiveTasks(userId);
            archiveTasks.Show();
            LoadTasks(DateTime.Now);
            LoadCategories();
            HighlightDeadlines();
        }

        private void ShowAllTasksButton_Click(object sender, RoutedEventArgs e)
        {
            LoadTasks(DateTime.MinValue);
            SelectedDateTextBlock.Text = "Все задачи";
            HighlightDeadlines();
        }
    }


    public class CategoryModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
    }

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

