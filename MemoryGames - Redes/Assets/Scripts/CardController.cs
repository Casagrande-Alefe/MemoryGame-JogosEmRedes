using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardController : MonoBehaviour
{
    [SerializeField] Card cardPrefab;
    [SerializeField] Transform gridTransform;
    [SerializeField] Sprite[] sprites;
    [SerializeField] TMP_Text playerTurnText;
    [SerializeField] TMP_Text victoryText;
    [SerializeField] TMP_Text p1ScoreText;
    [SerializeField] TMP_Text p2ScoreText;

    [SerializeField] private Image player1Image;
    [SerializeField] private Image player2Image;

    [SerializeField] private Button revengeButton;

    // Networking
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;

    private int meuId = -1;
    private int currentPlayer = 0;

    private List<Card> cartas = new List<Card>();

    private int[] cardStates; // 0 = escondida, 1 = virada, 2 = achada
    private int[] playerScores = new int[2];

    private Card firstCard;
    private Card secondCard;

    void Start()
    {
        victoryText.gameObject.SetActive(false);
        revengeButton.gameObject.SetActive(false);

        p1ScoreText.text = "P1 score: 0";
        p2ScoreText.text = "P2 score: 0";
        playerTurnText.text = "Aguardando conexão...";

        revengeButton.onClick.AddListener(RestartGame);

        ConnectToServer();
    }

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 8080);
            stream = client.GetStream();

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao conectar: " + e.Message);
        }
    }

    void ReceiveData()
    {
        byte[] buffer = new byte[1024];
        while (true)
        {
            try
            {
                int len = stream.Read(buffer, 0, buffer.Length);
                if (len == 0) break;

                string msg = Encoding.UTF8.GetString(buffer, 0, len);
                Debug.Log("[Cliente] Mensagem recebida: " + msg);

                ProcessServerMessage(msg);
            }
            catch (Exception e)
            {
                Debug.LogError("Erro ao receber dados: " + e.Message);
                break;
            }
        }
    }

    void ProcessServerMessage(string msg)
    {
        if (msg.StartsWith("id:"))
        {
            meuId = int.Parse(msg.Substring(3));
            Debug.Log("Meu ID: " + meuId);
        }
        else if (msg.StartsWith("vez:"))
        {
            currentPlayer = int.Parse(msg.Substring(4));
            UpdateTurnUI();
        }
        else if (msg.StartsWith("estado:"))
        {
            // Formato esperado: estado:currentPlayer;score1,score2;cardStatesSeparatedByComma
            string[] parts = msg.Substring(7).Split(';');
            if (parts.Length < 3) return;

            currentPlayer = int.Parse(parts[0]);

            string[] scores = parts[1].Split(',');
            playerScores[0] = int.Parse(scores[0]);
            playerScores[1] = int.Parse(scores[1]);

            string[] cardStatesStr = parts[2].Split(',');

            // Atualizar cartas e UI na main thread
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                UpdateGameState(cardStatesStr);
                UpdateScoresUI();
                UpdateTurnUI();
            });
        }
        else if (msg.StartsWith("fim:"))
        {
            string[] pontos = msg.Substring(4).Split(',');
            int p1 = int.Parse(pontos[0]);
            int p2 = int.Parse(pontos[1]);

            string result;
            if (p1 > p2) result = "Player 1 venceu!";
            else if (p2 > p1) result = "Player 2 venceu!";
            else result = "Empate!";

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                victoryText.text = result;
                victoryText.gameObject.SetActive(true);
                revengeButton.gameObject.SetActive(true);
            });
        }
    }

    void UpdateGameState(string[] cardStatesStr)
    {
        // Se as cartas não foram criadas ainda, cria
        if (cartas.Count == 0)
        {
            CreateCards();
        }

        cardStates = new int[cardStatesStr.Length];

        for (int i = 0; i < cardStatesStr.Length; i++)
        {
            cardStates[i] = int.Parse(cardStatesStr[i]);
        }

        for (int i = 0; i < cartas.Count; i++)
        {
            if (cardStates[i] == 0)
            {
                cartas[i].Hide();
                cartas[i].SetAchado(false);
            }
            else if (cardStates[i] == 1)
            {
                cartas[i].Show();
                cartas[i].SetAchado(false);
            }
            else if (cardStates[i] == 2)
            {
                cartas[i].Show();
                cartas[i].SetAchado(true);
            }
        }
    }

    void CreateCards()
    {
        for (int i = 0; i < sprites.Length * 2; i++)
        {
            Card card = Instantiate(cardPrefab, gridTransform);
            card.SetIconSprite(sprites[i % sprites.Length]);
            card.cardId = i;
            card.index = i;
            card.controller = this;
            cartas.Add(card);
            card.Hide();
        }
    }

    public void SetSelected(Card card)
    {
        if (meuId != currentPlayer)
        {
            Debug.Log("Não é sua vez!");
            return;
        }

        if (cardStates[card.index] != 0)
        {
            // Já virada ou achada
            return;
        }

        if (firstCard == null)
        {
            firstCard = card;
            card.Show();
        }
        else if (secondCard == null && card != firstCard)
        {
            secondCard = card;
            card.Show();

            // Envia jogada para o servidor com as duas cartas escolhidas
            SendMessageToServer($"jogada:{firstCard.index},{secondCard.index}");

            firstCard = null;
            secondCard = null;
        }
    }

    void SendMessageToServer(string message)
    {
        if (stream == null) return;

        byte[] data = Encoding.UTF8.GetBytes(message);
        try
        {
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao enviar mensagem: " + e.Message);
        }
    }

    void UpdateScoresUI()
    {
        p1ScoreText.text = $"P1 Score: {playerScores[0]}";
        p2ScoreText.text = $"P2 Score: {playerScores[1]}";
    }

    void UpdateTurnUI()
    {
        playerTurnText.text = $"Vez de: player {currentPlayer + 1}";

        if (currentPlayer == 0)
        {
            player1Image.color = Color.white;
            player2Image.color = Color.gray;
        }
        else
        {
            player1Image.color = Color.gray;
            player2Image.color = Color.white;
        }

        MusicManager.Instance.PlayMusicForPlayer(currentPlayer);
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void OnApplicationQuit()
    {
        if (stream != null) stream.Close();
        if (client != null) client.Close();
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
    }
    public void ReceiveJogada(int cardId, int playerId)
    {
        Debug.Log($"Recebido jogada: jogador {playerId} virou carta {cardId}");

        // Procure a carta pelo ID para mostrar ou atualizar
        foreach (Card c in cartas)
        {
            if (c.cardId == cardId)
            {
                if (!c.isSelected && !c.isAchado)
                {
                    c.Show();
                }
                break;
            }
        }

        currentPlayer = playerId;
        UpdateTurnUI();
    }
}
