﻿using ISE182_project.Layers.CommunicationLayer;
using ISE182_project.Layers.DataAccsesLayer;
using ISE182_project.Layers.LoggingLayer;
//using ISE182_project.Layers.PersistentLayer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ISE182_project.Layers.BusinessLogic
{
    //This class manege the messages stored in RAM
     class MessageService
    {
        #region Members

        private const int MAX_MESSAGES = 200; // maximum items per quary

        private MessageExcuteor _excuteor; // the excuteor to the DB
        private DateTime _lastMessageTime; // the time of the last message

        private ICollection<Message> _ramData; // Store a coppy of the data in the ram for quick acces       
        private ICollection<IMessage> _lastFilteredList; //Save te filtered items     

        private ChatRoom.Sort _sortBy; //Save the last sort option
        private bool _descending; //Save the last sort direction

        private string _lastFilter; //Save the last filter option
        private string _lastFilterNick; //Save the last filter nickname
        private int _lastFilterGroup; //Save the last filter gtoup

        #endregion

        #region singletone

        //private ctor
        private MessageService()
        {
            _excuteor = new MessageExcuteor(MAX_MESSAGES);
            _ramData = new List<Message>();
            _lastFilteredList = new List<IMessage>();
            _lastFilter = "None"; 
            _lastFilterNick = ""; 
            _lastFilterGroup = -1; 

    }

    private static MessageService _instence; // the instence

        // instemce getter
        public static MessageService Instence
        {
            get
            {
                if (_instence == null)
                    _instence = new MessageService();

                return _instence;
            }
        }

        #endregion

        #region functionalities

        //Get the filtered messages
        public ICollection<IMessage> getMessages()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            LastFilter(); //Filter again by the last filter
            LastSort(); //Sort before sending
            return _lastFilteredList;
        }

        //Getter and setter to the data stored in the ram
        private ICollection<Message> RamData
        {
            get { return _ramData; }
            set
            {
                if(value != null)
                    _ramData = value;
            }
        }

        //add a message to the ram
        private void add(IMessage item)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            if (!RamData.Contains(item))
                RamData.Add(new Message(item));

            if (!_lastFilteredList.Contains(item))
                _lastFilteredList.Add(new DisplayMessage(item));

            LastFilter(); //Filter again by the last filter
            LastSort(); //Sort before sending

            if (_lastFilteredList.Count > MAX_MESSAGES)
            {
                if(_descending)
                    _lastFilteredList.Remove(_lastFilteredList.Last());
                else
                    _lastFilteredList.Remove(_lastFilteredList.First());
            }
        }

        //Send a message and save into the ram
        public void send(IMessage item, int UserID)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            add(item);
            AddToDS(item, UserID);
        }

        #region Filter

        //recive all the messages from a certain user
        public void FilterByUser(IUser user)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            string Filter = "ByUser";

            _excuteor.FilterByUser(user);

            saveFilterdMessages(Filter);

             _lastFilter = Filter;
            _lastFilterGroup = user.Group_ID;
            _lastFilterNick = user.NickName;
        }

        //recive all the messages from a certain group
        public void FilterByGroup(int groupID)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            string Filter = "ByGroup";

            _excuteor.FilterByGroup(groupID);

            saveFilterdMessages(Filter);

            _lastFilter = Filter;
            _lastFilterGroup = groupID;
            _lastFilterNick = "";
        }

        //reset filters
        public void resetFilters()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            string Filter = "None";

            resetSavedFilterdmessages();
            LastSort();
          
            _lastFilter = Filter;
            _lastFilterGroup = -1;
            _lastFilterNick = "";
        }

        #endregion

        #region Sort

        //Sort a message List by the time
        public void sort(ChatRoom.Sort SortBy, bool descending)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            ICollection<IMessage> temp = null;

            //Save last sort option
            _sortBy = SortBy;
            _descending = descending;

            if (descending)
            {
                switch (SortBy)
                {
                    case ChatRoom.Sort.Time: temp = _lastFilteredList.OrderByDescending(msg => msg.Date).ToList(); break;
                    case ChatRoom.Sort.Nickname: temp = _lastFilteredList.OrderByDescending(msg => msg.UserName).ToList(); break;
                    case ChatRoom.Sort.GroupNickTime: temp = _lastFilteredList.OrderByDescending(msg => msg.GroupID).ThenByDescending(msg => msg.UserName).ThenByDescending(msg => msg.Date).ToList(); break;
                }
            }
            else
            {
                switch (SortBy)
                {
                    case ChatRoom.Sort.Time: temp = _lastFilteredList.OrderBy(msg => msg.Date).ToList(); break;
                    case ChatRoom.Sort.Nickname: temp = _lastFilteredList.OrderBy(msg => msg.UserName).ToList(); break;
                    case ChatRoom.Sort.GroupNickTime: temp = _lastFilteredList.OrderBy(msg => int.Parse(msg.GroupID)).ThenBy(msg => msg.UserName).ThenBy(msg => msg.Date).ToList(); break;
                }
            }

            _lastFilteredList = temp;
        }

        #endregion

        //retrive and save the new messages that were send after the last draw.
        public void DrawNewMessage()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));         

            _excuteor.clearFilters();
            _excuteor.AddTimeFilter(_lastMessageTime);

            try
            {
                Execute();

                LastFilter(); //Filter again by the last filter
                LastSort(); //Sort before sending

                if (_lastFilteredList.Count > 0)
                {
                    if(_descending)
                        _lastMessageTime = _lastFilteredList.First().Date;
                    else
                        _lastMessageTime = _lastFilteredList.Last().Date;
                }
            }
            catch
            {
                string error = "An Error Acured while drawing messages!";
                Logger.Log.Fatal(Logger.Maintenance(error));

                throw;
            } 
        }

        //Edid message by guid and save to the RAM and disk, and chrck if the user can edit the message
        public void EditMessage(Guid ID, string newBody, IUser editor)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            if(editor == null)
            {
                string errorMessage = "recived null user!";
                Logger.Log.Error(Logger.Maintenance(errorMessage));

                throw new ArgumentNullException(errorMessage);
            }


            //A dummy message to use in the .equals
            IMessage dummy = new Message(ID, DateTime.Now, "Dummy", 0, "Dummy");

            foreach (Message msg in RamData)
            {
                if (msg.Equals(dummy))
                {
                    if(!msg.GroupID.Equals(editor.Group_ID.ToString()) | !msg.UserName.Equals(editor.NickName)) //written by other user
                    {
                        string errorMessage = "you can edit only your own messges!!";
                        Logger.Log.Error(Logger.Maintenance(errorMessage));

                        throw new InvalidOperationException(errorMessage);
                    }

                    try
                    {
                        msg.editBody(newBody);

                        //Update filtered list
                        _lastFilteredList.Remove(dummy);
                        _lastFilteredList.Add(new DisplayMessage(msg));
                    }
                    catch
                    {
                        string errormessage = "could not edit your message, please check the length of your message!";
                        Logger.Log.Error(Logger.Maintenance(errormessage));

                        throw new ArgumentOutOfRangeException(errormessage);
                    }

                    _excuteor.UPDATE(msg);
                    return;
                }
            }

            //message is not exists

            string error = "Could not found a message with the ruqusted guid of " + ID;
            Logger.Log.Error(Logger.Maintenance(error));

            throw new KeyNotFoundException(error);
        }

        #endregion

        //-----------------------------------------------------------------

        #region override methods

        // deserialize messages
        protected void reciveData()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            ICollection<IMessage> temp = _excuteor.getALLLastMessages();

            foreach (IMessage msg in temp)
            {
                if (msg.Date <= DateTime.Now.ToUniversalTime())
                    add(msg);
            }
        }

        // serialize messages
        protected bool AddToDS(IMessage Data, int UserID)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

           return _excuteor.INSERT(Data, UserID) > 0;
        }

        //init data
        public void start()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            reciveData();

            //Defult sort options
            _sortBy = ChatRoom.Sort.Time;
            _descending = false;

            resetSavedFilterdmessages();

            if (_lastFilteredList.Count > 0)
                _lastMessageTime = _lastFilteredList.Last().Date;
            else
                _lastMessageTime = new DateTime(1, 1, 1);
        }

        #endregion

        #region Private Methods

        //Execute queries
        private void Execute()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            ICollection<IMessage> temp = _excuteor.getFilteredMessages();

            foreach (IMessage msg in temp)
            {
                if(msg.Date <= DateTime.Now.ToUniversalTime())
                     add(msg);
            }
        }

        //save the filterd messages
        private void saveFilterdMessages(string currentAskedFilter)
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            ICollection<IMessage> filtered = _excuteor.getFilteredMessages();
            _lastFilteredList = filtered;

        }

        //sort the messages by the last requested sort
        private void LastSort()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            sort(_sortBy, _descending); // resort
        }

        //filter the messages by the last requested filter
        private void LastFilter()
        {
            Logger.Log.Debug(Logger.MethodStart(MethodBase.GetCurrentMethod()));

            switch (_lastFilter)
            {
                case "ByUser": FilterByUser(new User(_lastFilterNick, _lastFilterGroup)); break;
                case "ByGroup": FilterByGroup(_lastFilterGroup); break;
                default: break;
            }
        }

        // _lastFilteredList = RamData;
        private void resetSavedFilterdmessages()
        {
            _lastFilteredList.Clear();

            foreach (Message msg in RamData)
                _lastFilteredList.Add(new DisplayMessage(msg));

            LastSort(); //Sortg by Time at start
            _lastFilteredList = _lastFilteredList.Reverse().Take(MAX_MESSAGES).Reverse().ToList();

        }

        #endregion

    }
}
