using UnityEngine;
using UnityEngine.InputSystem;

public class SamuraiDuelPrototype : MonoBehaviour
{
    private enum DuelState
    {
        Waiting,
        Drawing,
        Resolved
    }

    private enum DuelResult
    {
        None,
        Early,
        Good,
        Perfect,
        Late
    }

    [Header("Waiting Phase")]
    [SerializeField] private float minWaitTime = 1.5f;
    [SerializeField] private float maxWaitTime = 4.0f;

    [Header("Timing")]
    [SerializeField] private float cursorTravelTime = 1.2f;

    [Range(0f, 1f)]
    [SerializeField] private float targetPosition = 0.70f;

    [Range(0.005f, 0.25f)]
    [SerializeField] private float perfectHalfWidth = 0.035f;

    [Range(0.01f, 0.4f)]
    [SerializeField] private float goodHalfWidth = 0.10f;

    [Header("Round")]
    [SerializeField] private float restartDelay = 1.5f;

    [Header("Input")]
    [SerializeField] private bool allowSpaceKey = true;
    [SerializeField] private bool allowMouseClick = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;
    [SerializeField] private bool showNumericCursorPosition = false;

    private DuelState currentState;
    private DuelResult currentResult;

    private float waitTimer;
    private float timingTimer;
    private float restartTimer;

    private float cursorPosition;

    private void Start()
    {
        StartNewRound();
    }

    private void Update()
    {
        bool attackPressed = WasAttackPressed();

        switch (currentState)
        {
            case DuelState.Waiting:
                UpdateWaitingState(attackPressed);
                break;

            case DuelState.Drawing:
                UpdateDrawingState(attackPressed);
                break;

            case DuelState.Resolved:
                UpdateResolvedState();
                break;
        }
    }

    private void UpdateWaitingState(bool attackPressed)
    {
        waitTimer -= Time.deltaTime;

        // Atacar antes de la señal.
        if (attackPressed)
        {
            ResolveDuel(DuelResult.Early);
            return;
        }

        if (waitTimer <= 0f)
        {
            BeginDrawPhase();
        }
    }

    private void UpdateDrawingState(bool attackPressed)
    {
        timingTimer += Time.deltaTime;

        cursorPosition = Mathf.Clamp01(
            timingTimer / cursorTravelTime
        );

        if (attackPressed)
        {
            EvaluateAttack();
            return;
        }

        float goodEnd = targetPosition + goodHalfWidth;

        // El jugador esperó demasiado.
        if (cursorPosition > goodEnd)
        {
            ResolveDuel(DuelResult.Late);
        }
    }

    private void UpdateResolvedState()
    {
        restartTimer -= Time.deltaTime;

        if (restartTimer <= 0f)
        {
            StartNewRound();
        }
    }

    private void BeginDrawPhase()
    {
        currentState = DuelState.Drawing;

        timingTimer = 0f;
        cursorPosition = 0f;

        Debug.Log("DRAW!");
    }

    private void EvaluateAttack()
    {
        float distanceFromTarget =
            Mathf.Abs(cursorPosition - targetPosition);

        if (distanceFromTarget <= perfectHalfWidth)
        {
            ResolveDuel(DuelResult.Perfect);
            return;
        }

        if (distanceFromTarget <= goodHalfWidth)
        {
            ResolveDuel(DuelResult.Good);
            return;
        }

        if (cursorPosition < targetPosition)
        {
            ResolveDuel(DuelResult.Early);
        }
        else
        {
            ResolveDuel(DuelResult.Late);
        }
    }

    private void ResolveDuel(DuelResult result)
    {
        currentState = DuelState.Resolved;
        currentResult = result;

        restartTimer = restartDelay;

        Debug.Log(
            $"RESULT: {result} | Cursor: {cursorPosition:F2}"
        );
    }

    private void StartNewRound()
    {
        currentState = DuelState.Waiting;
        currentResult = DuelResult.None;

        cursorPosition = 0f;
        timingTimer = 0f;

        waitTimer = Random.Range(
            minWaitTime,
            maxWaitTime
        );

        Debug.Log("WAIT...");
    }

    private bool WasAttackPressed()
    {
        bool keyboardPressed = false;
        bool mousePressed = false;

        if (allowSpaceKey &&
            Keyboard.current != null)
        {
            keyboardPressed =
                Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        if (allowMouseClick &&
            Mouse.current != null)
        {
            mousePressed =
                Mouse.current.leftButton.wasPressedThisFrame;
        }

        return keyboardPressed || mousePressed;
    }

    private void OnValidate()
    {
        if (maxWaitTime < minWaitTime)
        {
            maxWaitTime = minWaitTime;
        }

        if (cursorTravelTime <= 0f)
        {
            cursorTravelTime = 0.01f;
        }

        if (goodHalfWidth < perfectHalfWidth)
        {
            goodHalfWidth = perfectHalfWidth;
        }

        targetPosition = Mathf.Clamp(
            targetPosition,
            goodHalfWidth,
            1f - goodHalfWidth
        );
    }

    private void OnGUI()
    {
        if (!showDebugGUI)
            return;

        DrawStateText();

        if (currentState == DuelState.Drawing)
        {
            DrawTimingBar();
        }

        if (currentState == DuelState.Resolved)
        {
            DrawResult();
        }
    }

    private void DrawStateText()
    {
        GUIStyle stateStyle = new GUIStyle(GUI.skin.label);

        stateStyle.fontSize = 32;
        stateStyle.alignment = TextAnchor.MiddleCenter;

        string stateText = "";

        switch (currentState)
        {
            case DuelState.Waiting:
                stateText = "WAIT...";
                break;

            case DuelState.Drawing:
                stateText = "DRAW!";
                break;

            case DuelState.Resolved:
                stateText = "";
                break;
        }

        GUI.Label(
            new Rect(
                Screen.width / 2f - 200f,
                40f,
                400f,
                60f
            ),
            stateText,
            stateStyle
        );
    }

    private void DrawTimingBar()
    {
        float barWidth = Mathf.Min(
            800f,
            Screen.width - 100f
        );

        float barHeight = 40f;

        float barX =
            (Screen.width - barWidth) / 2f;

        float barY =
            Screen.height / 2f;

        Rect barRect = new Rect(
            barX,
            barY,
            barWidth,
            barHeight
        );

        // Barra completa.
        GUI.Box(barRect, "");

        float goodStart =
            Mathf.Clamp01(targetPosition - goodHalfWidth);

        float goodEnd =
            Mathf.Clamp01(targetPosition + goodHalfWidth);

        float perfectStart =
            Mathf.Clamp01(targetPosition - perfectHalfWidth);

        float perfectEnd =
            Mathf.Clamp01(targetPosition + perfectHalfWidth);

        // Zona GOOD.
        Rect goodRect = new Rect(
            barX + goodStart * barWidth,
            barY,
            (goodEnd - goodStart) * barWidth,
            barHeight
        );

        GUI.Box(goodRect, "GOOD");

        // Zona PERFECT.
        Rect perfectRect = new Rect(
            barX + perfectStart * barWidth,
            barY,
            (perfectEnd - perfectStart) * barWidth,
            barHeight
        );

        GUI.Box(perfectRect, "PERFECT");

        // Cursor.
        float cursorX =
            barX + cursorPosition * barWidth;

        Rect cursorRect = new Rect(
            cursorX - 3f,
            barY - 10f,
            6f,
            barHeight + 20f
        );

        GUI.Box(cursorRect, "");

        if (showNumericCursorPosition)
        {
            GUI.Label(
                new Rect(
                    barX,
                    barY + 50f,
                    barWidth,
                    30f
                ),
                $"Cursor: {cursorPosition:F3}"
            );
        }
    }

    private void DrawResult()
    {
        GUIStyle resultStyle =
            new GUIStyle(GUI.skin.label);

        resultStyle.fontSize = 42;
        resultStyle.alignment =
            TextAnchor.MiddleCenter;

        GUI.Label(
            new Rect(
                Screen.width / 2f - 250f,
                Screen.height / 2f - 50f,
                500f,
                100f
            ),
            currentResult.ToString().ToUpper(),
            resultStyle
        );
    }
}