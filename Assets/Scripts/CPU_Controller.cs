using System.Collections;
using UnityEngine;

public class CPU_Controller : MonoBehaviour{

    public GameObject NPC1;
    public GameObject NPC2;
    public Vector3 stageExit = new Vector3(3, 0, 0);
    CPU_Animator CA1;
    CPU_Animator CA2;

    public PlayerTableStatus Status { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        CA1 = NPC1.GetComponent<CPU_Animator>();
        CA2 = NPC2.GetComponent<CPU_Animator>();
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
