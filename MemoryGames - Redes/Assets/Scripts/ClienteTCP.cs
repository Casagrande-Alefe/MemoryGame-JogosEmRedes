using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ClienteTCP : MonoBehaviour
{
    [Header("UI de conexão")]
    public GameObject connectUI;        // Painel com inputField e botão
    public InputField ipInputField;     // Campo para digitar o IP
    public Button connectButton;        // Botão conectar

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private CardController cardController;

    void Start()
    {
        // Pega o CardController da cena para mandar as mensagens recebidas pra ele
        cardController = FindObjectOfType<CardController>();

        connectButton.onClick.AddListener(() =>
        {
            string ip = ipInputField.text.Trim();
            if (!string.IsNullOrEmpty(ip))
            {
                ConnectToServer(ip);
            }
            else
            {
                Debug.LogWarning("Digite um IP válido!");
            }
        });
    }

    void ConnectToServer(string serverIP)
    {
        try
        {
            client = new TcpClient(serverIP, 8080);
            stream = client.GetStream();

            Debug.Log("Conectado ao servidor TCP no IP: " + serverIP);

            // Esconde a UI de conexão depois de conectar
            if (connectUI != null)
                connectUI.SetActive(false);

            // Começa a thread que vai ficar escutando o servidor
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Erro ao conectar: " + ex.Message);
        }
    }

    void ReceiveData()
    {
        byte[] buffer = new byte[1024];

        while (client != null && client.Connected)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("[Cliente] Recebeu: " + message);

                // Envia para o CardController processar na thread principal (importante para Unity)
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    cardController.ProcessServerMessage(message);
                });
            }
            catch
            {
                break;
            }
        }

        // Se desconectar, mostra novamente a UI para tentar reconectar
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (connectUI != null)
                connectUI.SetActive(true);
            Debug.LogWarning("Desconectado do servidor.");
        });
    }

    public void SendMessageToServer(string message)
    {
        if (client == null || !client.Connected) return;

        byte[] data = Encoding.UTF8.GetBytes(message);

        try
        {
            stream.Write(data, 0, data.Length);
            Debug.Log($"[Cliente] Enviou: {message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erro ao enviar mensagem: " + e.Message);
        }
    }

    private void OnApplicationQuit()
    {
        receiveThread?.Abort();
        stream?.Close();
        client?.Close();
    }
}

