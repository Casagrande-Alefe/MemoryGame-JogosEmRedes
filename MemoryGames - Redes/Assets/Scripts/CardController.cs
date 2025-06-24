using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;

public class CardController : MonoBehaviour
{
    [SerializeField] Card cardPrefab;
    [SerializeField] Transform gridTransform;
    [SerializeField] Sprite[] sprites;
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
       

        if (card.isSelected == false)
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
                firstCard =  null;
                secondCard = null;
            }
        }
    }

    IEnumerator CheckMatching(Card a, Card b)
    {
        
        yield return new WaitForSeconds(0.3f);
        if (a.iconSprite == b.iconSprite) //quando um par é encontrado --> pontuação
        {
            matchCounter++;
            
            playerScores[currentPlayer]++;
            Debug.Log($"Player {currentPlayer + 1} fez ponto! Agora tem {playerScores[currentPlayer]} pontos.");


            if (matchCounter >= spritePairs.Count / 2)
            {
                PrimeTween.Sequence.Create()
                    .Chain(PrimeTween.Tween.Scale(gridTransform, Vector3.one * 1.2f, 0.2f,
                        ease: PrimeTween.Ease.OutBack))
                    .Chain(PrimeTween.Tween.Scale(gridTransform, Vector3.one, 0.1f));
                
                if (playerScores[0] > playerScores[1])
                {
                    Debug.Log("Player 1 venceu!");
                }
                else if (playerScores[1] > playerScores[0])
                {
                    Debug.Log("Player 2 venceu!");
                }
                else
                {
                    Debug.Log("Empate!");
                }

            }
        }
        else
        {
         

            a.Hide();
            b.Hide();
            
            
            currentPlayer = (currentPlayer + 1) % 2; //passa a vez pro segundo jogador se errar o match

            Debug.Log($"Errou! Agora é a vez do Player {currentPlayer + 1}");
        }
    }
    
}
