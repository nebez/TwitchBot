using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ConsoleLogger
{
    public class Logger
    {
        public static Logger Log;

        //Private members
        private string _logFile;
        private FileStream _logStream;
        private bool _showtime;
        private bool _showthread;

        /// <summary>
        /// Full file path to current log file
        /// </summary>
        public string LogFile
        {
            get
            {
                return _logFile;
            }
        }

        //Public Methods/Functions
        /// <summary>
        /// Similar to Console.Read, but automatically prefixed with a timestamp and can be color coded
        /// depending on type (input, info, warning, error), and is saved to log file.
        /// Defaults to INFO type.
        /// </summary>
        /// <param name="msg">String to be displayed</param>
        public void Write(string msg)
        {
            Write(msg, ConsoleColor.Gray);
        }

        /// <summary>
        /// Similar to Console.Read, but automatically prefixed with a timestamp and can be color coded
        /// depending on type (input, info, warning, error), and is saved to log file.
        /// </summary>
        /// <param name="type">Type of output</param>
        /// <param name="msg">String to be displayed</param>
        public void Write(string msg, ConsoleColor color)
        {
            string message;
            string consolemessage;
            byte[] buffer;
            int lines;

            Console.ForegroundColor = color;

            //Message for the log file
            message = "[" + Timestamp() + "] " + "[" + System.Threading.Thread.CurrentThread.Name + "] " + msg;

            //Message for the console
            consolemessage = msg;
            if(_showthread)
                consolemessage = "[" + System.Threading.Thread.CurrentThread.Name + "] " + consolemessage;
            if (_showtime)
                consolemessage = "[" + Timestamp() + "] " + consolemessage;

            //We also remove ASCII bell char (0x07 \a) from console to avoid that annoying beeping sound
            consolemessage = consolemessage.Replace('\a', '_');

            buffer = Encoding.ASCII.GetBytes(message + System.Environment.NewLine);
            lines = (int)Math.Ceiling((double)buffer.Length / Console.BufferWidth);

            //Write to console...
            Console.WriteLine(consolemessage);
            Console.ForegroundColor = ConsoleColor.Gray; //Return to gray color

            //... now write to log file
            _logStream.Write(buffer, 0, buffer.Length);
            _logStream.Flush();
        }

        /// <summary>
        /// A function for timestamps in HH:mm:ss.fff format. (ex: 10:34:56.243)
        /// </summary>
        /// <returns>Returns a string in specified format</returns>
        public static string Timestamp()
        {
            return DateTime.Now.ToString("HH:mm:ss.fff");
        }

        //Constructors
        /// <summary>
        /// Modified console window to accomodate for visual logging and file logging
        /// </summary>
        /// <param name="logfilename">Name of file log (exclude .txt)</param>
        /// <param name="title">Title of console window</param>
        /// <param name="width">Console buffer width</param>
        /// <param name="height">Console buffer height</param>
        public Logger(string logfilename, bool showtime, bool showthread)
        {
            _logFile = Path.Combine(Environment.CurrentDirectory, "logs", logfilename + ".txt");
            _showthread = showthread;
            _showtime = showtime;
            
            //Do some moving around! (we check for file existence not folder as there could be multiple loggers on seperate threads)
            if (File.Exists(_logFile))
            {   //Log file already exists, move the directory!
                string oldLogPath = Path.Combine(Environment.CurrentDirectory, "oldlogs");
                if(!Directory.Exists(oldLogPath))
                    Directory.CreateDirectory(oldLogPath);
                //Find a new name for the log folder and move it
                string newName = "logs" + Directory.GetDirectories(oldLogPath, "logs*").Count();
                Directory.Move(Path.GetDirectoryName(_logFile), Path.Combine(oldLogPath, newName));
            }

            //Make our log file directory!
            if (!Directory.Exists(Path.GetDirectoryName(_logFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(_logFile));
            _logStream = new FileStream(_logFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        }
    }
}
