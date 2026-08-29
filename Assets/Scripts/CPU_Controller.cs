using System.Collections;
using TMPro;
using UnityEngine;

public class CPU_Controller : MonoBehaviour{

    public GameObject NPC1;
    public GameObject NPC2;
    public Vector3 stageExit = new Vector3(3, 0, 0);
    CPU_Animator CA1;
    CPU_Animator CA2;

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

    // Name of whichever NPC model is currently on stage.
    public string CharacterName{
        get{
            EnsureAnimators();
            if (NPC2 != null && NPC2.activeSelf && CA2 != null) return CA2.Name;
            if (CA1 != null) return CA1.Name;
            return string.Empty;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        EnsureAnimators();
    }

    // CallDialogue can be reached before Start(), so resolve on demand rather
    // than relying on Start() having run.
    void EnsureAnimators(){
        if (CA1 == null && NPC1 != null) CA1 = NPC1.GetComponent<CPU_Animator>();
        if (CA2 == null && NPC2 != null) CA2 = NPC2.GetComponent<CPU_Animator>();
    }

    // Picks this character's next line, pushes it into the assigned TMPs, and
    // hands it back so the caller can decide whether to open a dialogue box.
    // Returns an empty string when there is nothing to say.
    public string CallDialogue(){
        string line = PlaceholderLine;
        string speaker = CharacterName;

        Debug.Log($"[Dialogue] CallDialogue() on '{name}' -> name=\"{speaker}\", line=\"{line}\"", this);

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
        }
        else if (CA2.caughtCheating == true && NPC2.activeSelf){
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
            NPC1.SetActive(true);
        }
    }

    IEnumerator Caught(){
        transform.position -= stageExit;
        SwapNPC();
        yield return new WaitForSeconds(3f);
        transform.position += stageExit;
    }
}
