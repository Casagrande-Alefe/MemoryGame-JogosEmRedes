using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

public class Card : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    public Sprite hiddenIconSprite;
    public Sprite iconSprite;

    public bool isSelected;
    public bool isAchado; // indica se a carta já foi encontrada
    public int cardId;    // ID único da carta
    public int index;     // índice usado no controller para acessar o estado

    public CardController controller;

    public void OnCardClick()
    {
        if (!isSelected && !isAchado)
        {
            controller.SetSelected(this);
        }
    }

    public void SetIconSprite(Sprite sp)
    {
        iconSprite = sp;
    }

    public void Show()
    {
        Tween.Rotation(transform, new Vector3(0f, 180f, 0f), 0.2f);

        Tween.Delay(0.1f, () =>
        {
            iconImage.sprite = iconSprite;
            isSelected = true;
        });
    }

    public void Hide()
    {
        Tween.Rotation(transform, new Vector3(0f, 0f, 0f), 0.2f);

        Tween.Delay(0.1f, () =>
        {
            iconImage.sprite = hiddenIconSprite;
            isSelected = false;
        });
    }

    public void SetAchado(bool achado)
    {
        isAchado = achado;
        if (achado)
        {
            isSelected = true;
        }
    }
}