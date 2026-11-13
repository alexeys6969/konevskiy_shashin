using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Common;

namespace Server
{
    class Programm
    {
        private static ServerConfig _config;
        private static ConcurrentDictionary<string, ClientSession> _sessions = new();
        private static TcpListener _listener;

        static async Task Main(string[] args)
        {
            LoadOrCreateConfig();

            _listener = new TcpListener(IPAddress.Parse(_config.Ip), _config.Port);
            _listener.Start();
            Console.WriteLine($"[Сервер] Запущен на {_config.Ip}:{_config.Port}");
            Console.WriteLine($"[Сервер] Макс. клиентов: {_config.MaxClients}");

            _ = Task.Run(CleanupExpiredSessions);
            _ = Task.Run(AcceptClientsAsync);

            while (true)
            {
                Console.Write("\nКоманда (status, disconnect <код>, exit): ");
                var input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input)) continue;

                if (input == "exit") break;
                if (input == "status") PrintStatus();
                if (input.StartsWith("disconnect "))
                {
                    var code = input.Split(' ')[1];
                    DisconnectClient(code, "Отключен администратором");
                }
            }

            _listener.Stop();
        }
        //Метод загрузки или создания конфигурации сервера
        static void LoadOrCreateConfig()
        {
            string path = "server_config.json";
            if(File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _config = JsonSerializer.Deserialize<ServerConfig>(json);
            } 
            else
            {
                Console.WriteLine("Первоначальная настройка сервера");
                _config = new ServerConfig();
                Console.Write("IP-адрес сервера (127.0.0.1): ");
                var ip = Console.ReadLine(); if (!string.IsNullOrWhiteSpace(ip)) _config.Ip = ip;
                Console.Write("Порт (5000): ");
                if (int.TryParse(Console.ReadLine(), out int p)) _config.Port = p;
                Console.Write("Макс. кол-во клиентов (5): ");
                if (int.TryParse(Console.ReadLine(), out int m)) _config.MaxClients = m;
                Console.Write("Время отключения клиента (сек, 60): ");
                if (int.TryParse(Console.ReadLine(), out int t)) _config.SessionTimeoutSeconds = t;

                File.WriteAllText(path, JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine("Конфигурация сохранена.");
            }
        }
        //Метод приема подключений
        static async Task AcceptClientsAsync()
        {
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(client));
            }
        }

        static async Task HandleClientAsync(TcpClient client)
        {
            var endPoint = (IPEndPoint)client.Client.RemoteEndPoint;

            if (_sessions.Count >= _config.MaxClients)
            {
                SendMessage(client, new { Status = "Rejected", Message = "Достигнут лимит подключений (5/5)" });
                client.Close();
                return;
            }

            var session = new ClientSession
            {
                Ip = endPoint.Address.ToString(),
                Port = endPoint.Port,
                TcpClient = client
            };

            _sessions.TryAdd(session.UniqueCode, session);
            Console.WriteLine($"[+] Подключился клиент: {session.Ip}:{session.Port} | Код: {session.UniqueCode}");

            SendMessage(client, new { Status = "Success", UniqueCode = session.UniqueCode, Message = "Лицензия выдана" });

            try
            {
                using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
                while (client.Connected)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;

                    var req = JsonSerializer.Deserialize<JsonElement>(line);
                    if (req.TryGetProperty("Action", out var action) && action.GetString() == "Renew")
                    {
                        // Обновляем время последнего подтверждения (опционально, если нужна логика продления)
                        session.ConnectedAt = DateTime.Now;
                        SendMessage(client, new { Status = "Renewed" });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[-] Ошибка чтения от {session.Ip}: {ex.Message}");
            }
            finally
            {
                RemoveSession(session.UniqueCode, "Клиент отключился");
            }
        }

        static async Task CleanupExpiredSessions()
        {
            while (true)
            {
                await Task.Delay(1000);
                var now = DateTime.Now;
                foreach (var session in _sessions.Values)
                {
                    if ((now - session.ConnectedAt).TotalSeconds > _config.SessionTimeoutSeconds)
                    {
                        DisconnectClient(session.UniqueCode, "Истекло время сессии. Получите новый код.");
                    }
                }
            }
        }

        static void DisconnectClient(string code, string reason)
        {
            if (_sessions.TryRemove(code, out var session))
            {
                try
                {
                    SendMessage(session.TcpClient, new { Status = "Disconnected", Message = reason });
                    session.TcpClient.Close();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Что-то пошло не так: {ex.Message}");
                }
                Console.WriteLine($"[-] Отключен клиент {code}. Причина: {reason}");
            }
        }

        static void RemoveSession(string code, string reason)
        {
            if (_sessions.TryRemove(code, out var session))
            {
                session.TcpClient.Close();
                Console.WriteLine($"[-] Удалена сессия {code}. Причина: {reason}");
            }
        }

        static void PrintStatus()
        {
            Console.WriteLine("\n--- Активные сессии ---");
            Console.WriteLine($"Всего: {_sessions.Count}/{_config.MaxClients}");
            foreach (var s in _sessions.Values)
            {
                var duration = (DateTime.Now - s.ConnectedAt).ToString(@"hh\:mm\:ss");
                Console.WriteLine($"Код: {s.UniqueCode} | IP: {s.Ip}:{s.Port} | Подключен: {s.ConnectedAt:HH:mm:ss} | Длит.: {duration}");
            }
            Console.WriteLine("-----------------------");
        }

        static void SendMessage(TcpClient client, object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data) + "\n";
                var bytes = Encoding.UTF8.GetBytes(json);
                client.GetStream().Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Что-то пошло не так: {ex.Message}");
            }
        }
    }
}
