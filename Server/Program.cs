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
            //LoadOrCreateConfig(); пока несозданный метод

            _listener = new TcpListener(IPAddress.Parse(_config.Ip), _config.Port);
            _listener.Start();
            Console.WriteLine($"[Сервер] Запущен на {_config.Ip}:{_config.Port}");
            Console.WriteLine($"[Сервер] Макс. клиентов: {_config.MaxClients}");

            //_ = Task.Run(CleanupExpiredSessions); пока несозданный метод, фоновая проверка таймаутов
            //_ = Task.Run(AcceptClientAsync); пока несозданный метод, запуск приема подключений

            while (true)
            {
                Console.Write("\nКоманда (status, disconnect <код>, exit): ")
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
    }
}
