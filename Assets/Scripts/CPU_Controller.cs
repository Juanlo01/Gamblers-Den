using System.Collections;
using TMPro;
using UnityEngine;

public class CPU_Controller : MonoBehaviour{

    public bool caught = false;
    public GameObject[] npcs; // List of NPCs
    public Vector3 stageExit = new Vector3(3, 0, 0); // Position at which NPC will exit when caught

    // Index into npcs[]/CA1..CA5 of whichever NPC currently occupies this seat.
    // Updated only from the main thread (Start/SwapNPC). GameObject.activeSelf
    // itself is Unity API and cannot be read from the poker engine's background
    // thread, so ActiveCheatState reads this cached index instead.
    private volatile int _activeIndex;

    // Whichever NPC is currently occupying this seat - read fresh so it
    // survives a swap, used by the poker engine's CheatingPlayer decorator.
    // Null until this seat's own Start() has run (CA1..CA5 assigned).
    public CheatState ActiveCheatState
    {
        get
        {
            var animators = new[] { CA1, CA2, CA3, CA4, CA5 };
            var active = _activeIndex >= 0 && _activeIndex < animators.Length ? animators[_activeIndex] : null;
            return active != null ? active.CheatState : null;
        }
    }

    public float moveDuration = 5.0f;

    // Total number of cheaters caught across all seats this session. Read by
    // CaughtPanelDisplay to show "Cheaters caught: N" in the UI.
    public static int CheatersCaught { get; private set; }

    CPU_Animator CA1, CA2, CA3, CA4, CA5;

    [Header("Dialogue")]
    [Tooltip("YarnSpinner script for this character. Held as a TextAsset until the YarnSpinner package is added.")]
    public TextAsset yarnFile;

    [Tooltip("TMP showing this character's name while they are speaking.")]
    public TMP_Text Nameplate;

    [Tooltip("TMP showing this character's spoken line.")]
    public TMP_Text DialogueText;

    public PlayerTableStatus Status { get; private set; }

    // The CPU_Animator on whichever NPC model is currently on stage. Falls back
    // to the first assigned one so identity still resolves before Start() runs.
    public CPU_Animator ActiveAnimator{
        get{
            EnsureAnimators();

            var slots = new[] { npcs[0], npcs[1], npcs[2], npcs[3], npcs[4] };
            var animators = new[] { CA1, CA2, CA3, CA4, CA5 };

            for (int i = 0; i < slots.Length; i++){
                if (slots[i] != null && slots[i].activeSelf && animators[i] != null) return animators[i];
            }

            for (int i = 0; i < animators.Length; i++){
                if (animators[i] != null) return animators[i];
            }

            return null;
        }
    }

    // Name of whichever NPC model is currently on stage.
    public string CharacterName{
        get{
            var animator = ActiveAnimator;
            return animator != null ? animator.Name : string.Empty;
        }
    }

    // snake_case yarn id of whichever NPC model is currently on stage.
    public string YarnId{
        get{
            var animator = ActiveAnimator;
            return animator != null ? animator.YarnId : string.Empty;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        CA1 = npcs[0].GetComponent<CPU_Animator>();
        CA2 = npcs[1].GetComponent<CPU_Animator>();
        CA3 = npcs[2].GetComponent<CPU_Animator>();
        CA4 = npcs[3].GetComponent<CPU_Animator>();
        CA5 = npcs[4].GetComponent<CPU_Animator>();

        for (int i = 0; i < npcs.Length; i++){
            if (npcs[i].activeSelf){
                _activeIndex = i;
                break;
            }
        }
    }

    // CallDialogue can be reached before Start(), so resolve on demand rather
    // than relying on Start() having run.
    void EnsureAnimators(){
        if (CA1 == null && npcs[0] != null) CA1 = npcs[0].GetComponent<CPU_Animator>();
        if (CA2 == null && npcs[1] != null) CA2 = npcs[1].GetComponent<CPU_Animator>();
        if (CA3 == null && npcs[2] != null) CA3 = npcs[2].GetComponent<CPU_Animator>();
        if (CA4 == null && npcs[3] != null) CA4 = npcs[3].GetComponent<CPU_Animator>();
        if (CA5 == null && npcs[4] != null) CA5 = npcs[4].GetComponent<CPU_Animator>();
    }

    // Picks this character's next line, pushes it into the assigned TMPs, and
    // hands it back so the caller can decide whether to open a dialogue box.
    // Returns an empty string when there is nothing to say.
    public string CallDialogue(){
        return CallDialogue(string.Empty);
    }

    // Asks DialogueManager for this character's own reaction to "action".
    // Returns "" when no yarn node matches (or none are loaded) - there is no
    // fallback line, so the seat simply stays quiet rather than inventing one.
    public string CallDialogue(string action){
        var manager = DialogueManager.Instance;
        if (manager == null || string.IsNullOrEmpty(YarnId)){
            return string.Empty;
        }

        var selection = manager.RequestSelfReaction(YarnId, action);
        if (selection == null){
            return string.Empty;
        }

        return Speak(selection.SpeakerDisplayName, selection.Text);
    }

    // Pushes an already-chosen line into this seat's TMPs and hands it back.
    public string Speak(string speaker, string line){
        Debug.Log($"[Dialogue] Speak() on '{name}' -> name=\"{speaker}\", line=\"{line}\"", this);

        if (DialogueText != null){
            DialogueText.text = line;
        }
        else{
            Debug.LogWarning($"[Dialogue] '{name}' has no DialogueText TMP assigned.", this);
        }

        if (Nameplate != null){
            Nameplate.text = speaker;
        }
        else{
            Debug.LogWarning($"[Dialogue] '{name}' has no Nameplate TMP assigned.", this);
        }

        if (string.IsNullOrEmpty(speaker)){
            Debug.LogWarning($"[Dialogue] '{name}' resolved an empty CharacterName - set Name on the active NPC's CPU_Animator.", this);
        }

        return line;
    }

    // Called by PokerGameManager (via SlowedPlayer) whenever this seat's poker status changes.
    public void UpdateStatus(PlayerTableStatus status){
        Status = status;
    }

    public void Transition(){ // Handles which NPC was caught and that they do the caught animation
        if (CA1.caughtCheating == true && npcs[0].activeSelf){
                CheatersCaught++;
                StartCoroutine(Caught());
                CA1._animator.SetBool("isCaught", true);
            }
        else if (CA2.caughtCheating == true && npcs[1].activeSelf){
                CheatersCaught++;
                CA2._animator.SetBool("isCaught", true);
                StartCoroutine(Caught());
            }
        else if (CA3.caughtCheating == true && npcs[2].activeSelf){
                CheatersCaught++;
                CA3._animator.SetBool("isCaught", true);
                StartCoroutine(Caught());
            }
        else if (CA4.caughtCheating == true && npcs[3].activeSelf){
                CheatersCaught++;
                CA4._animator.SetBool("isCaught", true);
                StartCoroutine(Caught());
            }
        else if (CA5.caughtCheating == true && npcs[4].activeSelf){
                CheatersCaught++;
                CA5._animator.SetBool("isCaught", true);
                StartCoroutine(Caught());
            }
    }

    public void callCheat(){ // Active NPC rolls to see if they cheat
        for (int i = 0; i < npcs.Length; i++){
            if (npcs[i].activeSelf){
                StartCoroutine(npcs[i].GetComponent<CPU_Animator>().TryCheat());
            }
        }
    }

    public void Replenish(){
        if (caught){
            StartCoroutine(Move(-stageExit, moveDuration)); // New character gets brought in
            caught = false;
        }
    }

    IEnumerator Caught(){ // Routine NPC goes through when caught
        StartCoroutine(Move(stageExit, moveDuration)); // Caught character gets taken out
        yield return new WaitForSeconds(7f);
        SwapNPC(); // Chooses replacement NPC
        caught = true;
        yield return new WaitForSeconds(7f);
    }


    void SwapNPC(){ // Swaps out NPC randomly from one in the list
        int randomIndex = Random.Range(0, npcs.Length);

        for (int i = 0; i < npcs.Length; i++){
            if (npcs[i].activeSelf){
                npcs[i].SetActive(false);
            }
        }
        npcs[randomIndex].SetActive(true);
        _activeIndex = randomIndex;
    }


    IEnumerator Move(Vector3 target, float duration){ // Handles NPC Movement
        Vector3 position = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration){ // Smoothly slides character off screen
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float smoothedT = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(position, position - target, smoothedT);
            yield return null;
        }
        
    }


}
