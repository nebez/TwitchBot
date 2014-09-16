using ConsoleLogger;
using System.Collections.Generic;
using System.Linq;

namespace IrcBot.Objects
{
    public class Channel
    {
        public Bot bothandler;
        public User botuser;
        public string name;
        public List<Objects.User> users;
        public long messagesReceived;

        public Channel(string channelname, Bot handler)
        {
            name = channelname;
            bothandler = handler;
            users = new List<User>();
            messagesReceived = 0;
        }

        public void AddUser(User user)
        {
            if (!users.Contains(user))
                users.Add(user);
        }

        public void RemoveUser(User user)
        {
            if (users.Contains(user))
                users.Remove(user);
        }

        public bool UserExists(string user)
        {
            if (users.FirstOrDefault(u => u.Nickname.ToLower().Equals(user.ToLower())) != null)
                return true;
            return false;
        }

        public User GetUser(string user)
        {
            return users.FirstOrDefault(u => u.Nickname.ToLower().Equals(user.ToLower()));
        }

        public void SendMessage(string message)
        {
            //Enqueue the message
            bothandler.messageQueue.Enqueue(new KeyValuePair<Channel, string>(this, message));
        }

        public void SlowMode(int delay)
        {
            SendMessage("/slow " + delay);
        }

        public void SlowModeOff()
        {
            SendMessage("/slowoff");
        }

        public void SubOnly(bool enabled)
        {
            if (enabled)
                SendMessage("/subscribers");
            else
                SendMessage("/subscribersoff");
        }

        public void ClearChat()
        {
            SendMessage("/clear");
        }
    }
}
