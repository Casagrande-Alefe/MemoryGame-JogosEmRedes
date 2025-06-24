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
        if (a.iconSprite == b.iconSprite)
        {
            matchCounter++;
            if (matchCounter >= spritePairs.Count / 2)
            {
                PrimeTween.Sequence.Create()
                    .Chain(PrimeTween.Tween.Scale(gridTransform, Vector3.one * 1.2f, 0.2f,
                        ease: PrimeTween.Ease.OutBack))
                    .Chain(PrimeTween.Tween.Scale(gridTransform, Vector3.one, 0.1f));
            }
        }
        else
        {
            a.Hide();
            b.Hide();
        }
    }
    
}
