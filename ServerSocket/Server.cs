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
		public static IPAddress _address;
		public static IPEndPoint _endPoint;
		public static int MaxUser;
		public static string Hello="Hello from server";
		public static List<Socket> allClient;
		public static void Start(string host,int port,int maxUser)
		{
			MaxUser = maxUser;
			allClient= new List<Socket> ();
			_address = IPAddress.Parse(host);
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
				Socket client = _listener.EndAccept(ar);
				allClient.Add(client);
				Console.WriteLine($" client {client.RemoteEndPoint} connected to server ....");
				_listener.BeginAccept(OnAcceptCLient, null);
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

		private static void OnReceiveFromClient(IAsyncResult ar)
		{

			Socket client = (Socket)ar.AsyncState;
			int byteRead = 0;
			try
			{
				byteRead = client.EndReceive(ar);
				if(byteRead<=0)
				{
					client.Close();
					allClient.Remove(client);
					Console.WriteLine($" client {client.RemoteEndPoint} disconnect from server");
					return;
				}
				SendBroadcast(new ST_DATA_TRANFER()
				{
					DataInt = (uint)allClient.IndexOf(client),
					DataString = $"client  {client.RemoteEndPoint} connected to server",
					DataBool = true,
					DataByteArr = Encoding.UTF8.GetBytes($"client  {client.RemoteEndPoint} connected to server"),
					DataUshort = (ushort)byteRead
				});

			}
			catch (SocketException ex) { 
				client.Close();
				allClient.Remove(client);
				Console.WriteLine($" client wrong disconnect due to {ex.ToString()}" );
			}
		}

		//private static void Send(string message) { 
		//	byte[] buffer = Encoding.UTF8.GetBytes(message);
		//	client.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, OnSendDataToCLient, null);
		//}
		private static void Send(object data,Socket client)
		{
			byte[] output = new byte[1024];
			int size = 0;
			AppMath.ConvertStructeToByteArr(data, ref size, ref output);
			client.BeginSend(output, 0, output.Length, SocketFlags.None, OnSendDataToCLient, new SendData() {
				sk=client,
				data=output
			});
		}
		private static void SendBroadcast(object data)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($" all client ");
			foreach (var client in allClient)
			{
				if (client!=null&& client.Connected)
				{
					stringBuilder.AppendLine($" client {allClient.IndexOf(client)} with endpoint {client.RemoteEndPoint}");
					Send(data, client);
				}
				else
				{
					//allClient.Remove(client);
				}
			}
			Console.WriteLine(stringBuilder.ToString());
		}

		private static void OnSendDataToCLient(IAsyncResult ar)
		{
			SendData sendData = (SendData)ar.AsyncState;
			Socket socket = sendData.sk;
			byte[] data = sendData.data;
			try
			{
				var byteSend = socket.EndSend(ar);
				if (byteSend > 0)
				{
					ST_DATA_TRANFER sT_DATA_TRANFER= default(ST_DATA_TRANFER);
					AppMath.ConvertByteArrToStructure(data, byteSend, ref sT_DATA_TRANFER);
					//Console.WriteLine($"Send data success to client with {sT_DATA_TRANFER.ToString()} ");
				}
			}catch(SocketException ex)
			{
				socket.Close();
				allClient.Remove(socket);
				Console.WriteLine($"cant send data to server due to {ex.Message}");
			}
			
		}

		

		public static void Stop() { 
			_listener.Shutdown(SocketShutdown.Both);
			_listener.Dispose();
			_listener.Close();
			_listener = null;
		}
		
	}
}
public  class SendData
{
	public Socket sk;
	public byte[] data;
}
