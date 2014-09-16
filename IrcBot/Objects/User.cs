using System.Collections.Generic;
using System.Linq;

namespace IrcBot.Objects
{
    public class User
    {
        public enum PermissionLevel
        {
            User,
            SuperUser,
            Turbo,
            Subscriber,
            Mod,
            Streamer,
            Admin,
            Staff
        }

        private string _nickname;
        private Channel _channel;
        private List<PermissionLevel> _permissions;
        private string _userColor;
        private int _messagesSent;

        public string Nickname { get { return _nickname; } }
        public Channel Channel { get { return _channel; } }
        public PermissionLevel HighestPermission { get { return _permissions.Max(); } }
        public List<PermissionLevel> Permissions { get { return _permissions; } }
        public string UserColor { get { return _userColor; } }
        public int MessagesSent { get { return _messagesSent; } set { _messagesSent = value; } }
        public bool IsSubscriber { get { return _permissions.Contains(PermissionLevel.Subscriber); } }

        public User(string nick, Channel chan)
        {
            _nickname = nick;
            _channel = chan;
            _permissions = new List<PermissionLevel>();
            _messagesSent = 0;

            //Everybody is a user! Or are they? iunno
            AddPermission(PermissionLevel.User);

            //Hackish way of determining who the streamer is :(
            if (chan.name.ToLower().Equals("#" + nick.ToLower()))
                AddPermission(PermissionLevel.Streamer);

            //Add us to the channel list of users
            chan.AddUser(this);
        }


        public void AddPermission(PermissionLevel perm)
        {
            if (!_permissions.Contains(perm))
                _permissions.Add(perm);
        }

        public void RemovePermission(PermissionLevel perm)
        {
            if (_permissions.Contains(perm))
                _permissions.Remove(perm);
        }

        public void SetUserColor(string color)
        {
            _userColor = color;
        }

        public void Purge()
        {
            //Purge their chat messages by timing them out for 1 second
            this._channel.SendMessage("/timeout " + _nickname + " 1");
        }

        public void Timeout(int time)
        {
            this._channel.SendMessage("/timeout " + _nickname + " " + time);
        }

        public void Ban()
        {
            this._channel.SendMessage("/ban " + _nickname);
        }

        public void Unban()
        {
            this._channel.SendMessage("/unban " + _nickname);
        }
    }
}
