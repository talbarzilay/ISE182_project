﻿using ISE182_project.Layers.CommunicationLayer;
using ISE182_project.Layers.LoggingLayer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ISE182_project.Layers.BusinessLogic
{

    //This class hanel the chatroom
    public static class ChatRoom
    {
        private const string HOME_URL = @"http://localhost/";             // The url addres of the server at home
        private const string BGU_URL = @"http://ise172.ise.bgu.ac.il:80"; // The url addres of the server
        private static Place _location;                                   // The location of the client
        private static IUser _loggedinUser;                               // Current logged in user
        private static int _loggeninUserID;                               // The id of the loged user

        #region General

        //The location of the client
        public enum Place
        {
            Home,
            University
        }

        //Getter to the URL
        private static string URL
        {
            get
            {
                switch (_location)
                {
                    case Place.Home: return HOME_URL;
                    case Place.University: return BGU_URL;
                    default:
                        {                      
                            string error = "recived unknown enum value.";
                            Logger.Log.Fatal(Logger.Developer(error));

                            throw new ArgumentOutOfRangeException(error);
                        }
                }
 
            }
        }

        //Geter and setter to the current user
        private static IUser LoggedinUser
        {
            get { return _loggedinUser; }
            set { _loggedinUser = value; }
        }

        //Geter and setter to the current user
        public static DisplayUser LoggedUser
        {
            get { return new DisplayUser(_loggedinUser.NickName, _loggedinUser.Group_ID); }
        }

        //Initiating the ram's saves from disk
        public static void start(Place location)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            MessageService.Instence.start(); // Initiating mesaages on ram
            _location = location;   // Set the location
            _loggeninUserID = -1;
        }

        //exit the program
        public static void exit()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            if (isLoggedIn())
                logout(); //logout first

            Logger.Log.Info(Logger.Maintenance("The client closed the program"));
            Environment.Exit(1); //Exiting
        }

        #endregion

        #region User

        // return if tere is an loggedin user
        public static bool isLoggedIn()
        {
            return LoggedinUser != null;
        }

        // register a user to the server
        public static void register(string nickname, int GroupID/*, string password */)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            IUser user = new User(nickname, GroupID/*, password */);

            if (isLoggedIn()) //Already logged In
            {
                string error = "A user tried to register while loggedin to: " + LoggedinUser;
                Logger.Log.Error(Logger.Maintenance(error));

                throw new InvalidOperationException(error);
            }

            if (!UserService.Instence.canRegister(user)) //Was regusterd
            {
                string error = "The user tried to register to a not register account";
                Logger.Log.Error(Logger.Maintenance(error));

                throw new InvalidOperationException(error);
            }

            UserService.Instence.register(user); //register
        }

        // logIn an existing user to the server
        public static void login(string nickname, int GroupID/*, string password */)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            IUser user = new User(nickname, GroupID/*, password */);
            int id;

            if (isLoggedIn()) //Already logged In
            {
                string error = "A user tried to login while loggedin to: " + LoggedinUser;
                Logger.Log.Error(Logger.Maintenance(error));

                throw new InvalidOperationException(error);
            }

            id = UserService.Instence.canLogIn(user);

            if (id < 0) //Was regusterd
            {
                string error = "An error accured while trying to login. try checkin your info!";
                Logger.Log.Error(Logger.Maintenance(error));

                throw new InvalidOperationException(error);
            }

            LoggedinUser = user; //log in
            _loggeninUserID = id;
            Logger.Log.Info(Logger.Maintenance("The user " + user + " loggedin"));
        }

        // logout the user from the server
        public static void logout()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            if (!isLoggedIn()) //There is no logged in userer
            {
                string error = "A user tried to logout without being logedin to a user";
                Logger.Log.Error(Logger.Maintenance(error));

                throw new InvalidOperationException(error);
            }

            LoggedinUser.logout();
            _loggeninUserID = -1;
            LoggedinUser = null; // Change the loggedin user to null
        }

        #endregion

        #region Message

        //Get the filtered messages
        public static ICollection<string> getMessages()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            ICollection<IMessage> temp = MessageService.Instence.getMessages();

            ICollection<string> output = new List<string>();

            foreach (IMessage msg in temp)
                output.Add(msg.ToString());

            return output;
        }

        // Send new message to te server
        public static void send(string body)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            if(!isLoggedIn())
            {
                string error = "You cant send a message without login first!";
                Logger.Log.Error(Logger.Maintenance(error));

                throw new InvalidOperationException(error);
            }

            LoggedinUser.send(body, _loggeninUserID); // Sending
        }


        // Receive the last 20 messages
        public static ICollection<IMessage> request20Messages()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            return requestNMessages(20);
        }

        //draw the last messages since kast draw
        public static void DrawLastMessages()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            MessageService.Instence.DrawNewMessage();
        }

        #region Sort

        //Sort a message List by the time
        public static void sort(Sort SortBy, bool descending)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            MessageService.Instence.sort(SortBy, descending);

        }

        //Sort options
        public enum Sort
        {
            Time,
            Nickname,
            GroupNickTime
        }

        #endregion

        #region Filter

        // Receive all the messages from a certain user
        public static void filterByUser(string nickName, int GroupID)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            MessageService.Instence.FilterByUser(new User(nickName, GroupID));
        }

        // Receive all the messages from a certain group
        public static void filterByGroup(int GroupID)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            MessageService.Instence.FilterByGroup(GroupID);
        }

        // Reset Filters
        public static void resetFilters()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            MessageService.Instence.resetFilters();
        }

        #endregion

        #endregion

        // ----------------------------------------------------------

        #region private Methods

        // Receive the last n messages
        private static ICollection<IMessage> requestNMessages(int number)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            return MessageService.Instence.lastNmesages(number);
        }
        #endregion
    }
}
