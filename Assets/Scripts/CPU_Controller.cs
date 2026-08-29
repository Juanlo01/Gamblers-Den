using System.Collections;
using UnityEngine;

public class CPU_Controller : MonoBehaviour{

    public GameObject NPC1;
    public GameObject NPC2;
    public Vector3 stageExit = new Vector3(3, 0, 0);
    CPU_Animator CA1;
    CPU_Animator CA2;

    // Mirrors NPC1.activeSelf, updated only from the main thread (Start/SwapNPC).
    // GameObject.activeSelf itself is Unity API and cannot be read from the
    // poker engine's background thread, so ActiveCheatState reads this instead.
    private volatile bool _npc1IsActive;

    // Whichever NPC is currently occupying this seat - read fresh so it
    // survives a swap, used by the poker engine's CheatingPlayer decorator.
    // Null until this seat's own Start() has run (CA1/CA2 assigned).
    public CheatState ActiveCheatState
    {
        get
        {
            var active = _npc1IsActive ? CA1 : CA2;
            return active != null ? active.CheatState : null;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        CA1 = NPC1.GetComponent<CPU_Animator>();
        CA2 = NPC2.GetComponent<CPU_Animator>();
        _npc1IsActive = NPC1.activeSelf;
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
            _npc1IsActive = false;
        }
        else if (NPC2.activeSelf){
            CA2.caughtCheating = false;
            NPC2.SetActive(false);
            NPC1.SetActive(true);
            _npc1IsActive = true;
        }
    }

    IEnumerator Caught(){
        transform.position -= stageExit;
        SwapNPC();
        yield return new WaitForSeconds(3f);
        transform.position += stageExit;
    }
}
