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

            int id = clients.Count; // 1, 2, 3...
            Debug.Log($"Cliente conectado. ID = {id}");

            // Envia ID para o cliente
            SendMessageToClient(client, $"id:{id}");

            // Cria thread para tratar mensagens deste cliente
            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.IsBackground = true;
            clientThread.Start();

            // Se já tem 2 jogadores, começa o jogo
            if (clients.Count == 2)
            {
                Debug.Log("2 jogadores conectados. Iniciando o jogo...");
                BroadcastMessage("INICIAR_JOGO", null);
            }
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
                Debug.Log("[Servidor] Recebido: " + message);

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
                if ((sender == null || c != sender) && c.Connected)
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

    void SendMessageToClient(TcpClient client, string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);
        }
        catch { }
    }

    private void OnApplicationQuit()
    {
        server?.Stop();
        serverThread?.Abort();
    }
}
