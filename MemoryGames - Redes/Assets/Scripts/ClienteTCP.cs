using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClienteTCP : MonoBehaviour
{
    [Header("UI de conexão")]
    public GameObject connectUI;        // Canvas ou painel com InputField + Button
    public InputField ipInputField;     // Campo para digitar o IP
    public Button connectButton;        // Botão para conectar

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private CardController cardController;

    private string serverIP;

    void Start()
    {
        cardController = FindObjectOfType<CardController>();

        // Configura o botão para chamar o método ConnectToServer com o IP digitado
        connectButton.onClick.AddListener(() =>
        {
            serverIP = ipInputField.text.Trim();
            if (!string.IsNullOrEmpty(serverIP))
            {
                ConnectToServer();
            }
            else
            {
                Debug.LogWarning("Digite um IP válido!");
            }
        });
    }

    public void ConnectToServer()
    {
        try
        {
            client = new TcpClient(serverIP, 8080);
            stream = client.GetStream();
            Debug.Log("Conectado ao servidor TCP no IP: " + serverIP);

            // Esconder a UI de conexão, mas o objeto ClienteTCP continua ativo!
            if (connectUI != null)
                connectUI.SetActive(false);

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Erro ao conectar: " + ex.Message);
        }
    }

    public void SendJogada(int cardId, int playerId)
    {
        if (client == null || !client.Connected) return;

        string message = $"JOGADA|{cardId}|{playerId}";
        byte[] data = Encoding.UTF8.GetBytes(message);

        stream.Write(data, 0, data.Length);
        Debug.Log($"[Cliente] Enviou: {message}");
    }

    void ReceiveData()
    {
        byte[] buffer = new byte[1024];

        while (client.Connected)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("[Cliente] Recebeu: " + message);

                // Processa a mensagem na thread principal
                UnityMainThreadDispatcher.Instance().Enqueue(() => ProcessMessage(message));
            }
            catch
            {
                break;
            }
        }
    }

    void ProcessMessage(string message)
    {
        if (!message.StartsWith("JOGADA|")) return;

        string[] parts = message.Split('|');
        if (parts.Length != 3) return;

        int cardId = int.Parse(parts[1]);
        int playerId = int.Parse(parts[2]);

        cardController.ReceiveJogada(cardId, playerId);
    }

    private void OnApplicationQuit()
    {
        receiveThread?.Abort();
        stream?.Close();
        client?.Close();
    }
}
