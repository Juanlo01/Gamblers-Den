using System.Collections;
using UnityEngine;

public class CPU_Animator : MonoBehaviour{

    int cheatingOpportunity = 0;
    public int sweatOpportunity = 3;
    public int dartOpportunity = 13;
    public int shakeOpportunity = 13;
    public int coughOpportunity = 13;
    public int blinkOpportunity = 13;
    public bool catchOpportunity;
    public bool isCheating;
    public bool caughtCheating;

    [SerializeField] private SpriteRenderer sweatDrop;

    public Animator _animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        ClearStates();
        StartCoroutine(Cheat());
    }

    // // Update is called once per frame
    // void Update(){
        
    // }

    void ClearStates(){
        sweatDrop.enabled = false;
        _animator.SetBool("isSweating", false);
        _animator.SetBool("isDarting", false);
        _animator.SetBool("isShaking", false);
        _animator.SetBool("isCoughing", false);
        _animator.SetBool("isBlinking", false);
        _animator.SetBool("isCaught", false);
        isCheating = false;
        caughtCheating = false;
        catchOpportunity = false;
    }

    IEnumerator Cheat(){
        while (true){
            cheatingOpportunity = Random.Range(0, 11);
            if (cheatingOpportunity >= sweatOpportunity && !isCheating){
                isCheating = true; // In the process of cheating, can not cheat again while cheating
                _animator.SetBool("isSweating", true);
                sweatDrop.enabled = true;
                StartCoroutine(CatchOpportunity());
            }
            else if (cheatingOpportunity >= dartOpportunity && !isCheating){
                isCheating = true;
                _animator.SetBool("isDarting", true);
                StartCoroutine(CatchOpportunity());
            }
            else if (cheatingOpportunity >= shakeOpportunity && !isCheating){
                isCheating = true;
                _animator.SetBool("isShaking", true);
                StartCoroutine(CatchOpportunity());
            }
            else if (cheatingOpportunity >= coughOpportunity && !isCheating){
                isCheating = true;
                _animator.SetBool("isCoughing", true);
                StartCoroutine(CatchOpportunity());
            }
            else if (cheatingOpportunity >= blinkOpportunity && !isCheating){
                isCheating = true; // prevents attempting to cheat while already in animation
                _animator.SetBool("isBlinking", true);
                //StartCoroutine(CatchOpportunity()); Can not get caught cus not cheating
            }
            yield return new WaitForSeconds(3f);
            isCheating = false; // redundant only if already was cheating
        }
    }


    IEnumerator CatchOpportunity(){
        catchOpportunity = true; // This is your chance! Catch the cheater!
        yield return new WaitForSeconds(1.0f);
        yield return new WaitForSeconds(2.0f);
        catchOpportunity = false; // Times up! Can no longer be caught
        isCheating = false; // Can start cheating again
        ClearStates();
    }
}
