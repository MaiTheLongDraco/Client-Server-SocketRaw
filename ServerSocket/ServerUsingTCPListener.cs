using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ServerSocket
{
    public class ServerUsingTCPListener
    {//192.168.100.170
        TcpListener listener;
        public int port = 5000;
        public string host = "192.168.100.170";
        public Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
        private object _lockObj = new object();
        private int _clientCounter = 0; // Biến đếm để tạo ID duy nhất

        public void Start()
        {
            IPAddress iPAddress = IPAddress.Parse(GetLocalIPv4Address());
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
        
        private string GetLocalIPv4Address()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Bỏ qua các interface không hoạt động hoặc là loopback
                if (ni.OperationalStatus != OperationalStatus.Up ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var ipProperties = ni.GetIPProperties();

                foreach (UnicastIPAddressInformation ip in ipProperties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.Address.ToString();
                    }
                }
            }
            return null;
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
            var assignIdMessage = new ProtocolMessage<ClientIdDto>
            {
                ProtocolType = (int)ServerToClientOperationCode.UpdatePlayerId,
                Data = new ClientIdDto(){Id = clientId}
            };
            SendMessage(stream, assignIdMessage);
            Console.WriteLine($"Sent assigned ID {clientId} to client.");

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
                        // HandleGetMessage(clientId, stream);
                        break;
                    case ClientToServerOperationCode.SendMessage:
                        HandleClientSendPublicMessage(clientId, protocolMessage.Data);
                        break;
                    case ClientToServerOperationCode.SendPrivateMessage:
                        HandleClientSendPrivateMessage(protocolMessage.Data);
                        break; 
                    case ClientToServerOperationCode.NotifyNewPlayer:
                        NotiFyNewUser(protocolMessage.Data, client);
                        break;
                    case ClientToServerOperationCode.SendAudio:
                        HandleClientSendVoiceMessage(protocolMessage.Data);
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
                        PublicMessageDTO publicMessageDto = new PublicMessageDTO()
                        {
                            SenderId = clientId,
                            Content = $"{clientId} đã ngắt kết nối."
                        };
							// Thông báo cho tất cả client khác rằng client đã ngắt kết nối
							BroadCast(publicMessageDto, clients[clientId]);
							clients.Remove(clientId);
                    }
                }
            }
            client.Close();
            Console.WriteLine($"Client {clientId ?? "Unknown"} disconnected.");
        }
    }

     

        private void BroadCast(PublicMessageDTO message, TcpClient excludeClient)
        {
            var broadcastMessage = new ProtocolMessage<PublicMessageDTO>
            {
                ProtocolType = (int)ServerToClientOperationCode.MessageReceived,
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
        private void BroadCastVoice(VoiceMessagePack message, TcpClient excludeClient)
        {
            var broadcastMessage = new ProtocolMessage<VoiceMessagePack>
            {
                ProtocolType = (int)ServerToClientOperationCode.AudioReceived,
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
        private void NotiFyNewUser(object data,TcpClient excludeClient)
        {
            var messageJson = data.ToString();
            var messageDTO = JsonConvert.DeserializeObject<NotifyNewPlayerDTO>(messageJson);
            BroadCastNewUser(messageDTO,excludeClient);
        }
        private void BroadCastNewUser(NotifyNewPlayerDTO message, TcpClient excludeClient)
        {
            var broadcastMessage = new ProtocolMessage<NotifyNewPlayerDTO>
            {
                ProtocolType = (int)ServerToClientOperationCode.NotifyNewPlayer,
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

        private void SendMessageToSpecificClient(string clientId, PrivateMessageDTO messageDTO)
        {
            lock (_lockObj)
            {
                if (clients.ContainsKey(clientId))
                {
                    TcpClient client = clients[clientId];
                    NetworkStream stream = client.GetStream();
                    var protocolMessage = new ProtocolMessage<PrivateMessageDTO>
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

        private void HandleClientSendPublicMessage(string clientId, object data)
        {
            // Giả sử data là một chuỗi JSON chứa MessageDTO
            var messageJson = data.ToString();
            var messageDTO = JsonConvert.DeserializeObject<PublicMessageDTO>(messageJson);

            // Xử lý tin nhắn (ví dụ: lưu vào cơ sở dữ liệu, phát lại cho các client khác, v.v.)
            Console.WriteLine($"Processing SendMessage from {clientId}: {messageDTO.Content}");

            // Ví dụ: Phát tin nhắn này cho tất cả các client khác
            BroadCast(messageDTO, clients[clientId]);
        }
        private void HandleClientSendPrivateMessage(object data)
        {
            // Giả sử data là một chuỗi JSON chứa MessageDTO
            var messageJson = data.ToString();
            var messageDTO = JsonConvert.DeserializeObject<PrivateMessageDTO>(messageJson);

            // Xử lý tin nhắn (ví dụ: lưu vào cơ sở dữ liệu, phát lại cho các client khác, v.v.)
            Console.WriteLine($"Processing SendMessage from {messageDTO.SenderId}: {messageDTO.Content}");

            // Ví dụ: Phát tin nhắn này cho tất cả các client khác
            SendMessageToSpecificClient(messageDTO.TargetID,messageDTO);
        }

        private void HandleClientSendVoiceMessage(object data)
        {
            var messageJson = data.ToString();
            var messageDto = JsonConvert.DeserializeObject<VoiceMessagePack>(messageJson);
            if (messageDto.TargetId != String.Empty)
            {
                lock (_lockObj)
                {
                    if (clients.ContainsKey(messageDto.TargetId))
                    {
                        TcpClient client = clients[messageDto.TargetId];
                        NetworkStream stream = client.GetStream();
                        var protocolMessage = new ProtocolMessage<VoiceMessagePack>
                        {
                            ProtocolType = (int)ServerToClientOperationCode.AudioReceived,
                            Data = messageDto
                        };
                        SendMessage(stream, protocolMessage);
                        Console.WriteLine($"Sent to {messageDto.TargetId}: {messageDto.SenderName} voice data count {messageDto.ByteData.Length}");
                    }
                    else
                    {
                        Console.WriteLine($"Client {messageDto.TargetId} not found.");
                    }
                }
            }
            else
            {
                BroadCastVoice(messageDto, clients[messageDto.SenderId]);
            }
        }
    }

    public struct MessageDTO
    {
        public string SenderId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
    public struct PublicMessageDTO
    {
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public int EmojiIndex { get; set; }
        public DateTime Timestamp { get; set; }
        public override string ToString()
        {
            return
                $"SenderID {SenderId} SenderName {SenderName} Content {Content} EmojiIndex {EmojiIndex} TimeSend {Timestamp.ToString()}";
        }
    }
    public struct PrivateMessageDTO
    {
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string TargetID { get; set; }
        public string Content { get; set; }
        public int EmojiIndex { get; set; }
        public DateTime Timestamp { get; set; }
    }
    public struct NotifyNewPlayerDTO
    {
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
    }
// ProtocolMessage.cs
    public struct ProtocolMessage<T>
    {
        public int ProtocolType { get; set; }
        public T Data { get; set; }
    }

    public struct ClientIdDto
    {
        public string Id;
    }
    public struct VoiceMessagePack
    {
        public string SenderId { get; set; }
        public string TargetId { get; set; }
        public string SenderName { get; set; }
        public byte[] ByteData { get; set; }
    }
    public enum ServerToClientOperationCode
    {
        UpdatePlayerId=0,
        GetMessageResponse = 1,
        MessageReceived = 2,
        NotifyNewPlayer=3,
        AudioReceived=4
        // Thêm các operation code khác nếu cần
    }

    public enum ClientToServerOperationCode
    {
        GetMessage = 1,
        SendMessage = 2,
        SendPrivateMessage = 3,
        NotifyNewPlayer=4,
        SendAudio=5
        // Thêm các operation code khác nếu cần
    }
   
}
