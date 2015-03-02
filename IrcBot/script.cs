using System;
using System.Linq;
using ConsoleLogger;
using IrcBot.Objects;

namespace IrcBot.Scripts
{
    public class script : Interfaces.IScript
    {
        public Bot bot;
        public Random random;
        public string spammerinoMessage = "Hey {0}...";
        // ( ° ͜ʖ͡°)╭∩╮

        public void onLoad()
        {
            //generate a new random seed
            Logger.Log.Write("Generating random seed", ConsoleColor.DarkGray);
            random = new Random();

            //create our bot
            bot = new Bot();
            bot.Connect(
                "irc.twitch.tv",
                6667,
                "chapanyabot",
                "oauth:1hzq2bpwqiki1rwt99joqncr4pyhubz"
            );
            Channel chan = bot.JoinChannel("#shiphtur");
            chan.SendMessage("Chap Kappa");
        }

        public void onChatMessage(Channel channel, User sender, string message)
        {
            if(message.IndexOf('!') == 0)
            {
                string[] split = message.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                string command = split[0].Substring(1);
                string payload = "";

                if(split.Length > 1)
                {
                    payload = split[1];
                }

                handleCommand(command, payload, sender, channel);
            }
        }

        public void onNewSubscriber(Channel channel, User subscriber)
        {
            Logger.Log.Write("new sub: " + subscriber.Nickname, ConsoleColor.Blue);
        }

        public void onUserAction(Channel channel, User sender, string action)
        {
            //triggered when people use the /me command.
        }

        private void handleCommand(string command, string payload, User sender, Channel channel)
        {
            switch(command)
            {
                case "chat":
                case "subs":
                case "plebs":
                    spammerino(command, channel);
                    break;
                case "me":
                    spammerino(sender.Nickname, channel);
                    break;
                case "hi":
                    if (payload.ToLower().Contains("nebez"))
                    {
                        spammerino(sender.Nickname, channel, true);
                    }
                    else
                    {
                        spammerino(payload, channel);
                    }
                    break;
                case "repeat":
                    channel.SendMessage(payload);
                    break;
                case "shutdown":
                    if(sender.Nickname.ToLower().Equals("nebezb"))
                    {
                        channel.SendMessage("goodbye Kappa");
                        bot.Stop();
                    }
                    break;
                case "join":
                    if(sender.Nickname.ToLower().Equals("nebezb"))
                    {
                        bot.JoinChannel(payload);
                    }
                    break;
                case "leave":
                    if(sender.Nickname.ToLower().Equals("nebezb"))
                    {
                        if (payload.Length > 0)
                            bot.LeaveChannel(payload);
                        else
                            bot.LeaveChannel(channel.name);
                    }
                    break;
                case "spammerino":
                    if(sender.Nickname.ToLower().Equals("nebezb"))
                    {
                        spammerinoMessage = payload;
                        channel.SendMessage("Kappa");
                    }
                    break;
                case "channels":
                    if(sender.Nickname.ToLower().Equals("nebezb"))
                    {
                        channel.SendMessage(String.Join(", ", bot.channels.Select(c => c.name)));
                    }
                    break;
            }
        }

        private void spammerino(string target, Channel channel, bool trollerino = false)
        {
            string msg = String.Format(spammerinoMessage, target);
            if(trollerino)
            {
                msg += " Keepo";
            }
            channel.SendMessage(msg);
        }
    }
}
