using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour{
    
    public float rayLength;
    public LayerMask layermask;
    public CPU_Controller cpu_controller1;
    public CPU_Controller cpu_controller2;
    public CPU_Controller cpu_controller3;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update(){
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()){
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //Debug.Log("Player Clicked!");
            if (Physics.Raycast(ray, out hit, rayLength, layermask)){
                if (hit.collider.GetComponent<CPU_Animator>().catchOpportunity && hit.collider.transform.parent.name == "CPU1"){
                    Debug.Log(hit.collider.name + " Caught Cheating!");
                    Debug.Log(hit.collider.transform.parent.name);
                    hit.collider.GetComponent<CPU_Animator>().ClearStates();
                    hit.collider.GetComponent<CPU_Animator>().caughtCheating = true;
                    hit.collider.GetComponent<CPU_Animator>()._animator.SetBool("isCaught", true);
                    cpu_controller1.Transition();
                }
                else if (hit.collider.GetComponent<CPU_Animator>().catchOpportunity && hit.collider.transform.parent.name == "CPU2"){
                    Debug.Log(hit.collider.name + " Caught Cheating!");
                    Debug.Log(hit.collider.transform.parent.name);
                    hit.collider.GetComponent<CPU_Animator>().ClearStates();
                    hit.collider.GetComponent<CPU_Animator>().caughtCheating = true;
                    hit.collider.GetComponent<CPU_Animator>()._animator.SetBool("isCaught", true);
                    cpu_controller2.Transition();
                }
                else if (hit.collider.GetComponent<CPU_Animator>().catchOpportunity && hit.collider.transform.parent.name == "CPU3"){
                    Debug.Log(hit.collider.name + " Caught Cheating!");
                    Debug.Log(hit.collider.transform.parent.name);
                    hit.collider.GetComponent<CPU_Animator>().ClearStates();
                    hit.collider.GetComponent<CPU_Animator>().caughtCheating = true;
                    hit.collider.GetComponent<CPU_Animator>()._animator.SetBool("isCaught", true);
                    cpu_controller3.Transition();
                }
            }
        }
    }
}
