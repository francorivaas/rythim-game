using UnityEngine;
using TMPro;

public class RhythmNoteView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private TMP_Text numberText;

    [Header("Optional Colors")]
    [SerializeField] private UnityEngine.UI.Image background;

    [SerializeField] private Color attack1Color = Color.red;
    [SerializeField] private Color attack2Color = Color.blue;
    [SerializeField] private Color attack3Color = Color.green;
    [SerializeField] private Color attack4Color = Color.yellow;

    public void Setup(AttackType attackType)
    {
        numberText.text =
            ((int)attackType).ToString();

        if (background == null)
            return;

        switch (attackType)
        {
            case AttackType.Attack1:
                background.color =
                    attack1Color;
                break;

            case AttackType.Attack2:
                background.color =
                    attack2Color;
                break;

            case AttackType.Attack3:
                background.color =
                    attack3Color;
                break;

            case AttackType.Attack4:
                background.color =
                    attack4Color;
                break;
        }
    }

    public void SetPosition(
        Vector2 start,
        Vector2 end,
        float progress
    )
    {
        rectTransform.anchoredPosition =
            Vector2.LerpUnclamped(
                start,
                end,
                progress
            );
    }
}