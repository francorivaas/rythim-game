using UnityEngine;

public class AttackButtonUI : MonoBehaviour
{
    [SerializeField]
    private SamuraiRhythmDuel duelManager;

    [SerializeField]
    private AttackType attackType;

    public void Press()
    {
        duelManager.PressAttack(
            attackType
        );
    }
}