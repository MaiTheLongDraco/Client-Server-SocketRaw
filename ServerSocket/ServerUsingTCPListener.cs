using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ServerSocket
{
    public class ServerUsingTCPListener
    {
        TcpListener listener;
        public int port = 1234;
        public string host = "127.0.0.1";
        public Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
        private object _lockObj = new object();
        private int _clientCounter = 0; // Biến đếm để tạo ID duy nhất

        public void Start()
        {
            IPAddress iPAddress = IPAddress.Parse(host);
            listener = new TcpListener(iPAddress, port);
            listener.Start();
            Console.WriteLine($"Server is starting ");
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        }

        private  void SendMessage<T>(NetworkStream stream, ProtocolMessage<T> message)
        {
            string json = JsonConvert.SerializeObject(message);
            byte[] buffer = Encoding.UTF8.GetBytes(json + "\n");
            stream.Write(buffer, 0, buffer.Length);
        }

        private  void HandleClient(object clientObj)
    {
        TcpClient client = (TcpClient)clientObj;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[4096];
        int byteCount;
        string clientId = null;

        try
        {
            // Bước 1: Gán ID cho client
            clientId = GenerateClientId();
            Console.WriteLine($"Assigning ID {clientId} to new client.");

            // Thêm client vào Dictionary
            lock (_lockObj)
            {
                clients.Add(clientId, client);
            }

            // Bước 2: Gửi ID cho client
            var assignIdMessage = new ProtocolMessage<string>
            {
                ProtocolType = (int)ServerToClientOperationCode.UpdatePlayerId,
                Data = clientId
            };
            SendMessage(stream, assignIdMessage);
            Console.WriteLine($"Sent assigned ID {clientId} to client.");

            // Bước 3: Thông báo cho tất cả client khác rằng có client mới kết nối
            BroadCast($"{clientId} đã kết nối.", clients[clientId]);

            // Bước 4: Lắng nghe tin nhắn từ client
            while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string receivedData = Encoding.UTF8.GetString(buffer, 0, byteCount).Trim();
                Console.WriteLine($"Received from {clientId}: {receivedData}");

                // Deserialize ProtocolMessage từ JSON
                var protocolMessage = JsonConvert.DeserializeObject<ProtocolMessage<object>>(receivedData);

                switch ((ClientToServerOperationCode)protocolMessage.ProtocolType)
                {
                    case ClientToServerOperationCode.GetMessage:
                        HandleGetMessage(clientId, stream);
                        break;

                    case ClientToServerOperationCode.SendMessage:
                        HandleSendMessage(clientId, protocolMessage.Data);
                        break;

                    // Thêm các case khác nếu cần
                    default:
                        Console.WriteLine($"Unknown protocol type: {protocolMessage.ProtocolType}");
                        break;
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
                lock (_lockObj)
                {
                    if (clients.ContainsKey(clientId))
                    {
							// Thông báo cho tất cả client khác rằng client đã ngắt kết nối
							BroadCast($"{clientId} đã ngắt kết nối.", clients[clientId]);
							clients.Remove(clientId);
                    }
                }
            }
            client.Close();
            Console.WriteLine($"Client {clientId ?? "Unknown"} disconnected.");
        }
    }

        private void BroadCast(string message, TcpClient excludeClient)
        {
            var broadcastMessage = new ProtocolMessage<string>
            {
                ProtocolType = (int)ServerToClientOperationCode.GetMessageResponse,
                Data = message
            };

            string json = JsonConvert.SerializeObject(broadcastMessage) + "\n";
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            lock (_lockObj)
            {
                foreach (var clientPair in clients)
                {
                    string id = clientPair.Key;
                    TcpClient client = clientPair.Value;
                    if (client != excludeClient)
                    {
                        try
                        {
                            NetworkStream stream = client.GetStream();
                            if (stream.CanWrite)
                            {
                                stream.Write(buffer, 0, buffer.Length);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error broadcasting to {id}: {ex.Message}");
                        }
                    }
                }
            }
        }

        private string GenerateClientId()
        {
            lock (_lockObj)
            {
                _clientCounter++;
                return $"{_clientCounter}";
            }
        }

        private void SendMessageToSpecificClient(string clientId, MessageDTO messageDTO)
        {
            lock (_lockObj)
            {
                if (clients.ContainsKey(clientId))
                {
                    TcpClient client = clients[clientId];
                    NetworkStream stream = client.GetStream();
                    var protocolMessage = new ProtocolMessage<MessageDTO>
                    {
                        ProtocolType = (int)ServerToClientOperationCode.GetMessageResponse,
                        Data = messageDTO
                    };
                    SendMessage(stream, protocolMessage);
                    Console.WriteLine($"Sent to {clientId}: {messageDTO.Content}");
                }
                else
                {
                    Console.WriteLine($"Client {clientId} not found.");
                }
            }
        }

        private void HandleGetMessage(string clientId, NetworkStream stream)
        {
            // Ví dụ: Tạo một MessageDTO để gửi về client
            var messageDTO = new MessageDTO
            {
                SenderId = "Server",
                Content = "Đây là tin nhắn từ server.",
                Timestamp = DateTime.Now
            };

            var protocolMessage = new ProtocolMessage<MessageDTO>
            {
                ProtocolType = (int)ServerToClientOperationCode.GetMessageResponse,
                Data = messageDTO
            };

            SendMessage(stream, protocolMessage);
            Console.WriteLine($"Sent GetMessage response to {clientId}.");
        }

        private void HandleSendMessage(string clientId, object data)
        {
            // Giả sử data là một chuỗi JSON chứa MessageDTO
            var messageJson = data.ToString();
            var messageDTO = JsonConvert.DeserializeObject<MessageDTO>(messageJson);

            // Xử lý tin nhắn (ví dụ: lưu vào cơ sở dữ liệu, phát lại cho các client khác, v.v.)
            Console.WriteLine($"Processing SendMessage from {clientId}: {messageDTO.Content}");

            // Ví dụ: Phát tin nhắn này cho tất cả các client khác
            BroadCast($"{messageDTO.SenderId}: {messageDTO.Content}", clients[clientId]);
        }
    }

    public struct MessageDTO
    {
        public string SenderId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }

// ProtocolMessage.cs
    public struct ProtocolMessage<T>
    {
        public int ProtocolType { get; set; }
        public T Data { get; set; }
    }

    public enum ServerToClientOperationCode
    {
        UpdatePlayerId=0,
        GetMessageResponse = 1,
        MessageReceived = 2,
        // Thêm các operation code khác nếu cần
    }

// ClientToServerOperationCode.cs
    public enum ClientToServerOperationCode
    {
        GetMessage = 1,
        SendMessage = 2,
        // Thêm các operation code khác nếu cần
    }
}
