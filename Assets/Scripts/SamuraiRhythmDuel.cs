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

    [Header("Health")]
    [SerializeField] private float playerMaxHealth = 100f;
    [SerializeField] private float enemyMaxHealth = 100f;

    [Header("Damage")]
    [SerializeField] private float goodDamage = 15f;
    [SerializeField] private float perfectDamage = 25f;
    [SerializeField] private float earlyDamageToPlayer = 15f;
    [SerializeField] private float wrongDamageToPlayer = 15f;
    [SerializeField] private float missDamageToPlayer = 10f;

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
    [SerializeField] private float perfectWindow = 0.07f;
    [SerializeField] private float goodWindow = 0.16f;

    [Header("Rhythm Lane")]
    [SerializeField] private RectTransform notesContainer;
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private RectTransform hitPoint;
    [SerializeField] private RhythmNoteView notePrefab;

    [Header("Health UI")]
    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private Slider enemyHealthSlider;

    [Header("Feedback")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float feedbackDuration = 0.5f;

    [Header("Countdown")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float countdownStepDuration = 0.8f;
    [SerializeField] private float goDuration = 0.45f;

    [Header("Juiciness")]
    [Tooltip("Controlador opcional de feedback visual. Si queda vacío, el gameplay sigue funcionando.")]
    [SerializeField] private RhythmJuiceController juiceController;

    [Header("Round")]
    [SerializeField] private bool autoRestart = true;
    [SerializeField] private float restartDelay = 2f;

    [Header("PC Debug")]
    [SerializeField] private bool enableKeyboardInput = true;

    private float playerHealth;
    private float enemyHealth;

    private float duelTimer;
    private float restartTimer;
    private float feedbackTimer;

    private float countdownTimer;
    private int countdownValue;

    private int enemiesDefeated;
    private float currentNoteTravelTime;
    private float nextGroupSpawnTime;

    private DuelState currentState;

    private readonly List<NoteData> notes = new List<NoteData>();

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
        }
    }

    private void StartDuel()
    {
        ClearNotes();

        playerHealth = playerMaxHealth;
        enemyHealth = enemyMaxHealth;

        duelTimer = 0f;
        nextGroupSpawnTime = 0f;

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
    }

    // =====================================================
    // DIFFICULTY
    // =====================================================

    private void UpdateDifficulty()
    {
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
    {
        if (duelTimer < nextGroupSpawnTime)
            return;

        int currentEnemy = enemiesDefeated + 1;

        if (currentEnemy < burstStartEnemy)
        {
            SpawnSingleNoteGroup();
        }
        else
        {
            SpawnBurstGroup();
        }
    }

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
    }

    // =====================================================
    // NOTE CREATION / MOVEMENT
    // =====================================================

    private void CreateAndSpawnNote(float targetTime)
    {
        NoteData note = new NoteData
        {
            targetTime = targetTime,
            attackType = (AttackType)Random.Range(1, 5)
        };

        RhythmNoteView newView = Instantiate(
            notePrefab,
            notesContainer
        );

        newView.Setup(note.attackType);

        note.view = newView;

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
    }

    // =====================================================
    // INPUT / TIMING
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

        // EARLY
        if (timingOffset < -goodWindow)
        {
            ConsumeCurrentNoteError();

            DamagePlayer(earlyDamageToPlayer);

            ShowFeedback("EARLY!");
            juiceController?.PlayErrorFeedback();
            return;
        }

        // WRONG
        if (pressedAttack != currentNote.attackType)
        {
            ConsumeCurrentNoteError();

            DamagePlayer(wrongDamageToPlayer);

            ShowFeedback("WRONG!");
            juiceController?.PlayErrorFeedback();
            return;
        }

        // PERFECT
        if (absoluteOffset <= perfectWindow)
        {
            ConsumeCurrentNoteSuccess(true);

            DamageEnemy(perfectDamage, true);

            ShowFeedback("PERFECT!");
            juiceController?.PlayPerfectFeedback();
            return;
        }

        // GOOD
        if (absoluteOffset <= goodWindow)
        {
            ConsumeCurrentNoteSuccess(false);

            DamageEnemy(goodDamage, false);

            ShowFeedback("GOOD!");
            juiceController?.PlayGoodFeedback();
            return;
        }

        // LATE
        ConsumeCurrentNoteError();

        DamagePlayer(missDamageToPlayer);

        ShowFeedback("MISS!");
        juiceController?.PlayErrorFeedback();
    }

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
            juiceController?.PlayErrorFeedback();
        }
    }

    private void ConsumeCurrentNoteSuccess(bool perfect)
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

        if (note.view != null)
        {
            note.view.PlayErrorAndDestroy();
        }
    }

    // =====================================================
    // DAMAGE / HEALTH
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
    // =====================================================

    private void WinDuel()
    {
        enemiesDefeated++;

        currentState = DuelState.Won;
        restartTimer = restartDelay;

        ShowFeedback("VICTORY!");
    }

    private void LoseDuel()
    {
        currentState = DuelState.Lost;
        restartTimer = restartDelay;

        ShowFeedback("DEFEAT!");
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
    // DEFAULT BALANCE
    // =====================================================

    [ContextMenu("Apply Recommended Balance Defaults")]
    private void ApplyRecommendedBalanceDefaults()
    {
        firstNoteDelay = 0.65f;
        minSingleNotePause = 0.45f;
        maxSingleNotePause = 0.90f;

        burstStartEnemy = 2;
        minNotesPerBurst = 2;
        maxNotesPerBurst = 4;

        minBurstNoteInterval = 0.38f;
        maxBurstNoteInterval = 0.48f;
        minimumBurstReactionTime = 0.75f;

        minPauseBetweenBursts = 0.45f;
        maxPauseBetweenBursts = 0.85f;

        noteTravelTime = 2.60f;
        enemiesPerSpeedIncrease = 2;
        travelTimeReductionPerTier = 0.12f;
        minimumNoteTravelTime = 2.00f;

        perfectWindow = 0.07f;
        goodWindow = 0.16f;

        countdownStepDuration = 0.8f;
        goDuration = 0.45f;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

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
    }
}
