using IrcBot.Objects;
using System;

namespace IrcBot.Scripts
{
    public class Scripting
    {
        public static Interfaces.IScript Script;
    }

    public class Interfaces
    {
        public interface IScript
        {
            void onLoad();
            void onChatMessage(Channel channel, User sender, string message);
            void onNewSubscriber(Channel channel, User subscriber);
            void onUserAction(Channel channel, User sender, string action);
        }
    }
}
