using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour{
    
    public float rayLength;
    public LayerMask layermask;


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
                if (hit.collider.GetComponent<CPU_Animator>().catchOpportunity){
                    Debug.Log(hit.collider.name + " Caught Cheating!");
                    hit.collider.GetComponent<CPU_Animator>().caughtCheating = true;
                }
            }
        }
    }
}
