using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

public class Card : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    // Sprite que aparece quando a carta está virada para baixo (escondida)
    public Sprite hiddenIconSprite;

    // Sprite que aparece quando a carta está virada para cima (ícone real)
    public Sprite iconSprite;

    // Estado da carta
    public bool isSelected; // se está virada pra cima no momento
    public bool isAchado;   // se a carta já foi encontrada (par)

    // Identificadores para o controle do jogo
    public int cardId;  // ID único da carta (pode ser usado para identificar par)
    public int index;   // índice da carta dentro do controller (posição)

    // Referência para o controlador geral das cartas
    public CardController controller;

    // Método chamado ao clicar na carta (vincule pelo botão ou event trigger)
    public void OnCardClick()
    {
        // Só pode selecionar se não estiver selecionada e nem já achada
        if (!isSelected && !isAchado)
        {
            controller.SetSelected(this);
        }
    }

    // Define o sprite do ícone (quando virar a carta)
    public void SetIconSprite(Sprite sp)
    {
        iconSprite = sp;
    }

    // Mostrar carta (vira para cima com animação)
    public void Show()
    {
        Tween.Rotation(transform, new Vector3(0f, 180f, 0f), 0.2f);

        Tween.Delay(0.1f, () =>
        {
            iconImage.sprite = iconSprite;
            isSelected = true;
        });
    }

    // Esconder carta (vira para baixo com animação)
    public void Hide()
    {
        Tween.Rotation(transform, new Vector3(0f, 0f, 0f), 0.2f);

        Tween.Delay(0.1f, () =>
        {
            iconImage.sprite = hiddenIconSprite;
            isSelected = false;
        });
    }

    // Marca a carta como encontrada ou não
    public void SetAchado(bool achado)
    {
        isAchado = achado;
        if (achado)
        {
            isSelected = true; // mantém a carta virada pra cima
        }
    }
}
