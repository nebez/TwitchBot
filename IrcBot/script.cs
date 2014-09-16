using ConsoleLogger;
using IrcBot.Objects;
using System;

namespace IrcBot.Scripts
{
    public class script
    {
        public Bot bot;
        public Random random;

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
            bot.JoinChannel("#scarra");
            bot.JoinChannel("#clgdoublelift");
        }

        public void onChatMessage(Channel channel, User sender, string message)
        {
            
        }

        public void onNewSubscriber(Channel channel, User subscriber)
        {
            Logger.Log.Write("new sub: " + subscriber.Nickname, ConsoleColor.Blue);
        }

        public void onUserAction(Channel channel, User sender, string action)
        {
            //triggered when people use the /me command.
        }
    }
}
