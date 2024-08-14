using ClientSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ServerSocket
{
	public class Server
	{
		public  static Socket _listener;
		public static Socket client;
		public static IPAddress _address;
		public static IPEndPoint _endPoint;
		public static int MaxUser;
		public static string Hello="Hello from server";
		public static void Start(string host,int port,int maxUser)
		{
			MaxUser = maxUser;
			_address= IPAddress.Parse(host);
			_endPoint= new IPEndPoint(_address, port);
			_listener= new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);	
			_listener.Bind(_endPoint);
			_listener.Listen(MaxUser);
			Console.WriteLine($"server starting listen");
				 _listener.BeginAccept(OnAcceptCLient,null);
			Console.ReadLine();
			Stop();
		}

		private static void OnAcceptCLient(IAsyncResult ar)
		{
			try
			{
				 client= _listener.EndAccept(ar);
				Console.WriteLine($" client {client.RemoteEndPoint} connected to server ....");
				_listener.BeginAccept(OnAcceptCLient, null);
				ST_DATA_TRANFER sT_DATA_TRANFER= default( ST_DATA_TRANFER );
				sT_DATA_TRANFER.DataString = Hello;
				Send(sT_DATA_TRANFER);
				byte[] buffer = new byte[1024];
				client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceiveFromClient, client);
			
			}
			catch(ObjectDisposedException ex)
			{
				Console.WriteLine(ex.ToString() );
			}
			catch(Exception ex) {
				Console.WriteLine( ex.ToString() );
				Stop();
			}
		}
		//private static void Send(string message) { 
		//	byte[] buffer = Encoding.UTF8.GetBytes(message);
		//	client.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, OnSendDataToCLient, null);
		//}
		private static void Send(object data)
		{
			byte[] output = new byte[1024];
			int size = 0;
			AppMath.ConvertStructeToByteArr(data, ref size, ref output);
			client.BeginSend(output, 0, output.Length, SocketFlags.None, OnSendDataToCLient, null);
		
		}

		private static void OnSendDataToCLient(IAsyncResult ar)
		{
			var byteSend= client.EndSend(ar);
			if(byteSend>0)
			{
				Console.WriteLine($"Send data success to client with hello {Hello}");
			}
		}

		private static void OnReceiveFromClient(IAsyncResult ar)
		{
			try
			{
				Socket socket = (Socket)ar.AsyncState;
				if(socket!=client)
				{
					Console.WriteLine($" socket receive khac socket base");
					return;
				}
				int byteRead=client.EndReceive(ar);
				if (byteRead > 0) {
					Console.WriteLine($"receive data success");
				}
			}catch(Exception ex) { Console.WriteLine(ex.ToString()); }
		}

		public static void Stop() { 
			_listener.Shutdown(SocketShutdown.Both);
			_listener.Dispose();
			_listener.Close();
			_listener = null;
		}
		
	}
}
