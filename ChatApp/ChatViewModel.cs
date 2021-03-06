﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.ObjectModel;
using System.IO;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;

namespace ChatApp
{
    /// <summary>
    /// The viewmodel connection the GUI to the Chat and clients
    /// </summary>
    public class ChatViewModel : BaseViewModel
    {
        // =====================================================================
        // Properties
        // =====================================================================
        
        /// <summary>
        /// Chat instance handling the chats and clients
        /// </summary>
        /// <value></value>
        public ChatModel Chat { get; set; }

        /// <summary>
        /// Label bound to the invite button
        /// </summary>
        private string _inviteButtonLabel;
        public string InviteButtonLabel
        {
            get { return _inviteButtonLabel; }
            set
            {
                _inviteButtonLabel = value;
                RaisePropertyChanged("InviteButtonLabel");
            }
        }

        /// <summary>
        /// Label bound to the listen button
        /// </summary>
        private string _listenButtonLabel;
        public string ListenButtonLabel
        {
            get { return _listenButtonLabel; }
            set
            {
                _listenButtonLabel = value;
                RaisePropertyChanged("ListenButtonLabel");
            }
        }
        
        /// <summary>
        /// Textfield entry variable bound to the textfield where text to be sent are written
        /// </summary>
        private string _sendText;
        public string SendText
        {
            get { return _sendText; }
            set
            {
                _sendText = value;
                RaisePropertyChanged("SendText");

            }
        }
        
        // =====================================================================
        // ICommands for the buttons
        // =====================================================================
        public ICommand InviteButtonCommand { get; set; }
        public ICommand ListenButtonCommand { get; set; }
        public ICommand SendButtonCommand { get; set; }
        public ICommand SendImageButtonCommand { get; set; }
        public ICommand AcceptButtonCommand { get; set; }
        public ICommand DeclineButtonCommand { get; set; }
        public ICommand DisconnectClientCommand { get; set; }
        public ICommand LoginCommand { get; set; }
        public ICommand StoreConversationsCommand { get; set; }
        // =====================================================================
        // Constructor
        // =====================================================================
        public ChatViewModel()
        {
            ListenButtonLabel = "Search for connection";
            InviteButtonLabel = "Invite";

            ListenButtonCommand = new RelayCommand(new Action<object>(ListenButtonClick));
            InviteButtonCommand = new RelayCommand(new Action<object>(InviteButtonClick));
            SendButtonCommand = new RelayCommand(new Action<object>(SendButtonClick));
            SendImageButtonCommand = new RelayCommand(new Action<object>(SendImageButtonClick));
            AcceptButtonCommand = new RelayCommand(new Action<object>(AcceptButtonClick));
            DeclineButtonCommand = new RelayCommand(new Action<object>(DeclineButtonClick));
            DisconnectClientCommand = new RelayCommand(new Action<object>(DisconnectClientClick));
            LoginCommand = new RelayCommand(new Action<object>(Login));
            StoreConversationsCommand = new RelayCommand(new Action<object>(StoreConversations));

            Chat = new ChatModel();
            Chat.AddressBusy += AddressAlreadyInUse; //subscribe to event
        }

        // =====================================================================
        // Member functions
        // =====================================================================

        /// <summary>
        /// This functions is called after the user has selected a username
        /// </summary>
        /// <param name="sender"></param>
        private void Login(object sender)
        {
            Chat.IsNotLoggedIn = false;
            Chat.LoadConversations();
        }
        
        /// <summary>
        /// Functin that runs when the listen button is clicked
        /// </summary>
        /// <param name="sender"></param>
        private void ListenButtonClick(object sender)
        {
            if (!Chat.IsNotListening)
            {
                ListenButtonLabel = "Search for connection";
                Chat.StopListening();
            }
            else
            {
                ListenButtonLabel = "Stop searching";
                Chat.StartListening();
            }
        }

        /// <summary>
        /// Function that runs when the listen button is clicked
        /// </summary>
        /// <param name="sender"></param>
        private void InviteButtonClick(object sender)
        {
            Chat.Invite();
        }

        /// <summary>
        /// Function that runs when the send button is pressed
        /// </summary>
        /// <param name="sender"></param>
        private void SendButtonClick(object sender)
        {
            if (!Chat.Clients.Any()) //if there are no clients in the client list
            {
                Chat.Status = "There are no clients connected";
                Console.WriteLine("There are no clients connected");
            }
            else
            {
                //Send to selected client
                Chat.SendToSelectedClient(SendText);
                SendText = "";
            }
        }

        /// <summary>
        /// Function that runs when the send image button is pressed
        /// </summary>
        /// <param name="sender"></param>
        private void SendImageButtonClick(object sender)
        {
            //TODO: implement
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Filter = "JPG Files (*.jpg)|*.jpg|JPEG Files (*.jpeg)|*.jpeg";

            Nullable<bool> result = dlg.ShowDialog();

            string filename = dlg.FileName;

            if (result == true)
            {
                try
                {
                    Image image = new Image();

                    image.Source = new BitmapImage(new Uri(@filename));

                    byte[] byteImg;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(new Uri(@filename)));
                        encoder.Save(ms);
                        byteImg = ms.ToArray();
                    }

                    string imageStr = Convert.ToBase64String(byteImg);

                    Chat.SendImageToSelectedClient(imageStr);
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("The file " + filename + " could not be loaded: File not found");
                }
            }
        }

        /// <summary>
        /// Function that runs when the accept invite button is clicked
        /// </summary>
        /// <param name="sender"></param>
        private void AcceptButtonClick(object sender)
        {
            Chat.Accept = true;
            Chat.ShowPopup = false;
        }

        /// <summary>
        /// Function that runs when the decline invite button is clicked 
        /// </summary>
        /// <param name="sender"></param>
        private void DeclineButtonClick(object sender)
        {
            Chat.Accept = false;
            Chat.ShowPopup = false;
        }

        /// <summary>
        /// Function that runs when the disconnect client button is cliecked
        /// </summary>
        /// <param name="sender"></param>
        private void DisconnectClientClick(object sender)
        {
            try
            {
                Message msg = new Message(Chat.UserName);
                Chat.Clients.Single(i => i.Name == Chat.SelectedClient.Name).SendString(msg.GetDisconnectMessage());//Look for exception here
                Chat.DisconnectSelectedClient();
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("The selected client is not in the clients list?");
            }
        }

        /// <summary>
        /// Filters the connections list when the search event is activated
        /// </summary>
        /// <param name="sender"></param>
        private void SearchClick(object sender)
        {
            Chat.FilterConnections();
        }

        /// <summary>
        /// Store all conversations of the clients into files
        /// </summary>
        /// <param name="sender"></param>
        private void StoreConversations(object sender)
        {
            Chat.StoreConversations();
        }

        /// <summary>
        /// Function runs when the Chat.AddressBusy event is fired
        /// </summary>
        private void AddressAlreadyInUse()
        {
            MessageBox.Show("Address is busy.\nTry to send and invite instead.");
            ListenButtonLabel = "Search for connection";
            Chat.StopListening();
        }

    }
}
