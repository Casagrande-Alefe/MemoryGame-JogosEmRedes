using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;

public class ServidorTCP : MonoBehaviour
{
    TcpListener server;
    Thread serverThread;
    List<TcpClient> clients = new List<TcpClient>();

    void Start()
    {
        serverThread = new Thread(StartServer);
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    void StartServer()
    {
        server = new TcpListener(IPAddress.Any, 8080);
        server.Start();
        Debug.Log("Servidor ouvindo na porta 8080...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            lock (clients)
            {
                clients.Add(client);
            }
            Debug.Log("Cliente conectado.");

            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.IsBackground = true;
            clientThread.Start();
        }
    }

    void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        while (client.Connected)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("[Servidor] Mensagem recebida: " + message);

                // Repassa para os outros clientes
                BroadcastMessage(message, client);
            }
            catch
            {
                break;
            }
        }

        lock (clients)
        {
            clients.Remove(client);
        }
        client.Close();
        Debug.Log("Cliente desconectado.");
    }

    void BroadcastMessage(string message, TcpClient sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        lock (clients)
        {
            foreach (var c in clients)
            {
                if (c != sender && c.Connected)
                {
                    try
                    {
                        NetworkStream stream = c.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                    catch { }
                }
            }
        }
    }

    private void OnApplicationQuit()
    {
        server?.Stop();
        serverThread?.Abort();
    }
}
