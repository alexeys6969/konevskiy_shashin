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

            //_ = Task.Run(CleanupExpiredSessions); пока несозданный метод, фоновая проверка таймаутов
            //_ = Task.Run(AcceptClientAsync); пока несозданный метод, запуск приема подключений

            while (true)
            {
                Console.Write("\nКоманда (status, disconnect <код>, exit): ");
                var input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input)) continue;

                if (input == "exit") break;
                //if (input == "status") PrintStatus();
                if (input.StartsWith("disconnect "))
                {
                    var code = input.Split(' ')[1];
                    //DisconnectClient(code, "Отключен администратором");
                }
            }

            _listener.Stop();
        }

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
    }
}
