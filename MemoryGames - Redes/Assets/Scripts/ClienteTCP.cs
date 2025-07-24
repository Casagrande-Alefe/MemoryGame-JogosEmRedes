using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ClienteTCP : MonoBehaviour
{
    [Header("UI de conexão")]
    public GameObject connectUI;        // UI para conectar (InputField + Button)
    public InputField ipInputField;     // Campo para digitar o IP
    public Button connectButton;        // Botão conectar

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private CardController cardController;

    private string serverIP;

    void Start()
    {
        // Pega a referência para o CardController na cena
        cardController = Object.FindFirstObjectByType<CardController>();

        // Configura o botão para conectar ao IP informado
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

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient(serverIP, 8080);
            stream = client.GetStream();

            Debug.Log("Conectado ao servidor TCP no IP: " + serverIP);

            // Esconde UI de conexão após conectar
            if (connectUI != null)
                connectUI.SetActive(false);

            // Inicia a thread para receber dados do servidor
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Erro ao conectar: " + ex.Message);
        }
    }

    // Envia uma jogada para o servidor
    public void SendJogada(int cardId, int playerId)
    {
        if (client == null || !client.Connected) return;

        string message = $"JOGADA|{cardId}|{playerId}";
        byte[] data = Encoding.UTF8.GetBytes(message);

        try
        {
            stream.Write(data, 0, data.Length);
            Debug.Log($"[Cliente] Enviou: {message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erro ao enviar jogada: " + e.Message);
        }
    }

    // Thread para receber dados do servidor
    void ReceiveData()
    {
        byte[] buffer = new byte[1024];

        while (client != null && client.Connected)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break; // conexão fechada

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("[Cliente] Recebeu: " + message);

                // Processa mensagem na thread principal para evitar problemas com Unity
                UnityMainThreadDispatcher.Instance().Enqueue(() => ProcessMessage(message));
            }
            catch (System.Exception)
            {
                break;
            }
        }
    }

    // Interpreta e trata as mensagens recebidas do servidor
    void ProcessMessage(string message)
    {
        if (!message.StartsWith("JOGADA|")) return;

        string[] parts = message.Split('|');
        if (parts.Length != 3) return;

        int cardId;
        int playerId;
        if (int.TryParse(parts[1], out cardId) && int.TryParse(parts[2], out playerId))
        {
            cardController.ReceiveJogada(cardId, playerId);
        }
    }

    private void OnApplicationQuit()
    {
        // Limpa tudo ao fechar o jogo
        receiveThread?.Abort();
        stream?.Close();
        client?.Close();
    }
}


