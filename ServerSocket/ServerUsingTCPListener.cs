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
		public Dictionary<string,TcpClient> clients = new Dictionary<string,TcpClient>();
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
        private static void HandleClient(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int byteCount;
            string clientId = null;

            try
            {
                // Bước 1: Gán ID cho client
                clientId = GenerateClientId();
                Console.WriteLine($"Assigning ID {clientId} to new client.");

                // Thêm client vào Dictionary
                lock (lockObj)
                {
                    clients.Add(clientId, client);
                }

                // Bước 2: Gửi ID cho client
                SendMessage(stream, $"ASSIGNED_ID:{clientId}");
                Console.WriteLine($"Sent assigned ID {clientId} to client.");

                // Bước 3: Thông báo cho tất cả client khác rằng có client mới kết nối
                Broadcast($"{clientId} đã kết nối.", clientId);

                // Bước 4: Lắng nghe tin nhắn từ client
                while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, byteCount).Trim();
                    Console.WriteLine($"Received from {clientId}: {message}");

                    // Kiểm tra xem có lệnh gửi đến một client cụ thể hay không
                    if (message.StartsWith("SEND_TO:"))
                    {
                        // Định dạng: SEND_TO:ClientX:Tin nhắn
                        string[] splitMessage = message.Split(new char[] { ':' }, 3);
                        if (splitMessage.Length == 3)
                        {
                            string targetId = splitMessage[1];
                            string actualMessage = splitMessage[2];
                            SendMessageToSpecificClient(targetId, $"{clientId}: {actualMessage}");
                        }
                    }
                    else
                    {
                        // Phát tin nhắn đến tất cả các client khác
                        Broadcast($"{clientId}: {message}", clientId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with client {clientId ?? "Unknown"}: {ex.Message}");
            }
            finally
            {
                if (clientId != null)
                {
                    lock (lockObj)
                    {
                        if (clients.ContainsKey(clientId))
                        {
                            clients.Remove(clientId);
                            // Thông báo cho tất cả client khác rằng client đã ngắt kết nối
                            Broadcast($"{clientId} đã ngắt kết nối.", clientId);
                        }
                    }
                }
                client.Close();
                Console.WriteLine($"Client {clientId ?? "Unknown"} disconnected.");
            }
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
        private  string GenerateClientId()
        {
            lock (lockObj)
            {
                clientCounter++;
                return $"Client{clientCounter}";
            }
        }
        private  void SendMessageToSpecificClient(string clientId, string message)
        {
            lock (lockObj)
            {
                if (clients.ContainsKey(clientId))
                {
                    TcpClient client = clients[clientId];
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = Encoding.UTF8.GetBytes(message + "\n");

                    try
                    {
                        if (stream.CanWrite)
                        {
                            stream.Write(buffer, 0, buffer.Length);
                            Console.WriteLine($"Sent to {clientId}: {message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending message to {clientId}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Client {clientId} not found.");
                }
            }
        }
    }
}
