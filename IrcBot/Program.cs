using CSScriptLibrary;
using ConsoleLogger;
using IrcBot.Scripts;
using IrcBot.Objects;
using System;
using System.Threading;

namespace IrcBot
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //Create our logger
                Thread.CurrentThread.Name = "twitchbot";
                Logger.Log = new Logger("ircbot", false, false);
                Logger.Log.Write("Created logger", ConsoleColor.DarkGray);

                //Load up dat script
                Logger.Log.Write("Compiling scripts...", ConsoleColor.DarkGray);
                try
                {
                    Scripting.Script = CSScript.Evaluator.LoadFile<Interfaces.IScript>("./script.cs");
                }
                catch (Exception ex)
                {
                    Logger.Log.Write("Error compiling scripts! " + ex.ToString(), ConsoleColor.Red);
                    Thread.Sleep(5000);
                    return;
                }

                Logger.Log.Write("Successfully compiled!", ConsoleColor.DarkGray);
                // Invoke onLoad from the script, let it handle the rest.
                Scripting.Script.onLoad();
            }
            catch (Exception e)
            {
                Logger.Log.Write("Unhandled exception:\r\n" + e.ToString(), ConsoleColor.Red);
            }
        }

        //Our catch-all error handler
        public static void onException(object o, UnhandledExceptionEventArgs e)
        {	//Talk about the exception
            Logger.Log.Write("Unhandled exception:\r\n" + e.ExceptionObject.ToString(), ConsoleColor.Red);
        }
    }
}
