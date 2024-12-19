using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Security; // Add this line for SecurityException
using System.Windows.Forms;
using System.Drawing;

[SupportedOSPlatform("windows")]
class AddToPathApp
{
[STAThread]
static void Main(string[] args)
{
     try
     {
          // Check if running with admin privileges and restart with elevation if needed
          if (!IsUserAdministrator())
          {
               RestartWithElevatedPrivileges(args);
               return;
          }

          string pathToAdd;

          if (args.Length > 0)
          {
               pathToAdd = args[0];
               if (!Directory.Exists(pathToAdd))
               {
                    Console.WriteLine($"Error: Directory '{pathToAdd}' does not exist.");
                    return;
               }
          }
          else
          {
               // Use the application's directory if no path is provided
               pathToAdd = AppContext.BaseDirectory;
               Console.WriteLine($"No path provided. Using application directory: {pathToAdd}");
          }

          // Get the current PATH variable
          string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;
          var paths = currentPath.Split(Path.PathSeparator).ToList();

          // Check if path already exists
          if (paths.Any(p => string.Equals(p, pathToAdd, StringComparison.OrdinalIgnoreCase)))
          {
               Console.WriteLine($"Path '{pathToAdd}' is already in the system PATH.");
               ShowTooltip("Folder already exists", pathToAdd);
               return;
          }

          // Add the new path
          paths.Add(pathToAdd);
          string newPath = string.Join(Path.PathSeparator, paths);

          // Set the new PATH
          Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Machine);
          Console.WriteLine($"Successfully added '{pathToAdd}' to system PATH.");
          ShowTooltip("Added successfully", pathToAdd);
     }
     catch (SecurityException)
     {
          Console.WriteLine("Access denied. Please run as administrator.");
     }
     catch (Exception ex)
     {
          Console.WriteLine($"An error occurred: {ex.Message}");
     }
}

private static bool IsUserAdministrator()
{
     try
     {
          using WindowsIdentity? identity = WindowsIdentity.GetCurrent();
          if (identity == null) return false;

          WindowsPrincipal principal = new WindowsPrincipal(identity);
          return principal.IsInRole(WindowsBuiltInRole.Administrator);
     }
     catch (Exception)
     {
          return false;
     }
}

private static void RestartWithElevatedPrivileges(string[] args)
{
     try
     {
          // Get the path of the current executable
          using var currentProcess = Process.GetCurrentProcess();
          var mainModule = currentProcess.MainModule;
          if (mainModule?.FileName == null)
          {
               Console.WriteLine("Unable to determine the application path.");
               return;
          }

          string exePath = mainModule.FileName;

          // Prepare process start info for elevated privileges
          ProcessStartInfo startInfo = new ProcessStartInfo
          {
               UseShellExecute = true,
               WorkingDirectory = Environment.CurrentDirectory,
               FileName = exePath,
               Arguments = string.Join(" ", args.Select(arg => $"\"{arg}\"")),
               Verb = "runas" // This triggers the UAC prompt
          };

          // Start the new process
          using var process = Process.Start(startInfo);
          if (process == null)
          {
               Console.WriteLine("Failed to start elevated process.");
               return;
          }
     }
     catch (Exception ex)
     {
          Console.WriteLine("Failed to restart with elevated privileges: " + ex.Message);
     }
}

static void ShowTooltip(string status, string folderPath)
{
     Application.EnableVisualStyles();

     // Create tooltip form
     using Form tooltipForm = new Form
     {
          FormBorderStyle = FormBorderStyle.None,
          ShowInTaskbar = false,
          StartPosition = FormStartPosition.Manual,
          Size = new Size(Screen.PrimaryScreen.WorkingArea.Width / 3, 100), // Form size 400 x 50
          BackColor = Color.Black,
          Opacity = 0.9,
          TopMost = true // Make sure tooltip appears on top
     };

     // Position the form at bottom right of screen
     tooltipForm.Location = new Point(
          Screen.PrimaryScreen.WorkingArea.Width - tooltipForm.Width - 5,
          Screen.PrimaryScreen.WorkingArea.Height - tooltipForm.Height - 5
     );     
     // Create label for the message
     using var label = new Label
     {
          Dock = DockStyle.Fill,
          Text = $"Status: {status}\nPath: {folderPath}",
          ForeColor = Color.White,
          TextAlign = ContentAlignment.MiddleLeft,
          Font = new Font("Consolas", 10),
          Padding = new Padding(10)
     };

     tooltipForm.Controls.Add(label);

     // Show the form
     tooltipForm.Show();

     // Close the form after 1 second
     using var timer = new System.Windows.Forms.Timer
     {
          Interval = 5000
     };

     timer.Tick += (sender, e) =>
     {
          tooltipForm.Close();
          timer.Stop();
          Application.ExitThread(); // Ensure the application exits after showing the tooltip
     };
     timer.Start();

     // Run the message loop to ensure the form displays
     Application.Run();
}
}
