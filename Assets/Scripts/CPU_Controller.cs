using System.Collections;
using TMPro;
using UnityEngine;

public class CPU_Controller : MonoBehaviour{

    public GameObject[] npcs;
    public Vector3 stageExit = new Vector3(3, 0, 0);
    public float moveDuration = 5.0f;

    CPU_Animator CA1, CA2, CA3, CA4, CA5;

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
        CA1 = npcs[0].GetComponent<CPU_Animator>();
        CA2 = npcs[1].GetComponent<CPU_Animator>();
        CA3 = npcs[2].GetComponent<CPU_Animator>();
        CA4 = npcs[3].GetComponent<CPU_Animator>();
        CA5 = npcs[4].GetComponent<CPU_Animator>();
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
            
    }

    public void Transition(){
        if (CA1.caughtCheating == true && npcs[0].activeSelf){
                StartCoroutine(Caught());
                CA1._animator.SetBool("isCaught", true);
            }
        else if (CA2.caughtCheating == true && npcs[1].activeSelf){
                CA2._animator.SetBool("isCaught", true);
                StartCoroutine(Caught());
            }
        else if (CA3.caughtCheating == true && npcs[2].activeSelf){
                CA3._animator.SetBool("isCaught", true);
                StartCoroutine(Caught());
            }
        else if (CA4.caughtCheating == true && npcs[3].activeSelf){
                CA4._animator.SetBool("isCaught", true);
                StartCoroutine(Caught());
            }
        else if (CA5.caughtCheating == true && npcs[4].activeSelf){
                CA5._animator.SetBool("isCaught", true);
                StartCoroutine(Caught());
            }
    }

    void SwapNPC(){
        int randomIndex = Random.Range(0, npcs.Length);

        for (int i = 0; i < npcs.Length; i++){
            if (npcs[i].activeSelf){
                npcs[i].SetActive(false);
            }
        }

        npcs[randomIndex].SetActive(true);


        // if (NPC1.activeSelf){
        //     CA1.caughtCheating = false;
        //     NPC1.SetActive(false);
        //     NPC2.SetActive(true);
        // }
        // else if (NPC2.activeSelf){
        //     CA2.caughtCheating = false;
        //     NPC2.SetActive(false);
        //     NPC3.SetActive(true);
        // }
        // else if (NPC3.activeSelf){
        //     CA3.caughtCheating = false;
        //     NPC3.SetActive(false);
        //     NPC4.SetActive(true);
        // }
        // else if (NPC4.activeSelf){
        //     CA4.caughtCheating = false;
        //     NPC4.SetActive(false);
        //     NPC5.SetActive(true);
        // }
        // else if (NPC5.activeSelf){
        //     CA5.caughtCheating = false;
        //     NPC5.SetActive(false);
        //     NPC1.SetActive(true);
        // }
        
    }

    IEnumerator Move(Vector3 target, float duration){
        Vector3 position = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float smoothedT = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(position, position - target, smoothedT);

            yield return null;
        }
        
    }

    IEnumerator Caught(){
        StartCoroutine(Move(stageExit, moveDuration));
        yield return new WaitForSeconds(7f);
        SwapNPC();
        yield return new WaitForSeconds(7f);
        StartCoroutine(Move(-stageExit, moveDuration));

        //transform.position = Vector3.Lerp(transform.position, transform.position - stageExit, 2 * Time.deltaTime);
        //transform.position = Vector3.MoveTowards(transform.position, transform.position + stageExit, 2 * Time.deltaTime);
    }
}
