using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
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

    private List<Sprite> spritePairs;
    Card firstCard;
    Card secondCard;
    int matchCounter;

    int[] playerScores = new int[2]; // índice 0 = player 1, índice 1 = player 2
    int currentPlayer = 0; // 0 é player 1, 1 é player 2

    private void Start()
    {
        PrepareSprites();
        CreateCards();
        playerTurnText.text = $"vez de: player 1";

        p1ScoreText.text = "P1 score: 0";
        p2ScoreText.text = "P2 score: 0";

        MusicManager.Instance.PlayMusicForPlayer(currentPlayer);
        AtualizarImagemDoTurno();

        revengeButton.gameObject.SetActive(false); // botão invisível no começo
    }

    private void PrepareSprites()
    {
        spritePairs = new List<Sprite>();
        for (int i = 0; i < sprites.Length; i++)
        {
            spritePairs.Add(sprites[i]);
            spritePairs.Add(sprites[i]);
        }

        ShuffleSprites(spritePairs);
    }

    private void ShuffleSprites(List<Sprite> spritelist)
    {
        for (int i = spritelist.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Sprite temp = spritelist[i];
            spritelist[i] = spritelist[j];
            spritelist[j] = temp;
        }
    }

    void CreateCards()
    {
        for (int i = 0; i < spritePairs.Count(); i++)
        {
            Card card = Instantiate(cardPrefab, gridTransform);
            card.SetIconSprite(spritePairs[i]);
            card.controller = this;
        }
    }

    public void SetSelected(Card card)
    {
        if (!card.isSelected)
        {
            card.Show();

            if (firstCard == null)
            {
                firstCard = card;
                return;
            }

            if (secondCard == null)
            {
                secondCard = card;
                StartCoroutine(CheckMatching(firstCard, secondCard));
                firstCard = null;
                secondCard = null;
            }
        }
    }

    IEnumerator CheckMatching(Card a, Card b)
    {
        yield return new WaitForSeconds(0.3f);

        if (a.iconSprite == b.iconSprite) // acerto
        {
            matchCounter++;
            playerScores[currentPlayer]++;

            if (currentPlayer == 0)
            {
                p1ScoreText.text = $"P1 Score: {playerScores[currentPlayer]}";
            }
            else
            {
                p2ScoreText.text = $"P2 Score: {playerScores[currentPlayer]}";
            }

            Debug.Log($"player {currentPlayer + 1} fez ponto! Agora tem {playerScores[currentPlayer]} pontos.");

            if (matchCounter >= spritePairs.Count / 2)
            {
                PrimeTween.Sequence.Create()
                    .Chain(PrimeTween.Tween.Scale(gridTransform, Vector3.one * 1.2f, 0.2f, ease: PrimeTween.Ease.OutBack))
                    .Chain(PrimeTween.Tween.Scale(gridTransform, Vector3.one, 0.1f));

                string result;

                if (playerScores[0] > playerScores[1])
                {
                    result = "player 1 venceu!";
                }
                else if (playerScores[1] > playerScores[0])
                {
                    result = "player 2 venceu!";
                }
                else
                {
                    result = "empate";
                }

                victoryText.text = result;
                victoryText.gameObject.SetActive(true);
                revengeButton.gameObject.SetActive(true); // ativa botão no fim do jogo
            }
        }
        else // erro → troca turno
        {
            a.Hide();
            b.Hide();

            currentPlayer = (currentPlayer + 1) % 2;

            Debug.Log($"errou! agora é a vez do player {currentPlayer + 1}");
            playerTurnText.text = $"vez de: player {currentPlayer + 1}";

            MusicManager.Instance.PlayMusicForPlayer(currentPlayer);
            AtualizarImagemDoTurno();
        }
    }

    private void AtualizarImagemDoTurno()
    {
        if (currentPlayer == 0)
        {
            player1Image.color = Color.white; // Aceso
            player2Image.color = Color.gray;  // Apagado
        }
        else
        {
            player1Image.color = Color.gray;
            player2Image.color = Color.white;
        }
    }

    // Método para reiniciar o jogo ao clicar no botão Revanche
    public void RestartGame()
    {
        victoryText.gameObject.SetActive(false);
        revengeButton.gameObject.SetActive(false);

        matchCounter = 0;
        playerScores[0] = 0;
        playerScores[1] = 0;
        currentPlayer = 0;

        p1ScoreText.text = "P1 score: 0";
        p2ScoreText.text = "P2 score: 0";
        playerTurnText.text = "vez de: player 1";

        // Destroi todas as cartas atuais
        foreach (Transform child in gridTransform)
        {
            Destroy(child.gameObject);
        }

        PrepareSprites();
        CreateCards();

        MusicManager.Instance.PlayMusicForPlayer(currentPlayer);
        AtualizarImagemDoTurno();
    }
}
