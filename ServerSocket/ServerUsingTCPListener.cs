using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerSocket
{
	public class ServerUsingTCPListener
	{
		TcpListener listener;
		public int port=1234;
		public string host = "127.0.0.1";
		public List<TcpClient> clients = new List<TcpClient>();
		public void Start()
		{
			IPAddress iPAddress = IPAddress.Parse(host);
			listener = new TcpListener(iPAddress, port);
			listener.Start();
			Console.WriteLine($"Server is starting ");
			while (true)
			{
				TcpClient client = listener.AcceptTcpClient();
				Console.WriteLine($" client {client.Client.RemoteEndPoint} has connect to server");
				BroadCast($" client {client.Client.RemoteEndPoint} has connect to server",client);
				clients.Add(client);
				Thread clientThread = new Thread(HandleClient);
				clientThread.Start(client);
			}
		}
		private void HandleClient(object client)
		{
			TcpClient client1 = (TcpClient)client;
			NetworkStream stream = client1.GetStream();
			byte[] buffer = new byte[1024];
			int byteCount = stream.Read(buffer, 0, buffer.Length);
			try
			{
				while (byteCount > 0)
				{
					string data = Encoding.UTF8.GetString(buffer);
					Console.WriteLine($"data read from client {data}");
					BroadCast($"client {client1.Client.RemoteEndPoint} send message {data}", client1);
				}
			}
			catch (Exception ex) {
				Console.WriteLine($"client disconnect");
			}
			finally
			{
				clients.Remove(client1);
				client1.Close();
			}
			Console.WriteLine($"clients count {clients.Count}");
		}
		private void BroadCast(string message, TcpClient excludeClient)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(message);
			foreach (var client in clients)
			{
				if (client != excludeClient)
				{
					NetworkStream stream = client.GetStream();
					stream.Write(buffer, 0, buffer.Length);
				}
			}
		}
	}
}
