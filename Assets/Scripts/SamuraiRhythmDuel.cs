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

<<<<<<< Updated upstream
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
    // TIMING WINDOWS
    // =====================================================

    [Header("Timing Windows")]

    [Tooltip("Ventana PERFECT en segundos.")]
=======
    [Header("Enemy 1 - Single Notes")]
    [SerializeField] private float firstNoteDelay = 0.65f;
    [SerializeField] private float minSingleNotePause = 0.45f;
    [SerializeField] private float maxSingleNotePause = 0.90f;

    [Header("Bursts - Enemy 2+")]
    [SerializeField] private int burstStartEnemy = 2;
    [SerializeField] private int minNotesPerBurst = 2;
    [SerializeField] private int maxNotesPerBurst = 4;
    [SerializeField] private float minBurstNoteInterval = 0.38f;
    [SerializeField] private float maxBurstNoteInterval = 0.48f;
    [SerializeField] private float minimumBurstReactionTime = 0.75f;
    [SerializeField] private float minPauseBetweenBursts = 0.45f;
    [SerializeField] private float maxPauseBetweenBursts = 0.85f;

    [Header("Progressive Difficulty")]
    [SerializeField] private float noteTravelTime = 2.60f;
    [SerializeField] private int enemiesPerSpeedIncrease = 2;
    [SerializeField] private float travelTimeReductionPerTier = 0.12f;
    [SerializeField] private float minimumNoteTravelTime = 2.00f;
    [SerializeField] private TMP_Text enemyCounterText;
    [SerializeField] private TMP_Text difficultyText;

    [Header("Timing Windows")]
>>>>>>> Stashed changes
    [SerializeField] private float perfectWindow = 0.07f;
    [SerializeField] private float goodWindow = 0.16f;

    // =====================================================
    // RHYTHM UI
    // =====================================================

    [Header("Rhythm Lane")]

    [SerializeField] private RectTransform notesContainer;
    [SerializeField] private RectTransform spawnPoint;
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

<<<<<<< Updated upstream
    // =====================================================
    // ROUND
    // =====================================================
=======
    [Header("Countdown")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float countdownStepDuration = 0.8f;
    [SerializeField] private float goDuration = 0.45f;
>>>>>>> Stashed changes

    [Header("Juiciness")]
    [Tooltip("Controlador opcional de feedback visual. Si queda vacío, el gameplay sigue funcionando.")]
    [SerializeField] private RhythmJuiceController juiceController;

    [Header("Round")]

    [SerializeField] private bool autoRestart = true;
    [SerializeField] private float restartDelay = 2f;

    // =====================================================
    // DEBUG INPUT
    // =====================================================

    [Header("PC Debug")]
<<<<<<< Updated upstream

    [Tooltip("Permite usar 1,2,3,4 en teclado.")]
=======
>>>>>>> Stashed changes
    [SerializeField] private bool enableKeyboardInput = true;

    // =====================================================
    // PRIVATE
    // =====================================================

    private float playerHealth;
    private float enemyHealth;

    private float duelTimer;
    private float restartTimer;
    private float feedbackTimer;

<<<<<<< Updated upstream
=======
    private float countdownTimer;
    private int countdownValue;

    private int enemiesDefeated;
    private float currentNoteTravelTime;
    private float nextGroupSpawnTime;

>>>>>>> Stashed changes
    private DuelState currentState;

    private readonly List<NoteData> notes = new List<NoteData>();

    // =====================================================
    // UNITY
    // =====================================================

    private void Start()
    {
        StartDuel();
    }

    private void Update()
    {
        UpdateFeedback();

        if (currentState == DuelState.Playing)
        {
            duelTimer += Time.deltaTime;

            UpdateNotes();
            ProcessMissedNotes();

<<<<<<< Updated upstream
            if (enableKeyboardInput)
            {
                ReadKeyboardInput();
            }
        }
        else
        {
            UpdateFinishedDuel();
=======
                UpdateGroupGenerator();
                UpdateNotes();

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
>>>>>>> Stashed changes
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

<<<<<<< Updated upstream
        currentState = DuelState.Playing;

        UpdateHealthUI();

        feedbackText.text = "";
        feedbackTimer = 0f;

        GenerateInitialNotes();
=======
        UpdateDifficulty();
        SetHealthUIImmediate();
        UpdateProgressUI();

        juiceController?.ResetVisuals();

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
        nextGroupSpawnTime = firstNoteDelay;
>>>>>>> Stashed changes
    }

    // =====================================================
    // NOTE GENERATION
    // =====================================================

    private void GenerateInitialNotes()
    {
<<<<<<< Updated upstream
        float nextTime = firstNoteDelay;

        for (int i = 0; i < notesAhead; i++)
        {
            CreateScheduledNote(nextTime);

            nextTime += GetRandomInterval();
        }
    }

    private void GenerateNextNote()
=======
        int safeEnemiesPerIncrease = Mathf.Max(1, enemiesPerSpeedIncrease);
        int difficultyTier = enemiesDefeated / safeEnemiesPerIncrease;

        currentNoteTravelTime = Mathf.Max(
            minimumNoteTravelTime,
            noteTravelTime - (difficultyTier * travelTimeReductionPerTier)
        );
    }

    private void UpdateProgressUI()
    {
        int currentEnemy = enemiesDefeated + 1;

        if (enemyCounterText != null)
        {
            enemyCounterText.text = $"ENEMIGO {currentEnemy}";
        }

        if (difficultyText != null)
        {
            int safeEnemiesPerIncrease = Mathf.Max(1, enemiesPerSpeedIncrease);
            int difficultyTier = enemiesDefeated / safeEnemiesPerIncrease;

            string generationMode =
                currentEnemy >= burstStartEnemy
                    ? $"RÁFAGA {minNotesPerBurst}-{maxNotesPerBurst}"
                    : "SIMPLE";

            difficultyText.text =
                $"VEL. {difficultyTier + 1} | {currentNoteTravelTime:0.00}s | {generationMode}";
        }
    }

    // =====================================================
    // NOTE GROUP GENERATION
    // =====================================================

    private void UpdateGroupGenerator()
>>>>>>> Stashed changes
    {
        float newTime;

        if (notes.Count == 0)
        {
            newTime =
                duelTimer +
                noteTravelTime +
                GetRandomInterval();
        }
        else
        {
            float lastTime =
                notes[notes.Count - 1].targetTime;

<<<<<<< Updated upstream
            newTime =
                lastTime +
                GetRandomInterval();
        }

        CreateScheduledNote(newTime);
=======
    private void SpawnSingleNoteGroup()
    {
        float targetTime = duelTimer + currentNoteTravelTime;

        CreateAndSpawnNote(targetTime);

        float pause = Random.Range(
            minSingleNotePause,
            maxSingleNotePause
        );

        nextGroupSpawnTime = targetTime + pause;
    }

    private void SpawnBurstGroup()
    {
        int amount = Random.Range(
            minNotesPerBurst,
            maxNotesPerBurst + 1
        );

        amount = Mathf.Clamp(amount, 2, 4);

        float configuredInterval = Random.Range(
            minBurstNoteInterval,
            maxBurstNoteInterval
        );

        float maxSafeInterval = configuredInterval;

        if (amount > 1)
        {
            maxSafeInterval =
                (currentNoteTravelTime - minimumBurstReactionTime) /
                (amount - 1);

            maxSafeInterval = Mathf.Max(0.12f, maxSafeInterval);
        }

        float burstInterval = Mathf.Min(
            configuredInterval,
            maxSafeInterval
        );

        for (int i = 0; i < amount; i++)
        {
            int notesAfterThis = amount - 1 - i;

            float targetTime =
                duelTimer +
                currentNoteTravelTime -
                (notesAfterThis * burstInterval);

            CreateAndSpawnNote(targetTime);
        }

        float lastTargetTime = duelTimer + currentNoteTravelTime;

        float pause = Random.Range(
            minPauseBetweenBursts,
            maxPauseBetweenBursts
        );

        nextGroupSpawnTime = lastTargetTime + pause;
>>>>>>> Stashed changes
    }

    private void CreateScheduledNote(float targetTime)
    {
        NoteData note = new NoteData
        {
            targetTime = targetTime,
            attackType = (AttackType)Random.Range(1, 5)
        };

<<<<<<< Updated upstream
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
                timeUntilHit <= noteTravelTime
            )
            {
                SpawnNoteView(note);
            }

            if (note.view == null)
                continue;

            float progress =
                1f -
                (timeUntilHit / noteTravelTime);

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
=======
        RhythmNoteView newView = Instantiate(
            notePrefab,
            notesContainer
>>>>>>> Stashed changes
        );

        newView.Setup(note.attackType);

        note.view = newView;
<<<<<<< Updated upstream
=======

        notes.Add(note);

        notes.Sort(
            (a, b) => a.targetTime.CompareTo(b.targetTime)
        );

        UpdateSingleNotePosition(note);
    }

    private void UpdateNotes()
    {
        for (int i = 0; i < notes.Count; i++)
        {
            UpdateSingleNotePosition(notes[i]);
        }
    }

    private void UpdateSingleNotePosition(NoteData note)
    {
        if (note.view == null)
            return;

        float timeUntilHit = note.targetTime - duelTimer;

        float progress =
            1f -
            (timeUntilHit / currentNoteTravelTime);

        note.view.SetPosition(
            spawnPoint.anchoredPosition,
            hitPoint.anchoredPosition,
            progress
        );
>>>>>>> Stashed changes
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

<<<<<<< Updated upstream
        // -------------------------------------
        // TOO EARLY
        // -------------------------------------

=======
        // EARLY
>>>>>>> Stashed changes
        if (timingOffset < -goodWindow)
        {
            ConsumeCurrentNoteError();

            DamagePlayer(earlyDamageToPlayer);

            ShowFeedback("EARLY!");
<<<<<<< Updated upstream

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
=======
            juiceController?.PlayErrorFeedback();
            return;
        }

        // WRONG
        if (pressedAttack != currentNote.attackType)
>>>>>>> Stashed changes
        {
            ConsumeCurrentNoteError();

            DamagePlayer(wrongDamageToPlayer);

            ShowFeedback("WRONG!");
<<<<<<< Updated upstream

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
=======
            juiceController?.PlayErrorFeedback();
            return;
        }

        // PERFECT
        if (absoluteOffset <= perfectWindow)
>>>>>>> Stashed changes
        {
            ConsumeCurrentNoteSuccess(true);

            DamageEnemy(perfectDamage, true);

<<<<<<< Updated upstream
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
=======
            ShowFeedback("PERFECT!");
            juiceController?.PlayPerfectFeedback();
            return;
        }

        // GOOD
        if (absoluteOffset <= goodWindow)
>>>>>>> Stashed changes
        {
            ConsumeCurrentNoteSuccess(false);

            DamageEnemy(goodDamage, false);

<<<<<<< Updated upstream
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
=======
            ShowFeedback("GOOD!");
            juiceController?.PlayGoodFeedback();
            return;
        }

        // LATE
        ConsumeCurrentNoteError();

        DamagePlayer(missDamageToPlayer);

        ShowFeedback("MISS!");
        juiceController?.PlayErrorFeedback();
>>>>>>> Stashed changes
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
            ConsumeCurrentNoteError();

            DamagePlayer(missDamageToPlayer);

            ShowFeedback("MISS!");
<<<<<<< Updated upstream

            ConsumeCurrentNote();
        }
    }

    // =====================================================
    // CONSUME NOTE
    // =====================================================

    private void ConsumeCurrentNote()
=======
            juiceController?.PlayErrorFeedback();
        }
    }

    private void ConsumeCurrentNoteSuccess(bool perfect)
>>>>>>> Stashed changes
    {
        if (notes.Count == 0)
            return;

        NoteData note = notes[0];
        notes.RemoveAt(0);

        if (note.view != null)
        {
            note.view.PlaySuccessAndDestroy(perfect);
        }
    }

    private void ConsumeCurrentNoteError()
    {
        if (notes.Count == 0)
            return;

        NoteData note = notes[0];
        notes.RemoveAt(0);

<<<<<<< Updated upstream
        if (
            currentState ==
            DuelState.Playing
        )
        {
            GenerateNextNote();
=======
        if (note.view != null)
        {
            note.view.PlayErrorAndDestroy();
>>>>>>> Stashed changes
        }
    }

    // =====================================================
<<<<<<< Updated upstream
    // DAMAGE
=======
    // DAMAGE / HEALTH
>>>>>>> Stashed changes
    // =====================================================

    private void DamageEnemy(float damage, bool perfect)
    {
        enemyHealth -= damage;
        enemyHealth = Mathf.Max(enemyHealth, 0f);

        if (juiceController != null)
        {
            juiceController.AnimateHealth(
                enemyHealthSlider,
                enemyHealth
            );

            juiceController.PlayEnemyDamage(
                damage,
                perfect
            );
        }
        else
        {
            SetSliderValue(enemyHealthSlider, enemyHealth);
        }

        if (enemyHealth <= 0f)
        {
            WinDuel();
        }
    }

    private void DamagePlayer(float damage)
    {
        playerHealth -= damage;
        playerHealth = Mathf.Max(playerHealth, 0f);

        if (juiceController != null)
        {
            juiceController.AnimateHealth(
                playerHealthSlider,
                playerHealth
            );

            juiceController.PlayPlayerDamage(
                damage
            );
        }
        else
        {
            SetSliderValue(playerHealthSlider, playerHealth);
        }

        if (playerHealth <= 0f)
        {
            LoseDuel();
        }
    }

<<<<<<< Updated upstream
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
=======
    private void SetHealthUIImmediate()
    {
        if (playerHealthSlider != null)
        {
            playerHealthSlider.maxValue = playerMaxHealth;
        }

        if (enemyHealthSlider != null)
        {
            enemyHealthSlider.maxValue = enemyMaxHealth;
        }

        if (juiceController != null)
        {
            juiceController.SetHealthImmediate(
                playerHealthSlider,
                playerHealth
            );

            juiceController.SetHealthImmediate(
                enemyHealthSlider,
                enemyHealth
            );
        }
        else
        {
            SetSliderValue(playerHealthSlider, playerHealth);
            SetSliderValue(enemyHealthSlider, enemyHealth);
        }
    }

    private void SetSliderValue(Slider slider, float value)
    {
        if (slider != null)
        {
            slider.value = value;
        }
    }

    // =====================================================
    // RESULTS
>>>>>>> Stashed changes
    // =====================================================

    private void WinDuel()
    {
<<<<<<< Updated upstream
        currentState =
            DuelState.Won;

        restartTimer =
            restartDelay;
=======
        enemiesDefeated++;

        currentState = DuelState.Won;
        restartTimer = restartDelay;
>>>>>>> Stashed changes

        ShowFeedback(
            "VICTORY!"
        );
    }

    private void LoseDuel()
    {
        currentState = DuelState.Lost;
        restartTimer = restartDelay;

        ShowFeedback(
            "DEFEAT!"
        );
    }

    private void UpdateFinishedDuel()
    {
        if (!autoRestart)
            return;

        restartTimer -= Time.deltaTime;

        if (restartTimer <= 0f)
        {
            StartDuel();
        }
    }

    // =====================================================
    // FEEDBACK TEXT
    // =====================================================

    private void ShowFeedback(string message)
    {
        if (feedbackText == null)
            return;

        feedbackText.text = message;
        feedbackTimer = feedbackDuration;
    }

    private void UpdateFeedback()
    {
        if (feedbackTimer <= 0f)
            return;

        feedbackTimer -= Time.deltaTime;

        if (feedbackTimer <= 0f && feedbackText != null)
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

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            PressAttack(AttackType.Attack1);
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            PressAttack(AttackType.Attack2);
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            PressAttack(AttackType.Attack3);
        }

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            PressAttack(AttackType.Attack4);
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
                Destroy(note.view.gameObject);
            }
        }

        notes.Clear();
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    private void OnValidate()
    {
        playerMaxHealth = Mathf.Max(1f, playerMaxHealth);
        enemyMaxHealth = Mathf.Max(1f, enemyMaxHealth);

        perfectWindow = Mathf.Max(0.01f, perfectWindow);
        goodWindow = Mathf.Max(perfectWindow, goodWindow);

        firstNoteDelay = Mathf.Max(0f, firstNoteDelay);

        minSingleNotePause = Mathf.Max(0f, minSingleNotePause);
        maxSingleNotePause = Mathf.Max(
            minSingleNotePause,
            maxSingleNotePause
        );

<<<<<<< Updated upstream
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
=======
        burstStartEnemy = Mathf.Max(2, burstStartEnemy);

        minNotesPerBurst = Mathf.Clamp(
            minNotesPerBurst,
            2,
            4
        );

        maxNotesPerBurst = Mathf.Clamp(
            maxNotesPerBurst,
            minNotesPerBurst,
            4
        );

        minBurstNoteInterval = Mathf.Max(
            0.12f,
            minBurstNoteInterval
        );

        maxBurstNoteInterval = Mathf.Max(
            minBurstNoteInterval,
            maxBurstNoteInterval
        );

        minimumBurstReactionTime = Mathf.Max(
            goodWindow + 0.1f,
            minimumBurstReactionTime
        );

        minPauseBetweenBursts = Mathf.Max(
            0f,
            minPauseBetweenBursts
        );

        maxPauseBetweenBursts = Mathf.Max(
            minPauseBetweenBursts,
            maxPauseBetweenBursts
        );

        noteTravelTime = Mathf.Max(
            0.5f,
            noteTravelTime
        );

        enemiesPerSpeedIncrease = Mathf.Max(
            1,
            enemiesPerSpeedIncrease
        );

        travelTimeReductionPerTier = Mathf.Max(
            0f,
            travelTimeReductionPerTier
        );

        minimumNoteTravelTime = Mathf.Clamp(
            minimumNoteTravelTime,
            0.5f,
            noteTravelTime
        );

        countdownStepDuration = Mathf.Max(
            0.05f,
            countdownStepDuration
        );

        goDuration = Mathf.Max(
            0.05f,
            goDuration
        );
>>>>>>> Stashed changes
    }
}
