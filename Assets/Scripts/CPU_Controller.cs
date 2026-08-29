using System.Collections;
using TMPro;
using UnityEngine;

public class CPU_Controller : MonoBehaviour{

    public GameObject NPC1;
    public GameObject NPC2;
    public GameObject NPC3;
    public GameObject NPC4;
    public GameObject NPC5;
    public Vector3 stageExit = new Vector3(3, 0, 0);
    CPU_Animator CA1;
    CPU_Animator CA2;
    CPU_Animator CA3;
    CPU_Animator CA4;
    CPU_Animator CA5;

    [Header("Dialogue")]
    [Tooltip("YarnSpinner script for this character. Held as a TextAsset until the YarnSpinner package is added.")]
    public TextAsset yarnFile;

    [Tooltip("TMP showing this character's name while they are speaking.")]
    public TMP_Text Nameplate;

    [Tooltip("TMP showing this character's spoken line.")]
    public TMP_Text DialogueText;

    // Placeholder line until dialogue is driven from yarnFile.
    private const string PlaceholderLine = "Awfully confident for someone with such a weak hand.";

    public PlayerTableStatus Status { get; private set; }

    // The CPU_Animator on whichever NPC model is currently on stage. Falls back
    // to the first assigned one so identity still resolves before Start() runs.
    public CPU_Animator ActiveAnimator{
        get{
            EnsureAnimators();

            var slots = new[] { NPC1, NPC2, NPC3, NPC4, NPC5 };
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
        EnsureAnimators();
      
        CA1 = NPC1.GetComponent<CPU_Animator>();
        CA2 = NPC2.GetComponent<CPU_Animator>();
        CA3 = NPC3.GetComponent<CPU_Animator>();
        CA4 = NPC4.GetComponent<CPU_Animator>();
        CA5 = NPC5.GetComponent<CPU_Animator>();
    }

    // CallDialogue can be reached before Start(), so resolve on demand rather
    // than relying on Start() having run.
    void EnsureAnimators(){
        if (CA1 == null && NPC1 != null) CA1 = NPC1.GetComponent<CPU_Animator>();
        if (CA2 == null && NPC2 != null) CA2 = NPC2.GetComponent<CPU_Animator>();
        if (CA3 == null && NPC3 != null) CA3 = NPC3.GetComponent<CPU_Animator>();
        if (CA4 == null && NPC4 != null) CA4 = NPC4.GetComponent<CPU_Animator>();
        if (CA5 == null && NPC5 != null) CA5 = NPC5.GetComponent<CPU_Animator>();
    }

    // Picks this character's next line, pushes it into the assigned TMPs, and
    // hands it back so the caller can decide whether to open a dialogue box.
    // Returns an empty string when there is nothing to say.
    public string CallDialogue(){
        return CallDialogue(string.Empty);
    }

    // Asks DialogueManager for this character's own reaction to "action", falling
    // back to the placeholder when no yarn node matches (or none are loaded).
    public string CallDialogue(string action){
        string speaker = CharacterName;
        string line = PlaceholderLine;

        var manager = DialogueManager.Instance;
        if (manager != null && !string.IsNullOrEmpty(YarnId)){
            var selection = manager.RequestSelfReaction(YarnId, action);
            if (selection != null){
                line = selection.Text;
                speaker = selection.SpeakerDisplayName;
            }
        }

        return Speak(speaker, line);
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

    // Update is called once per frame
    void Update(){
        if (CA1.caughtCheating == true && NPC1.activeSelf){
            StartCoroutine(Caught());
            CA1._animator.SetBool("isCaught", true);
        }
        else if (CA2.caughtCheating == true && NPC2.activeSelf){
            CA2._animator.SetBool("isCaught", true);
            StartCoroutine(Caught());
        }
        else if (CA3.caughtCheating == true && NPC3.activeSelf){
            CA3._animator.SetBool("isCaught", true);
            StartCoroutine(Caught());
        }
        else if (CA4.caughtCheating == true && NPC4.activeSelf){
            CA4._animator.SetBool("isCaught", true);
            StartCoroutine(Caught());
        }
        else if (CA5.caughtCheating == true && NPC5.activeSelf){
            CA5._animator.SetBool("isCaught", true);
            StartCoroutine(Caught());
        }
    }

    void SwapNPC(){
        if (NPC1.activeSelf){
            CA1.caughtCheating = false;
            NPC1.SetActive(false);
            NPC2.SetActive(true);
        }
        else if (NPC2.activeSelf){
            CA2.caughtCheating = false;
            NPC2.SetActive(false);
            NPC3.SetActive(true);
        }
        else if (NPC3.activeSelf){
            CA3.caughtCheating = false;
            NPC3.SetActive(false);
            NPC4.SetActive(true);
        }
        else if (NPC4.activeSelf){
            CA4.caughtCheating = false;
            NPC4.SetActive(false);
            NPC5.SetActive(true);
        }
        else if (NPC5.activeSelf){
            CA5.caughtCheating = false;
            NPC5.SetActive(false);
            NPC1.SetActive(true);
        }
    }

    IEnumerator Caught(){
        transform.position = Vector3.MoveTowards(transform.position, transform.position - stageExit, 2 * Time.deltaTime);
        yield return new WaitForSeconds(10f);
        SwapNPC();
        yield return new WaitForSeconds(10f);
        transform.position = Vector3.MoveTowards(transform.position, transform.position + stageExit, 2 * Time.deltaTime);
    }
}
