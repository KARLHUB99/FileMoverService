FileMoverService
A Windows Service that monitors a source folder and moves files to a target folder, with comprehensive logging and error handling.

Features
Watches a specified folder for new or renamed files.

Moves files safely after ensuring they are ready (not locked).

Logs important events to both Windows Event Log and rolling daily log files.

Supports multiple logging implementations via a flexible ILogger interface.

Avoids duplicate processing using debounce logic.

Getting Started
Prerequisites
Windows OS

.NET Framework compatible with your project

Administrative privileges to install Windows Services

Installation
Clone the repository:

bash
Copy
Edit
git clone https://github.com/KARLHUB99/FileMoverService.git
cd FileMoverService
Build the solution:

Open the solution in Visual Studio and build it.

Install the Windows Service:

Open an elevated Command Prompt and run:

bash
Copy
Edit
sc create FileMoverService binPath= "C:\Path\To\FileMoverService.exe"
Replace the path with your actual executable location.

Start the service:

bash
Copy
Edit
sc start FileMoverService
Configuration
The source and target folders are configured in the service code constants:

csharp
Copy
Edit
private const string SourceFolder = @"C:\A Folder";
private const string TargetFolder = @"C:\B Folder";
Logs are stored in the Logs folder inside the application base directory.

Logging
Uses a composite logger to log to both Windows Event Log and daily rolling files.

Logs informational and error messages with timestamps.

Gracefully handles logging failures.

Contributing
Feel free to fork the repository and submit pull requests for improvements or bug fixes.

License
This project is licensed under the MIT License.

Developed by KARL
