﻿using BetaHang;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetaHangServer
{
    class Server
    {
        List<ClientHandler> clients = new List<ClientHandler>();
        internal Action<ClientHandler, BHangMessage> messageHandler;
        private Game myGame;
        //Hack: to force threads to shut down, might be better to have each client handle their threads
        List<Thread> listenerThreads = new List<Thread>();
        bool shutdown = false;
        List<TcpListener> listeners = new List<TcpListener>();


        public void RequestShutdown()
        {
            shutdown = true;
            foreach (var item in listeners)
            {
                item.Stop();
            }
            listeners = null;
        }
        public Server(Game myGame)
        {
            this.myGame = myGame;
        }

        public void Run()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 5000);
            //Console.WriteLine("Server up and running, waiting for messages...");
            listeners.Add(listener);
            try
            {
                listener.Start();

                while (!shutdown)
                {
                    TcpClient c = listener.AcceptTcpClient();
                    ClientHandler newClient = new ClientHandler(c /*, this.messageHandler*/); 
                    //clients.Add(newClient);

                    Thread clientThread = new Thread(newClient.Run);
                    clientThread.Start();
                    //todo: next line might not be needed, part of attempts to get good shutdowns
                    listenerThreads.Add(clientThread);

                    bool added = false;
                    if (myGame != null)
                    {
                        added = myGame.AddPlayer(newClient);
                        clients.Remove(newClient);
                    }
                    if (!added)
                    {
                        //todo: send fail message to client, then drop connection
                    }
                        
                }
            }
            catch (Exception ex)
            {
                // Console.WriteLine(ex.Message);
            }
            finally
            {
                if (listener != null)
                    listener.Stop();
            }
        }

        
        public void Broadcast(string message)
        {
            var Jsonmessage = "";

            foreach (ClientHandler tmpClient in clients)
            {

                NetworkStream n = tmpClient.tcpClient.GetStream();
                BinaryWriter w = new BinaryWriter(n);
                //DataContractJsonSerializer
                //w.Write(client.userName + " says: " + message);
                w.Write(message);
                w.Flush();

            }
        }
    }
}
