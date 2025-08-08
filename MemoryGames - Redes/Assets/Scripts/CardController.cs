using System;
using System.Collections.Generic;
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

    // Referência para o ClienteTCP para enviar mensagens
    public ClienteTCP clienteTCP;

    public int meuId = -1;              // ID do jogador (1 ou 2), começa -1 esperando o servidor setar
    private int currentPlayer = -1;     // Quem está jogando agora (1 ou 2)

    private List<Card> cartas = new List<Card>();

    // Estados das cartas: 0=escondida, 1=virada, 2=achada
    private int[] cardStates;
    private int[] playerScores = new int[2];

    private Card firstCard = null;
    private Card secondCard = null;

    private bool esperandoJogada = false; // bloqueia input até receber resposta do servidor

    void Start()
    {
        victoryText.gameObject.SetActive(false);
        revengeButton.gameObject.SetActive(false);

        p1ScoreText.text = "P1 Score: 0";
        p2ScoreText.text = "P2 Score: 0";
        playerTurnText.text = "Aguardando conexão...";

        revengeButton.onClick.AddListener(RestartGame);

        CreateCards();

        // Tenta achar o ClienteTCP na cena
        clienteTCP = FindObjectOfType<ClienteTCP>();
        if (clienteTCP == null)
            Debug.LogWarning("ClienteTCP não encontrado na cena! A conexão não funcionará.");
    }

    void CreateCards()
    {
        cartas.Clear();
        gridTransform.DetachChildren(); // Limpa grid caso reinicie

        for (int i = 0; i < sprites.Length * 2; i++)
        {
            Card card = Instantiate(cardPrefab, gridTransform);
            card.SetIconSprite(sprites[i % sprites.Length]);
            card.cardId = i;
            card.index = i;
            card.controller = this;
            card.Hide();
            cartas.Add(card);
        }

        cardStates = new int[cartas.Count];
        for (int i = 0; i < cardStates.Length; i++)
            cardStates[i] = 0;
    }

    // Chamado pelo ClienteTCP quando chega mensagem do servidor
    public void ProcessServerMessage(string msg)
    {
        if (msg.StartsWith("id:"))
        {
            meuId = int.Parse(msg.Substring(3));
            Debug.Log("Meu ID: " + meuId);
            playerTurnText.text = "Esperando partida iniciar...";
        }
        else if (msg.StartsWith("vez:"))
        {
            currentPlayer = int.Parse(msg.Substring(4));
            UpdateTurnUI();
            esperandoJogada = false; // libera input quando vez mudar
        }
        else if (msg.StartsWith("estado:"))
        {
            // Formato: estado:currentPlayer;score1,score2;cardState0,cardState1,...
            string[] parts = msg.Substring(7).Split(';');
            if (parts.Length < 3) return;

            currentPlayer = int.Parse(parts[0]);
            string[] scores = parts[1].Split(',');
            playerScores[0] = int.Parse(scores[0]);
            playerScores[1] = int.Parse(scores[1]);

            string[] cardStatesStr = parts[2].Split(',');

            UpdateGameState(cardStatesStr);
            UpdateScoresUI();
            UpdateTurnUI();

            esperandoJogada = false; // libera input após atualização completa
        }
        else if (msg.StartsWith("fim:"))
        {
            string[] pontos = msg.Substring(4).Split(',');
            int p1 = int.Parse(pontos[0]);
            int p2 = int.Parse(pontos[1]);

            string resultado = p1 > p2 ? "Jogador 1 venceu!" :
                               p2 > p1 ? "Jogador 2 venceu!" :
                               "Empate!";

            victoryText.text = resultado;
            victoryText.gameObject.SetActive(true);
            revengeButton.gameObject.SetActive(true);

            esperandoJogada = true; // bloqueia tudo quando o jogo acaba
        }
        else if (msg.StartsWith("jogada:"))
        {
            // Jogada recebida: jogada:firstIndex,secondIndex
            string[] parts = msg.Substring(7).Split(',');
            if (parts.Length != 2) return;

            int firstIndex = int.Parse(parts[0]);
            int secondIndex = int.Parse(parts[1]);

            ApplyOpponentMove(firstIndex, secondIndex);
        }
    }

    void UpdateGameState(string[] cardStatesStr)
    {
        if (cartas.Count == 0) CreateCards();

        for (int i = 0; i < cardStatesStr.Length; i++)
            cardStates[i] = int.Parse(cardStatesStr[i]);

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

    void ApplyOpponentMove(int firstIndex, int secondIndex)
    {
        Debug.Log($"Jogada do adversário: {firstIndex} e {secondIndex}");

        cartas[firstIndex].Show();
        cartas[secondIndex].Show();

        esperandoJogada = false; // libera input depois da jogada do adversário
    }

    public void SetSelected(Card card)
    {
        if (meuId == -1)
        {
            Debug.Log("Ainda não recebeu ID do servidor.");
            return;
        }

        if (meuId != currentPlayer)
        {
            Debug.Log("Não é sua vez!");
            return;
        }

        if (esperandoJogada)
        {
            Debug.Log("Esperando resposta do servidor...");
            return;
        }

        if (cardStates[card.index] != 0) // só pode selecionar carta escondida
        {
            // Carta já virada ou achada
            return;
        }

        if (firstCard == null)
        {
            firstCard = card;
            firstCard.Show();
        }
        else if (secondCard == null && card != firstCard)
        {
            secondCard = card;
            secondCard.Show();

            esperandoJogada = true;

            // Envia jogada para o servidor usando ClienteTCP
            if (clienteTCP != null)
                clienteTCP.SendMessageToServer($"jogada:{firstCard.index},{secondCard.index}");
            else
                Debug.LogWarning("ClienteTCP não está referenciado no CardController!");

            // A limpeza firstCard e secondCard deve ser feita quando receber o update do servidor
        }
    }

    void UpdateScoresUI()
    {
        p1ScoreText.text = $"Jogador 1: {playerScores[0]}";
        p2ScoreText.text = $"Jogador 2: {playerScores[1]}";
    }

    void UpdateTurnUI()
    {
        playerTurnText.text = $"Vez de: Jogador {currentPlayer}";

        player1Image.color = (currentPlayer == 1) ? Color.white : Color.gray;
        player2Image.color = (currentPlayer == 2) ? Color.white : Color.gray;
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}

