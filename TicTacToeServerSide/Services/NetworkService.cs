﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToeServerSide.Services
{
    public class NetworkService
    {
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int bufferSizeOfData = 1000000;
        private const int port = 27001;

        private static readonly byte[] buffer = new byte[bufferSizeOfData];

        public static bool IsFirst { get; private set; } = false;


        public static char[,] Points = new char[3, 3] { { '1', '2', '3' }, 
                                                        { '4', '5', '6' }, 
                                                        { '7', '8', '9' } };

        public static void Start()
        {
            Console.Title = "Server";
            SetupServer();
            Console.ReadLine();
            CloseAllSockets();
        }

        private static void CloseAllSockets()
        {
            foreach (var socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            serverSocket.Close();
        }

        private static void SetupServer()
        {
            Console.WriteLine();
            Console.WriteLine("Setting Up Server . . . ");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSocket.Listen(2);
            serverSocket.BeginAccept(AcceptCallBack, null);
        }

        private static void AcceptCallBack(IAsyncResult ar)
        {
            Socket socket = null;
            try
            {
                socket = serverSocket.EndAccept(ar);
            }
            catch (Exception)
            {
                return;
            }
            clientSockets.Add(socket);
            string t = string.Empty;
            if(!IsFirst)
            {
                IsFirst = true;
                t = "X";
            }
            else
            {
                IsFirst = false;
                t = "O";
            }
            byte[] data = Encoding.ASCII.GetBytes(t);
            socket.Send(data);

            socket.BeginReceive(buffer, 0, bufferSizeOfData,SocketFlags.None,ReceiveCallBack,socket);

        }

        private static void ReceiveCallBack(IAsyncResult ar)
        {
            Socket current = (Socket)ar.AsyncState;
            int received;
            try
            {
                received = current.EndReceive(ar);
            }
            catch (Exception)
            {
                Console.WriteLine("Client forcefully disconnected");
                current.Close();
                clientSockets.Remove(current);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);


            try
            {
                var no = text[0];
                var symbol = text[1];
                var number = Convert.ToInt32(no) - 49;
                if(number>=0 && number<=2)
                {
                    Points[0, number] = symbol;
                }
                else if(number>=3 && number<=5)
                {
                    Points[1, number-3] = symbol;
                }
                else if(number<=6 && number<=8)
                {
                    Points[2,number-6] = symbol;    
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }




            for (int i = 0; i < 3; i++) // <-- FOR TESTING
            {
                for (int k = 0; k < 3; k++)
                {
                    Console.Write($"{Points[i,k]}");
                }
                Console.WriteLine();
                Console.WriteLine();
            }


            if(text!=String.Empty)
            {
                var myData = ConvertString(Points);
                byte[] data = Encoding.ASCII.GetBytes(myData);
                foreach (var socket in clientSockets)
                {
                    socket.Send(data);
                    Console.WriteLine($"Data Send TO {socket.RemoteEndPoint}");
                }
            }
            else if(text == "exit")
            {
                current.Shutdown(SocketShutdown.Both);
                current.Close();
                clientSockets.Remove(current);
                Console.WriteLine($"{current.RemoteEndPoint} is disconnected");
                return;
            }
            else
            {
                Console.WriteLine("Text is Invalid");
                byte[] data = Encoding.ASCII.GetBytes("Invalid Request");
                current.Send(data);
                Console.WriteLine("Warning Sent");
            }

            current.BeginReceive(buffer, 0, bufferSizeOfData,SocketFlags.None,ReceiveCallBack,current);


        }

        private static string ConvertString(char[,] points)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < points.Length; i++)
            {
                for (int k = 0; k < points.Length; k++)
                {
                    sb.Append(Points[i, k]);
                    sb.Append("\t");
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

    }
}
