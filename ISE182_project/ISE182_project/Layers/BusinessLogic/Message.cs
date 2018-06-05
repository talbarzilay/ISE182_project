﻿using ISE182_project.Layers.CommunicationLayer;
using ISE182_project.Layers.LoggingLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ISE182_project.Layers.BusinessLogic
{
    [Serializable]
    // This class Implaments the Imessage interface
    // and represen a message in the chatroom
    class Message : IMessage
    {
        private Guid _guid;              // The unique idetifier of the message
        private DateTime _receivingTime; // The time the server received the message
        private IUser _sender;           // The sender user
        private string _body;            // The message’s content
        private const int MAX_LENGTH = 150;

        #region Getters & Setters

        //Getter to the reciving time
        public DateTime Date
        {
            get { return _receivingTime; }
        }

        //Getter to the group ID
        public string GroupID
        {
            get { return _sender.Group_ID.ToString(); }
        }

        //Getter to the guid
        public Guid Id
        {
            get { return _guid; }
        }

        //Getter to the content of the message
        public string MessageContent
        {
            get { return _body; }

            private set
            {
                if (!isValid(value))
                {
                    string error = "The user tried to edit a message with invalid content";
                    Logger.Log.Error(Logger.Maintenance(error));

                    throw new ArgumentException(error);            
                }

                _body = value; // edit content
            }
        }

        //Getter to the sender nickname
        public string UserName
        {
            get { return _sender.NickName; }
        }

        //Getter to the sender nuckname
        private IUser Sender
        {
            get { return _sender; }
        }

        #endregion

        #region Ctors

        //A constractor of message class
        public Message(Guid guid, DateTime receivingTime, IUser sender, string body)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            if (guid == null | receivingTime == null | sender == null | body == null)
            {
                Logger.Log.Error(Logger.Maintenance("recived a null as an argument"));

                throw new ArgumentNullException();
            }

            _guid = guid;
            _receivingTime = receivingTime;
            _sender = sender;
            _body = body;
        }

        //A copy constractor
        public Message(IMessage msg) : this(msg.Id, msg.Date, new User(msg.UserName, int.Parse(msg.GroupID)), msg.MessageContent)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));
        }

        #endregion

        #region functionalities

        //Cheak if the body is valid
        public static bool isValid(string body)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            return body != null &&  // Not null
                body.Length < MAX_LENGTH & // Less then 150 characters
                !body.Equals("");   // Atleast 1 charcter
        }

        #endregion

        #region ToString & Equals

        // Cheack if two messages are equals.
        // Two mesages are equals if they both have the same guid
        public override bool Equals(object obj)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            if (!(obj is IMessage))
                return false;

            IMessage other = (IMessage)obj;

            return Id.Equals(other.Id);
        }

        //return a string tat represent the message
        public override string ToString()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            return "Message : \nGuid: " + Id + "\nRecivingTime : " + Date.ToLocalTime() + 
                "\nSender : " + Sender + "\nMessage Body : " + MessageContent;
        }

        #endregion

        //-----------------------------------------------

        #region Unused Code

        //Edit the message's body.
        public void editBody(string newBody)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            MessageContent = newBody; //edit the body
        }

        #endregion
    }
}
