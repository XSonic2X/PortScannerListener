using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PortScannerListener;

static partial class Program
{

    static object _o = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("Мониторинг подключений к портам");
        Console.WriteLine();
        try
        {
            Console.WriteLine("Выберите тип подключения: 1 - TCP, 2 - UDP");
            string protoChoice = Console.ReadLine();
            Protocol selectedProtocol = protoChoice is "2" 
                ? Protocol.UDP 
                : Protocol.TCP;

            Console.Write("Введите начальный порт: ");
            int startPort = int.Parse(Console.ReadLine());

            Console.Write("Введите конечный порт: ");
            int endPort = int.Parse(Console.ReadLine());

            if (startPort < 1 || endPort > 65535 || startPort > endPort)
                goto ErroRange;

            Console.WriteLine($"\nЗапуск прослушивания {selectedProtocol} портов с {startPort} по {endPort}...");
            Console.WriteLine("Нажмите Ctrl+C для выхода.\n");

            List<Task> listeners = new List<Task>();

            Func<int, Task> setTest = selectedProtocol is Protocol.TCP 
                ? StartTcpListener 
                : StartUdpListener;
            for (int port = startPort; port <= endPort; port++)
                listeners.Add(setTest(port));
            await Task.WhenAll(listeners);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критическая ошибка: {ex.Message}");
        }
        return;
    ErroRange:
        Console.WriteLine("Ошибка: Некорректный диапазон портов (1-65535).");
    }

    static async Task StartTcpListener(int port)
    {
        try
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            while (true) using (TcpClient client = await listener.AcceptTcpClientAsync())
                {
                    string remoteIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    $"[{DateTime.Now:HH:mm:ss}] TCP ПРОВЕРКА: Порт {port} -> Подключение с {remoteIp}".LogSuccess();
                }
        }
        catch (SocketException)
        {
            $"Порт {port} недоступен (занят).".LogError();
        }
        catch (Exception ex)
        {
            $"Ошибка на TCP порту {port}: {ex.Message}".Log();
        }
    }

    static async Task StartUdpListener(int port)
    {
        try
        {
            using UdpClient udpClient = new UdpClient(port);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                string remoteIp = result.RemoteEndPoint.Address.ToString();
                $"[{DateTime.Now:HH:mm:ss}] UDP ПАКЕТ: Порт {port} -> Данные от {remoteIp}".LogSuccess();
            }
        }
        catch (SocketException)
        {
            $"Порт {port} недоступен (занят).".LogError();
        }
        catch (Exception ex)
        {
            $"Ошибка на UDP порту {port}: {ex.Message}".Log();
        }
    }

    static void LogError(this string txt)
    {
        lock (_o)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(txt);
            Console.ResetColor();
        }
    }

    static void LogSuccess(this string txt)
    {
        lock (_o)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(txt);
            Console.ResetColor();
        }
    }

    static void Log(this string txt)
    {
        lock (_o)
        {
            Console.WriteLine(txt);
        }
    }

}
static partial class Program
{

    enum Protocol
    {
        TCP,
        UDP
    }

}