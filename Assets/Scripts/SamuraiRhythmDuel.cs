using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public enum AttackType
{
    Attack1 = 1,
    Attack2 = 2,
    Attack3 = 3,
    Attack4 = 4
}

public class SamuraiRhythmDuel : MonoBehaviour
{
    private enum DuelState
    {
        Countdown,
        Playing,
        Won,
        Lost
    }

    private class NoteData
    {
        public float targetTime;
        public AttackType attackType;
        public RhythmNoteView view;
    }

    // =====================================================
    // HEALTH
    // =====================================================

    [Header("Health")]
    [SerializeField] private float playerMaxHealth = 100f;
    [SerializeField] private float enemyMaxHealth = 100f;

    [Header("Damage")]
    [SerializeField] private float goodDamage = 15f;
    [SerializeField] private float perfectDamage = 25f;

    [SerializeField] private float earlyDamageToPlayer = 15f;
    [SerializeField] private float wrongDamageToPlayer = 15f;
    [SerializeField] private float missDamageToPlayer = 10f;

    // =====================================================
    // RHYTHM
    // =====================================================

    [Header("Note Generation")]

    [Tooltip("Tiempo antes de la primera nota.")]
    [SerializeField] private float firstNoteDelay = 2f;

    [Tooltip("Separación mínima aleatoria entre notas.")]
    [SerializeField] private float minNoteInterval = 0.45f;

    [Tooltip("Separación máxima aleatoria entre notas.")]
    [SerializeField] private float maxNoteInterval = 1.5f;

    [Tooltip("Cuánto tarda una nota en viajar desde Spawn hasta Hit.")]
    [SerializeField] private float noteTravelTime = 2f;

    [Tooltip("Cantidad de notas que mantenemos programadas.")]
    [SerializeField] private int notesAhead = 8;

    // =====================================================
    // PROGRESSIVE DIFFICULTY
    // =====================================================

    [Header("Progressive Difficulty")]

    [Tooltip("Cada cuántos enemigos derrotados aumenta la velocidad.")]
    [SerializeField] private int enemiesPerSpeedIncrease = 2;

    [Tooltip("Cuántos segundos se reduce el tiempo de viaje por cada aumento de dificultad.")]
    [SerializeField] private float travelTimeReductionPerTier = 0.15f;

    [Tooltip("Límite mínimo del tiempo de viaje. Cuanto menor, más rápida la nota.")]
    [SerializeField] private float minimumNoteTravelTime = 0.8f;

    [Tooltip("Opcional: muestra el número del enemigo actual.")]
    [SerializeField] private TMP_Text enemyCounterText;

    [Tooltip("Opcional: muestra el nivel de velocidad actual para debug/balance.")]
    [SerializeField] private TMP_Text difficultyText;

    // =====================================================
    // TIMING WINDOWS
    // =====================================================

    [Header("Timing Windows")]

    [Tooltip("Ventana PERFECT en segundos.")]
    [SerializeField] private float perfectWindow = 0.07f;

    [Tooltip("Ventana GOOD en segundos.")]
    [SerializeField] private float goodWindow = 0.16f;

    // =====================================================
    // RHYTHM UI
    // =====================================================

    [Header("Rhythm Lane")]

    [SerializeField] private RectTransform notesContainer;

    [Tooltip("Lugar donde aparecen las notas.")]
    [SerializeField] private RectTransform spawnPoint;

    [Tooltip("Lugar exacto donde debemos tocar.")]
    [SerializeField] private RectTransform hitPoint;

    [SerializeField] private RhythmNoteView notePrefab;

    // =====================================================
    // HEALTH UI
    // =====================================================

    [Header("Health UI")]

    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private Slider enemyHealthSlider;

    //[SerializeField] private TMP_Text playerHealthText;
    //[SerializeField] private TMP_Text enemyHealthText;

    // =====================================================
    // FEEDBACK
    // =====================================================

    [Header("Feedback")]

    [SerializeField] private TMP_Text feedbackText;

    [SerializeField] private float feedbackDuration = 0.5f;

    // =====================================================
    // COUNTDOWN
    // =====================================================

    [Header("Countdown")]

    [Tooltip("Texto grande para mostrar 3, 2, 1, ¡YA!")]
    [SerializeField] private TMP_Text countdownText;

    [Tooltip("Duración de cada número de la cuenta regresiva.")]
    [SerializeField] private float countdownStepDuration = 0.8f;

    [Tooltip("Tiempo que permanece ¡YA! antes de comenzar a mover las notas.")]
    [SerializeField] private float goDuration = 0.45f;

    // =====================================================
    // ROUND
    // =====================================================

    [Header("Round")]

    [SerializeField] private bool autoRestart = true;
    [SerializeField] private float restartDelay = 2f;

    // =====================================================
    // DEBUG INPUT
    // =====================================================

    [Header("PC Debug")]

    [Tooltip("Permite usar 1,2,3,4 en teclado.")]
    [SerializeField] private bool enableKeyboardInput = true;

    // =====================================================
    // PRIVATE
    // =====================================================

    private float playerHealth;
    private float enemyHealth;

    private float duelTimer;
    private float restartTimer;
    private float feedbackTimer;

    private float countdownTimer;
    private int countdownValue;

    private int enemiesDefeated;
    private float currentNoteTravelTime;

    private DuelState currentState;

    private readonly List<NoteData> notes =
        new List<NoteData>();

    // =====================================================
    // UNITY
    // =====================================================

    private void Start()
    {
        enemiesDefeated = 0;
        StartDuel();
    }

    private void Update()
    {
        UpdateFeedback();

        switch (currentState)
        {
            case DuelState.Countdown:
                UpdateCountdown();
                break;

            case DuelState.Playing:
                duelTimer += Time.deltaTime;

                UpdateNotes();

                // Damos prioridad al input del jugador antes de marcar MISS.
                if (enableKeyboardInput)
                {
                    ReadKeyboardInput();
                }

                ProcessMissedNotes();
                break;

            case DuelState.Won:
            case DuelState.Lost:
                UpdateFinishedDuel();
                break;
        }
    }

    // =====================================================
    // START DUEL
    // =====================================================

    private void StartDuel()
    {
        ClearNotes();

        playerHealth = playerMaxHealth;
        enemyHealth = enemyMaxHealth;

        duelTimer = 0f;

        UpdateDifficulty();
        UpdateHealthUI();
        UpdateProgressUI();

        if (feedbackText != null)
        {
            feedbackText.text = "";
        }

        feedbackTimer = 0f;

        BeginCountdown();
    }

    // =====================================================
    // COUNTDOWN
    // =====================================================

    private void BeginCountdown()
    {
        currentState = DuelState.Countdown;

        countdownValue = 3;
        countdownTimer = countdownStepDuration;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "3";
        }
    }

    private void UpdateCountdown()
    {
        countdownTimer -= Time.deltaTime;

        if (countdownTimer > 0f)
            return;

        if (countdownValue > 1)
        {
            countdownValue--;
            countdownTimer = countdownStepDuration;

            if (countdownText != null)
            {
                countdownText.text = countdownValue.ToString();
            }

            return;
        }

        if (countdownValue == 1)
        {
            countdownValue = 0;
            countdownTimer = goDuration;

            if (countdownText != null)
            {
                countdownText.text = "¡YA!";
            }

            return;
        }

        StartGameplayAfterCountdown();
    }

    private void StartGameplayAfterCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        duelTimer = 0f;
        currentState = DuelState.Playing;

        GenerateInitialNotes();
    }

    // =====================================================
    // PROGRESSIVE DIFFICULTY
    // =====================================================

    private void UpdateDifficulty()
    {
        int safeEnemiesPerIncrease =
            Mathf.Max(1, enemiesPerSpeedIncrease);

        int difficultyTier =
            enemiesDefeated / safeEnemiesPerIncrease;

        currentNoteTravelTime = Mathf.Max(
            minimumNoteTravelTime,
            noteTravelTime -
            (difficultyTier * travelTimeReductionPerTier)
        );
    }

    private void UpdateProgressUI()
    {
        if (enemyCounterText != null)
        {
            enemyCounterText.text =
                $"ENEMIGO {enemiesDefeated + 1}";
        }

        if (difficultyText != null)
        {
            int safeEnemiesPerIncrease =
                Mathf.Max(1, enemiesPerSpeedIncrease);

            int difficultyTier =
                enemiesDefeated / safeEnemiesPerIncrease;

            difficultyText.text =
                $"VEL. {difficultyTier + 1}  |  {currentNoteTravelTime:0.00}s";
        }
    }

    // =====================================================
    // NOTE GENERATION
    // =====================================================

    private void GenerateInitialNotes()
    {
        float nextTime = firstNoteDelay;

        for (int i = 0; i < notesAhead; i++)
        {
            CreateScheduledNote(nextTime);

            nextTime += GetRandomInterval();
        }
    }

    private void GenerateNextNote()
    {
        float newTime;

        if (notes.Count == 0)
        {
            newTime =
                duelTimer +
                currentNoteTravelTime +
                GetRandomInterval();
        }
        else
        {
            float lastTime =
                notes[notes.Count - 1].targetTime;

            newTime =
                lastTime +
                GetRandomInterval();
        }

        CreateScheduledNote(newTime);
    }

    private void CreateScheduledNote(float targetTime)
    {
        NoteData note =
            new NoteData();

        note.targetTime = targetTime;

        note.attackType =
            (AttackType)Random.Range(1, 5);

        note.view = null;

        notes.Add(note);
    }

    private float GetRandomInterval()
    {
        return Random.Range(
            minNoteInterval,
            maxNoteInterval
        );
    }

    // =====================================================
    // NOTE UPDATE
    // =====================================================

    private void UpdateNotes()
    {
        foreach (NoteData note in notes)
        {
            float timeUntilHit =
                note.targetTime - duelTimer;

            // La nota todavía está demasiado lejos.
            if (
                note.view == null &&
                timeUntilHit <= currentNoteTravelTime
            )
            {
                SpawnNoteView(note);
            }

            if (note.view == null)
                continue;

            float progress =
                1f -
                (timeUntilHit / currentNoteTravelTime);

            note.view.SetPosition(
                spawnPoint.anchoredPosition,
                hitPoint.anchoredPosition,
                progress
            );
        }
    }

    private void SpawnNoteView(NoteData note)
    {
        RhythmNoteView newView =
            Instantiate(
                notePrefab,
                notesContainer
            );

        newView.Setup(
            note.attackType
        );

        note.view = newView;
    }

    // =====================================================
    // PLAYER INPUT
    // =====================================================

    public void PressAttack(AttackType attack)
    {
        if (currentState != DuelState.Playing)
            return;

        if (notes.Count == 0)
            return;

        EvaluateAttack(attack);
    }

    private void EvaluateAttack(AttackType pressedAttack)
    {
        NoteData currentNote = notes[0];

        float timingOffset =
            duelTimer -
            currentNote.targetTime;

        float absoluteOffset =
            Mathf.Abs(timingOffset);

        // -------------------------------------
        // TOO EARLY
        // -------------------------------------

        if (timingOffset < -goodWindow)
        {
            DamagePlayer(
                earlyDamageToPlayer
            );

            ShowFeedback("EARLY!");

            ConsumeCurrentNote();

            return;
        }

        // -------------------------------------
        // WRONG BUTTON
        // -------------------------------------

        if (
            pressedAttack !=
            currentNote.attackType
        )
        {
            DamagePlayer(
                wrongDamageToPlayer
            );

            ShowFeedback("WRONG!");

            ConsumeCurrentNote();

            return;
        }

        // -------------------------------------
        // PERFECT
        // -------------------------------------

        if (
            absoluteOffset <=
            perfectWindow
        )
        {
            DamageEnemy(
                perfectDamage
            );

            ShowFeedback(
                "PERFECT!"
            );

            ConsumeCurrentNote();

            return;
        }

        // -------------------------------------
        // GOOD
        // -------------------------------------

        if (
            absoluteOffset <=
            goodWindow
        )
        {
            DamageEnemy(
                goodDamage
            );

            ShowFeedback(
                "GOOD!"
            );

            ConsumeCurrentNote();

            return;
        }

        // -------------------------------------
        // TOO LATE
        // -------------------------------------

        DamagePlayer(
            missDamageToPlayer
        );

        ShowFeedback("MISS!");

        ConsumeCurrentNote();
    }

    // =====================================================
    // MISSED NOTES
    // =====================================================

    private void ProcessMissedNotes()
    {
        while (
            notes.Count > 0 &&
            duelTimer >
            notes[0].targetTime +
            goodWindow &&
            currentState ==
            DuelState.Playing
        )
        {
            DamagePlayer(
                missDamageToPlayer
            );

            ShowFeedback("MISS!");

            ConsumeCurrentNote();
        }
    }

    // =====================================================
    // CONSUME NOTE
    // =====================================================

    private void ConsumeCurrentNote()
    {
        if (notes.Count == 0)
            return;

        NoteData note =
            notes[0];

        if (note.view != null)
        {
            Destroy(
                note.view.gameObject
            );
        }

        notes.RemoveAt(0);

        if (
            currentState ==
            DuelState.Playing
        )
        {
            GenerateNextNote();
        }
    }

    // =====================================================
    // DAMAGE
    // =====================================================

    private void DamageEnemy(float damage)
    {
        enemyHealth -= damage;

        enemyHealth =
            Mathf.Max(
                enemyHealth,
                0f
            );

        UpdateHealthUI();

        if (enemyHealth <= 0f)
        {
            WinDuel();
        }
    }

    private void DamagePlayer(float damage)
    {
        playerHealth -= damage;

        playerHealth =
            Mathf.Max(
                playerHealth,
                0f
            );

        UpdateHealthUI();

        if (playerHealth <= 0f)
        {
            LoseDuel();
        }
    }

    // =====================================================
    // HEALTH UI
    // =====================================================

    private void UpdateHealthUI()
    {
        playerHealthSlider.maxValue =
            playerMaxHealth;

        playerHealthSlider.value =
            playerHealth;

        enemyHealthSlider.maxValue =
            enemyMaxHealth;

        enemyHealthSlider.value =
            enemyHealth;

        //if (playerHealthText != null)
        //{
        //    playerHealthText.text =
        //        $"{playerHealth:0}";
        //}

        //if (enemyHealthText != null)
        //{
        //    enemyHealthText.text =
        //        $"{enemyHealth:0}";
        //}
    }

    // =====================================================
    // RESULT
    // =====================================================

    private void WinDuel()
    {
        // Solo una victoria real hace avanzar la dificultad.
        enemiesDefeated++;

        currentState =
            DuelState.Won;

        restartTimer =
            restartDelay;

        ShowFeedback(
            "VICTORY!"
        );
    }

    private void LoseDuel()
    {
        currentState =
            DuelState.Lost;

        restartTimer =
            restartDelay;

        ShowFeedback(
            "DEFEAT!"
        );
    }

    private void UpdateFinishedDuel()
    {
        if (!autoRestart)
            return;

        restartTimer -=
            Time.deltaTime;

        if (restartTimer <= 0f)
        {
            StartDuel();
        }
    }

    // =====================================================
    // FEEDBACK
    // =====================================================

    private void ShowFeedback(
        string message
    )
    {
        if (feedbackText == null)
            return;

        feedbackText.text =
            message;

        feedbackTimer =
            feedbackDuration;
    }

    private void UpdateFeedback()
    {
        if (feedbackTimer <= 0f)
            return;

        feedbackTimer -=
            Time.deltaTime;

        if (feedbackTimer <= 0f)
        {
            feedbackText.text = "";
        }
    }

    // =====================================================
    // KEYBOARD DEBUG
    // =====================================================

    private void ReadKeyboardInput()
    {
        if (Keyboard.current == null)
            return;

        if (
            Keyboard.current
            .digit1Key
            .wasPressedThisFrame
        )
        {
            PressAttack(
                AttackType.Attack1
            );
        }

        if (
            Keyboard.current
            .digit2Key
            .wasPressedThisFrame
        )
        {
            PressAttack(
                AttackType.Attack2
            );
        }

        if (
            Keyboard.current
            .digit3Key
            .wasPressedThisFrame
        )
        {
            PressAttack(
                AttackType.Attack3
            );
        }

        if (
            Keyboard.current
            .digit4Key
            .wasPressedThisFrame
        )
        {
            PressAttack(
                AttackType.Attack4
            );
        }
    }

    // =====================================================
    // CLEAR
    // =====================================================

    private void ClearNotes()
    {
        foreach (NoteData note in notes)
        {
            if (note.view != null)
            {
                Destroy(
                    note.view.gameObject
                );
            }
        }

        notes.Clear();
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    private void OnValidate()
    {
        playerMaxHealth =
            Mathf.Max(
                1f,
                playerMaxHealth
            );

        enemyMaxHealth =
            Mathf.Max(
                1f,
                enemyMaxHealth
            );

        perfectWindow =
            Mathf.Max(
                0.01f,
                perfectWindow
            );

        goodWindow =
            Mathf.Max(
                perfectWindow,
                goodWindow
            );

        minNoteInterval =
            Mathf.Max(
                goodWindow * 2.2f,
                minNoteInterval
            );

        maxNoteInterval =
            Mathf.Max(
                minNoteInterval,
                maxNoteInterval
            );

        noteTravelTime =
            Mathf.Max(
                0.1f,
                noteTravelTime
            );

        notesAhead =
            Mathf.Max(
                2,
                notesAhead
            );

        enemiesPerSpeedIncrease =
            Mathf.Max(1, enemiesPerSpeedIncrease);

        travelTimeReductionPerTier =
            Mathf.Max(0f, travelTimeReductionPerTier);

        minimumNoteTravelTime =
            Mathf.Clamp(
                minimumNoteTravelTime,
                0.1f,
                noteTravelTime
            );

        countdownStepDuration =
            Mathf.Max(0.05f, countdownStepDuration);

        goDuration =
            Mathf.Max(0.05f, goDuration);
    }
}