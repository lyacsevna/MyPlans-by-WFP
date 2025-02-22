# Planner app

The planner app is a convenient tool for managing daily tasks. It provides users with the ability to easily record, sort and organize their affairs. This application was implemented by me as part of my coursework at the university. Stack - C# + WPF + Microsoft SQL Server

<image src="https://github.com/lyacsevna/MyPlans/blob/master/mainwindow.png?raw=true" alt=" Main window ">
<image src="https://github.com/lyacsevna/MyPlans/blob/master/addtask.png?raw=true" alt= "Adding a task">

## Main functions

- Adding a task: the user can add new tasks by entering a title and description, as well as setting the due date and time.
- Task Priority: ability to set a priority level ( high , medium, low) for each task, helping you focus on the most important things.
- Creating categories: the user can create their own task categories (e.g. work, study, personal matters, sports, etc.).
- Categorization on Add: when adding a new task, the user can select a category from a drop-down list, making it easier to organize tasks.
- Import and export tasks in .docx .xml and .xlsx formats.
- Automatic archiving of tasks with the statuses "completed" and "canceled", as well as the ability to restore tasks from the archive.
- Separation of tasks by users: each user is provided with separate access to the application functionality. For this purpose, registration and authorization functions are implemented, which allows users to create their own accounts and log in to the system.

## Installing and building the project

Please follow the steps below to install and build the project on your computer.

### Prerequisites

Before you begin, make sure you have the following programs installed:

- [ Visual Studio ](https://visualstudio.microsoft.com/) (version 2019 or newer recommended)
- [.NET 5.0](https://dotnet.microsoft.com/download/dotnet/5.0) or later

### Cloning a repository

1. Open the terminal (or command prompt).
2. Clone the repository using the following command:

Copy
``` bash
git clone https://github.com/lyacsevna/MyPlans.git
```
3. Go to the project directory:
``` bash
cd MyPlans
```
### Building the project

1. Open the solution in Visual Studio Studio by double-clicking the `. sln ` file located in the root of the .
2. Make sure you have the correct build configuration selected ( Debug or Release ).
3. Build the project by pressing ` Ctrl + Shift + B` or by selecting ` Build -> Build Solution ` from the menu.

### Launching the application

1. After successful build, run the application by pressing `F5` or selecting ` Debug -> Start Debugging `.
2. The application should open the program window.

### Installing dependencies

If the project uses third-party libraries (for example, via NuGet ), perform a package restore:

- Open `Package Manager Console` in Visual Studio.
- Do it command :
```bash
powershell
Update-Package-Reinstall
```
### Troubleshooting

If you have any problems with installation or assembly, please check the following points:

- Make sure all dependencies are installed.
- Check that you have the correct version of .NET installed.
- Look in ` Output ` or ` Error List ` in V
