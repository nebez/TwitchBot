using ConsoleLogger;
using IrcBot.Scripts;
using Sharkbite.Irc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IrcBot.Objects
{
    public class Bot
    {
        public List<Channel> channels;
        public Connection ircConnection;
        public ConnectionArgs ircArgs;

        public Thread messageThread;
        public Queue<KeyValuePair<Channel, string>> messageQueue;
        public List<int> previousMessageTimestamps;

        public int messageLimitDelay = 35000; //35 seconds (stop at 35 just to be safe, actually 30)
        public int messageLimit = 19; //19 messages (stop at 19 just to be safe, actually 20)

        public Bot()
        {
            channels = new List<Channel>();
        }

        //crappy way of exposing the logger we're already using. hope it works? :D
        public static void LogMessage(string message)
        {
            //Logger.Log.Write(
        }

        public void Connect(string host, int port, string botnickname, string serverpassword)
        {
            //Create Identd server
            Identd.Start(botnickname);

            //Create connection args
            ircArgs = new ConnectionArgs();
            ircArgs.Hostname = host;
            ircArgs.Port = port;
            ircArgs.ClientName = "TWITCHCLIENT 3"; //yay, twitchclient 3 is out!
            ircArgs.Nick = botnickname;
            ircArgs.RealName = botnickname;
            ircArgs.UserName = botnickname;
            ircArgs.ServerPassword = serverpassword;

            //Create ircConnection
            ircConnection = new Connection(ircArgs, false, false);

            //Setup our irc event handlers
            ircConnection.Listener.OnRegistered += new RegisteredEventHandler(OnRegistered);
            ircConnection.Listener.OnPublic += new PublicMessageEventHandler(OnPublic);
            ircConnection.Listener.OnPrivate += new PrivateMessageEventHandler(OnPrivate);
            ircConnection.Listener.OnChannelModeChange += new ChannelModeChangeEventHandler(OnChannelModeChange);
            ircConnection.Listener.OnError += new ErrorMessageEventHandler(OnError);
            ircConnection.Listener.OnDisconnected += new DisconnectedEventHandler(OnDisconnected);
            ircConnection.Listener.OnAction += new ActionEventHandler(OnAction);

            //Let's try connecting!
            try
            {
                Logger.Log.Write("Connecting to " + host + ":" + port + "...", ConsoleColor.DarkGray);
                ircConnection.Connect();
            }
            catch (Exception e)
            {
                Logger.Log.Write("Error connecting to network, exception: " + e.ToString(), ConsoleColor.Red);
                Identd.Stop();
                return;
            }

            //We made it!
            Logger.Log.Write("Connected to " + host, ConsoleColor.DarkGray);

            //Create our message queue poller
            messageQueue = new Queue<KeyValuePair<Channel, string>>();
            previousMessageTimestamps = new List<int>();
            messageThread = new Thread(new ThreadStart(MessagePoller));
            messageThread.Start();
        }

        public void MessagePoller()
        {
            int now;
            int delay = 100; //run once every 100 ms to avoid consuming too many resources
            while (true)
            {
                Thread.Sleep(delay);

                now = Environment.TickCount;

                //update our previousmessage list
                foreach (int time in previousMessageTimestamps.ToArray())
                {
                    //are the messages older than our limit (35 seconds)?
                    if (now - time >= messageLimitDelay)
                        previousMessageTimestamps.Remove(time);
                }

                //is there anything in the message queue?
                if (messageQueue.Count == 0)
                    continue;

                //can we send a message?
                if (previousMessageTimestamps.Count < messageLimit)
                {
                    //yes! let's send the next one in queue
                    KeyValuePair<Channel, string> chanmsg = messageQueue.Dequeue();
                    ircConnection.Sender.PublicMessage(chanmsg.Key.name, chanmsg.Value);

                    Logger.Log.Write(chanmsg.Key.name + ":" + chanmsg.Key.botuser.Nickname + "> " + chanmsg.Value, System.ConsoleColor.DarkMagenta);

                    //keep track of the message timestamp
                    previousMessageTimestamps.Add(now);
                }
            }
        }

        public void OnRegistered()
        {
            try
            {
                //Dispose of identd server
                Identd.Stop();
            }
            catch (Exception e)
            {
                Logger.Log.Write("Error in OnRegistered(): " + e.ToString(), ConsoleColor.Red);
            }
        }

        private void OnAction(UserInfo user, string channel, string description)
        {
            //Got an action message. Log it and pass it off to the script
            Channel tempchan = GetChannelByName(channel);
            User sender;

            //Do we have this user object already?
            if (!tempchan.UserExists(user.Nick))
                //Nope, let's make a user object for this channel
                sender = new User(user.Nick, tempchan);
            else
                sender = tempchan.GetUser(user.Nick);

            //Increase their message count
            sender.MessagesSent++;

            //Increase the channels message count
            tempchan.messagesReceived++;

            //Log it!
            Logger.Log.Write(sender.Channel.name + ":" + sender.Nickname + " " + description, ConsoleColor.DarkYellow);

            //Send this off to our script
            Scripting.Script.onUserAction(tempchan, sender, description);
        }

        public void OnPublic(UserInfo user, string channel, string message)
        {
            //Got a public message!
            Channel tempchan = GetChannelByName(channel);
            User sender;

            //Quickly! Is it a twitch notification? We handle those elsewhere!
            if (user.Nick.ToLower().Equals("twitchnotify"))
            {
                //Trigger the proper event and get outta here
                OnTwitchNotification(tempchan, message);
                return;
            }

            //or maybe it's the new TWITCHCLIENT 3 jtv commands
            if (user.Nick.ToLower().Equals("jtv"))
            {
                //yep, handle it elsewhere!
                OnJTVCommand(tempchan, message);
                return;
            }

            //Do we have this user object already?
            if (!tempchan.UserExists(user.Nick))
                //Nope, let's make a user object for this channel
                sender = new User(user.Nick, tempchan);
            else
                sender = tempchan.GetUser(user.Nick);

            //Increase their message count
            sender.MessagesSent++;

            //Increase the channels message count
            tempchan.messagesReceived++;

            //Log it!
            Logger.Log.Write(sender.Channel.name + ":" + sender.Nickname + "> " + message);

            //Send this off to our script
            Scripting.Script.onChatMessage(tempchan, sender, message);

            //HACK: Built in ping/pong test... just in case!
            if (sender.Nickname.ToLower().Equals("nebezb") && message.ToLower().Equals("ping"))
                sender.Channel.SendMessage("pong");
        }

        public void OnTwitchNotification(Channel channel, string notification)
        {
            //This is a twitch chat notification for a specific channel!
            //What kind of notification is it? Let's parse!
            if (notification.ToLower().Contains("just subscribed"))
            {
                //It's a subscription!
                string subscriber = notification.Split(new string[] {" just"}, StringSplitOptions.None).First();

                //is this person a subscriber?
                User target;
                if (!channel.UserExists(subscriber))
                    //make a new user!
                    target = new User(subscriber, channel);
                else
                    target = channel.GetUser(subscriber);

                //Pass it off to the relevant handler in our script
                Scripting.Script.onNewSubscriber(channel, target);
                return;
            }
            else
            {
                Logger.Log.Write("UNHANDLED TWITCH NOTIFICATION: " + channel.name + ":" + notification, ConsoleColor.Yellow);
            }
        }

        public void OnChannelModeChange(UserInfo who, string channel, ChannelModeInfo[] modes)
        {
            Channel tempchan = GetChannelByName(channel);

            //What kind of modes are we applying
            foreach (ChannelModeInfo c in modes)
            {
                User.PermissionLevel permission;
                //What kind of permission?
                if (c.Mode == ChannelMode.ChannelOperator)
                    permission = User.PermissionLevel.Mod;
                else
                {
                    //WHOA!
                    Logger.Log.Write("UNHANDLED PERMISSION MODE CHANGE: " + c.ToString(), ConsoleColor.Yellow);
                    return;
                }

                //Who is the target?

                User target;
                if (!tempchan.UserExists(c.Parameter))
                    target = new User(c.Parameter, tempchan);
                else
                    target = tempchan.GetUser(c.Parameter);

                //Are we adding or removing?
                if (c.Action == ModeAction.Add)
                {
                    Logger.Log.Write(channel + " +o " + target.Nickname, ConsoleColor.DarkCyan);
                    target.AddPermission(permission);
                }
                else
                {
                    Logger.Log.Write(channel + " -o " + target.Nickname, ConsoleColor.DarkRed);
                    target.RemovePermission(permission);
                }
            }
        }

        public void OnJTVCommand(Channel chan, string command)
        {
            User target;

            //What kind of command was this?
            string[] split = command.Split(' ');

            string cmd = split[0].ToLower();

            if (cmd == "specialuser")
            {
                //Does this user even exist?
                if (!chan.UserExists(split[1]))
                    //Nope, let's make a user object
                    target = new User(split[1], chan);
                else
                    target = chan.GetUser(split[1]);

                //What kind of specialuser are they?
                string perm = split[2].ToLower();

                if (perm == "subscriber")
                    target.AddPermission(User.PermissionLevel.Subscriber);
                else if (perm == "turbo")
                    target.AddPermission(User.PermissionLevel.Turbo);
                else if (perm == "admin")
                    target.AddPermission(User.PermissionLevel.Admin);
                else if (perm == "staff")
                    target.AddPermission(User.PermissionLevel.Staff);
                else
                    Logger.Log.Write("UNHANDLED SPECIALUSER: " + command, ConsoleColor.Yellow);
            }

            else if (cmd == "usercolor")
            {
                //Set the chat color of a specific user

                //Does this user even exist?
                if (!chan.UserExists(split[1]))
                    //Nope, let's make a user object
                    target = new User(split[1], chan);
                else
                    target = chan.GetUser(split[1]);

                target.SetUserColor(split[2]);
            }

            else if (cmd == "clearchat")
            {
                //Was a target specified?
                if (split.Count() == 2)
                {
                    //We need to clear a certain users chat!
                    //TODO: DO SOMETHING HERE
                    Logger.Log.Write("USER CHAT CLEARED: " + split[1]);
                }
                else
                {
                    //Entire chat cleared!
                    //TODO: DO SOMETHING HERE
                    Logger.Log.Write("Chat history cleared");
                }
            }

            else if (cmd == "emoteset")
            {
                //Set the emotes for a specific user
                //TODO: figure out what this crap is for. We'll just ignore it for now, seems like a waste.
            }

                //Slowmode and sub mode are weird, they send entire sentences instead of commands.
                //We'll keep them unhandled for now... it'd be really ugly to program this.

                //TODO: Slow mode on/off
                //TODO: Sub mode on/off

            else
            {
                Logger.Log.Write("Unhandled JTV command: " + command, ConsoleColor.Yellow);
            }
        }

        public void OnPrivate(UserInfo user, string message)
        {
            //We got a private message! Print it out if it's not from jtv
            if (user.Nick.ToLower() != "jtv")
            {
                Logger.Log.Write("PRIVMSG:" + user.Nick + "> " + message);
            }
            else
            {
                //Send this to our JTV command handler
                //Since we're using TWITCHCLIENT 3, this shouldn't ever really happen.
                //OnJTVCommand(message);
                Logger.Log.Write("Received unhandled legacy TWITCHCLIENT 2 command from jtv: " + message, ConsoleColor.Yellow);
            }
        }

        public void OnError(ReplyCode code, string message)
        {
            Logger.Log.Write("An error of type: \"" + code + "\", message: \"" + message + "\"", ConsoleColor.Yellow);
        }

        public void OnDisconnected()
        {
            Logger.Log.Write("Connection to the server has been closed.", ConsoleColor.DarkRed);
        }

        public void JoinChannel(string channel)
        {
            if (ircConnection.Connected)
            {
                //Join the channel
                Logger.Log.Write("Joining " + channel + "...", ConsoleColor.DarkGray);
                ircConnection.Sender.Join(channel, true);


                if (GetChannelByName(channel) != null)
                {
                    //Whoa! We already exist?
                    Logger.Log.Write("Channel " + channel + " exists already?");
                    return;
                }

                Channel newchannel = new Channel(channel, this);

                //Now make a user object for ourself. We're still a user, are we not?
                User me = new User(ircArgs.Nick, newchannel);
                newchannel.botuser = me;

                //Add this channel to watch!
                channels.Add(newchannel);
            }
            else
            {
                Logger.Log.Write("Unable to join channel without a connection");
            }
        }

        public void LeaveChannel(string channel)
        {
            if (ircConnection.Connected)
            {
                //Leave the channel
                Logger.Log.Write("Parting " + channel + "...", ConsoleColor.DarkGray);
                ircConnection.Sender.Part(channel);

                //Remove from list of active channels, and clear out the object
                Channel tempchan = GetChannelByName(channel);
                if (tempchan != null)
                {
                    channels.Remove(tempchan);
                    tempchan = null;
                }
                else
                {
                    Logger.Log.Write("Unable to delete channel object " + channel);
                }
            }
            else
            {
                Logger.Log.Write("Unable to part channel without a connection");
            }
        }

        public Channel GetChannelByName(string channel)
        {
            return channels.FirstOrDefault(c => c.name.ToLower().Equals(channel.ToLower()));
        }
    }
}
