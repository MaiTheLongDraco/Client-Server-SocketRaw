﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ClientSocket
{
	internal class ClientSocket
	{
		public static string Host = "127.0.0.1";
		public static int Port = 1234;
		public static Socket request;
		public static string hello = "Hello from client";
		public static byte[] dataReceiveBuffer = new byte[1024];
		public static void Connect()
		{
			request = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPAddress iPAddress = IPAddress.Parse(Host);
			IPEndPoint endPoint = new IPEndPoint(iPAddress, Port);
			request.BeginConnect(endPoint, OnConnectCallback, null);
			Console.ReadLine();
		}

		private static void OnConnectCallback(IAsyncResult ar)
		{
			try
			{
				request.EndConnect(ar);
				Console.WriteLine("connect to server success");
				byte[] buffer = Encoding.UTF8.GetBytes(hello);
				request.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, OnSendDataToServer, null);
			}
			catch (Exception ex)
			{
				Console.WriteLine("connect to server fail " + ex.ToString());
			}


		}

		private static void OnSendDataToServer(IAsyncResult ar)
		{
			int byteSend = request.EndSend(ar);
			if (byteSend > 0)
			{
				Console.WriteLine($" send data success");
			}
			request.BeginReceive(dataReceiveBuffer, 0, dataReceiveBuffer.Length, SocketFlags.None, OnReceiveCallBack, null);
		}

		private static void OnReceiveCallBack(IAsyncResult ar)
		{
			int byteRead = request.EndReceive(ar);
			if (byteRead > 0)
			{
				ST_DATA_TRANFER sT_DATA_TRANFER = default (ST_DATA_TRANFER);
				AppMath.ConvertByteArrToStructure(dataReceiveBuffer, byteRead, ref sT_DATA_TRANFER);
				//Console.WriteLine("receive data success");
				StringBuilder sb= new StringBuilder();
				sb.AppendLine(sT_DATA_TRANFER.DataInt.ToString());
				sb.AppendLine(sT_DATA_TRANFER.DataUshort.ToString());
				sb.AppendLine(sT_DATA_TRANFER.DataBool.ToString());
				sb.AppendLine(sT_DATA_TRANFER.DataString.ToString());
				//sb.AppendLine(sT_DATA_TRANFER.DataByteArr.Count().ToString());
				Console.WriteLine($"message from server {sb.ToString()}");
				request.BeginReceive(dataReceiveBuffer, 0, byteRead, SocketFlags.None, OnReceiveCallBack, null);
			}
		}
	}
}
