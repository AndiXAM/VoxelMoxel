using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class IntroCutsceneManager : MonoBehaviour
{
    public static IntroCutsceneManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject blackScreenOverlay;   
    public GameObject tutorialPromptWindow;  
    public GameObject hotbarHUD;             

    [Header("UI Text")]
    public TextMeshProUGUI introText;        
    public TextMeshProUGUI tutorialText;     

    [Header("Audio")]
    public AudioSource windAudioSource;      
    public AudioSource mainMusicAudioSource; 
    public AudioClip woodBreakSound;         
    public AudioClip typingSound;            
    private AudioSource myAudio;

    [Header("Player Systems References")]
    public StatsContainer playerStats;
    public Inventory playerInventory;
    public SkillTreeUIManager skillTreeUI;
    
    [Header("Quest Objects")]
    public ClassData baseClass;              
    public Item fistsItem;                   
    public GameObject debrisBlock;     

    [Header("Intro NPC Dialogue")]
    public NPCInteractable introNPC;       
    public DialogueData introDialogue;        

    [Header("Story Lines (English)")]
    [TextArea(3, 5)]

      
    public string[] storyLines = new string[]
    {
        "A cold wind howled all around...",
        "The house burned to the ground. Everything I had turned to ash.",
        "But somehow, I survived.",
        "Now I need to break through this debris and escape..."
    };

    private int tutorialStep = 0;
    private bool isTyping = false;
    private string fullCurrentText;

    private Coroutine tutorialTypeCoroutine;

    private void Awake()
    {
        Instance = this;
        myAudio = GetComponent<AudioSource>();
        if (myAudio == null) myAudio = gameObject.AddComponent<AudioSource>();
    }

    public void StartCutscene()
    {
        blackScreenOverlay.SetActive(true);
        hotbarHUD.SetActive(false);
        tutorialPromptWindow.SetActive(false);
        introText.text = "";

        if (mainMusicAudioSource != null)
        {
            mainMusicAudioSource.Stop();
        }

        if (windAudioSource != null)
        {
            windAudioSource.volume = 0.5f;
            windAudioSource.Play();
        }

        SetPlayerControl(false);
        StartCoroutine(PlayStoryRoutine());
    }

    // --- PHASE 1: STORY LINES ---
    private IEnumerator PlayStoryRoutine()
    {
        // 5 seconds of darkness at start
        yield return new WaitForSeconds(5f);

        for (int i = 0; i < storyLines.Length; i++)
        {
            fullCurrentText = storyLines[i];
            introText.text = "";
            
            yield return StartCoroutine(TypeLineRoutine(fullCurrentText));
            
            yield return new WaitForSeconds(5f);
        }

        introText.text = ""; 

        // --- 1. PAUSE FOR 2 SECONDS IN DARKNESS BEFORE TUTORIAL ---
        yield return new WaitForSeconds(2f);

        StartTutorial();
    }

    private IEnumerator TypeLineRoutine(string line)
    {
        isTyping = true;
        foreach (char c in line.ToCharArray())
        {
            introText.text += c;
            if (c != ' ' && typingSound != null)
            {
                myAudio.pitch = Random.Range(0.9f, 1.1f);
                myAudio.PlayOneShot(typingSound);
            }
            yield return new WaitForSeconds(0.08f); // Slow speed
        }
        isTyping = false;
    }

    // --- PHASE 2: STEP-BY-STEP TUTORIAL ---
    private void StartTutorial()
    {
        tutorialPromptWindow.SetActive(true);
        
        // Unlock cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Start step 1 with 2x faster typewriter effect (0.04f)
        ChangeTutorialStep(1, "Press [TAB] to open the Progression Menu.");
    }

    // Safely changes the step and starts the typewriter effect on the prompt text
    private void ChangeTutorialStep(int newStep, string text)
    {
        tutorialStep = newStep;
        if (tutorialTypeCoroutine != null) StopCoroutine(tutorialTypeCoroutine);
        tutorialTypeCoroutine = StartCoroutine(TypeTutorialPromptRoutine(text));
    }

    // --- 2. TYPEWRITER EFFECT FOR TUTORIAL TEXT (2X FASTER) ---
    private IEnumerator TypeTutorialPromptRoutine(string line)
    {
        tutorialText.text = "";
        foreach (char c in line.ToCharArray())
        {
            tutorialText.text += c;
            if (c != ' ' && typingSound != null)
            {
                // Slightly higher pitch for tutorial prompts to distinguish from story
                myAudio.pitch = Random.Range(1.1f, 1.3f); 
                myAudio.PlayOneShot(typingSound);
            }
            yield return new WaitForSeconds(0.04f); // 2x faster than story (0.08f)
        }
    }

    private void Update()
    {
        if (tutorialStep == 0) return;

        switch (tutorialStep)
        {
            case 1: // Wait for menu open
                if (Input.GetKeyDown(KeyCode.Tab) || (skillTreeUI != null && skillTreeUI.menuCanvas.activeInHierarchy))
                {
                    // --- 3. REFINED CLASS SELECTION FLOW (Step 1: Unlock) ---
                    ChangeTutorialStep(2, "Click on the Base Class node to UNLOCK it.");
                }
                break;

            case 2: // Wait for player to UNLOCK Base Class
                if (skillTreeUI != null && skillTreeUI.IsClassUnlocked(baseClass))
                {
                    // (Step 2: Equip/Activate)
                    ChangeTutorialStep(3, "Click on the Base Class node again to EQUIP it.");
                }
                break;

            case 3: // Wait for player to EQUIP Base Class
                if (playerStats != null && playerStats.equippedClass == baseClass)
                {
                    // (Step 3: Enter Tree)
                    ChangeTutorialStep(4, "Click on the Base Class node once more to ENTER its skill tree.");
                }
                break;

            case 4: // Wait for player to ENTER the skill tree panel
                if (skillTreeUI != null && skillTreeUI.skillGraphPanel.activeInHierarchy)
                {
                    hotbarHUD.SetActive(true); // Show Hotbar
                    ChangeTutorialStep(5, "Double-click the Fists icon to unlock and equip them.");
                }
                break;

            case 5: // Wait for Fists to appear in hotbar
                if (playerInventory != null && playerInventory.slotsData[0].item == fistsItem)
                {
                    ChangeTutorialStep(6, "Press [TAB] to close the Progression Menu.");
                }
                break;

            case 6: // Wait for menu close
                if (Input.GetKeyDown(KeyCode.Tab) || (skillTreeUI != null && !skillTreeUI.menuCanvas.activeInHierarchy))
                {
                    SetPlayerControl(true); // Enable movement
                    
                    // Lock cursor for gameplay camera
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;

                    ChangeTutorialStep(7, "Select Fists in your hotbar by pressing [1].");
                }
                break;

            case 7: // Wait for Slot 1 selection
                if (playerInventory != null && playerInventory.selectedSlotIndex == 0)
                {
                    ChangeTutorialStep(8, "Fight! (Press LMB to attack)");
                }
                break;

            case 8: // Wait for debris destruction
                if (debrisBlock == null) 
                {
                    StartCoroutine(EndCutsceneRoutine());
                }
                break;
        }
    }

    // --- PHASE 3: THE ESCAPE ---
    private IEnumerator EndCutsceneRoutine()
    {
        tutorialStep = 0; 
        tutorialPromptWindow.SetActive(false);

        if (myAudio != null && woodBreakSound != null)
        {
            myAudio.PlayOneShot(woodBreakSound);
        }

        if (windAudioSource != null)
        {
            StartCoroutine(FadeOutAudio(windAudioSource, 2f));
        }

        yield return new WaitForSeconds(2f);

        if (mainMusicAudioSource != null)
        {
            mainMusicAudioSource.Play();
        }

        Image blackImage = blackScreenOverlay.GetComponent<Image>();
        if (blackImage != null)
        {
            float timer = 0f;
            float fadeDuration = 3f; 
            Color startColor = blackImage.color;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                blackImage.color = Color.Lerp(startColor, Color.clear, timer / fadeDuration);
                yield return null;
            }
        }

        blackScreenOverlay.SetActive(false);
        
        // --- 4. RETURN MOUSE/CURSOR TO PLAYER WHEN SCREEN BRIGHTENS ---
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ---  ЗАПУСКАЕМ ДИАЛОГ С НПС ---
         if (introNPC != null && introDialogue != null)
        {
            // Находим мозг нашего НПС
            NPCBehavior b = introNPC.GetComponent<NPCBehavior>();
            if (b == null) b = introNPC.GetComponentInChildren<NPCBehavior>();

            // Вызываем метод, передавая И мозг, И особый диалог!
            introNPC.StartInteraction(b, introDialogue);
        }

        Debug.Log("Intro cutscene successfully completed!");
    }

    private void SetPlayerControl(bool state)
    {
        Character moveScript = playerStats.GetComponent<Character>();
        if (moveScript != null) moveScript.enabled = state;

        PlayerCombat combatScript = playerStats.GetComponent<PlayerCombat>();
        if (combatScript != null) combatScript.enabled = state;
    }

    private IEnumerator FadeOutAudio(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }
        source.Stop();
    }
}