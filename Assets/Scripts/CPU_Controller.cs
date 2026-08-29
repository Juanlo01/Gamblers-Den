using System.Collections;
using UnityEngine;

public class CPU_Controller : MonoBehaviour{

    public GameObject[] npcs;
    public Vector3 stageExit = new Vector3(3, 0, 0);
    public float moveDuration = 5.0f;

    CPU_Animator CA1, CA2, CA3, CA4, CA5;

    public PlayerTableStatus Status { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        CA1 = npcs[0].GetComponent<CPU_Animator>();
        CA2 = npcs[1].GetComponent<CPU_Animator>();
        CA3 = npcs[2].GetComponent<CPU_Animator>();
        CA4 = npcs[3].GetComponent<CPU_Animator>();
        CA5 = npcs[4].GetComponent<CPU_Animator>();
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
