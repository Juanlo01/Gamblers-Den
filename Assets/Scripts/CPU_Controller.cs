using System.Collections;
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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        CA1 = NPC1.GetComponent<CPU_Animator>();
        CA2 = NPC2.GetComponent<CPU_Animator>();
        CA3 = NPC3.GetComponent<CPU_Animator>();
        CA4 = NPC4.GetComponent<CPU_Animator>();
        CA5 = NPC5.GetComponent<CPU_Animator>();
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
