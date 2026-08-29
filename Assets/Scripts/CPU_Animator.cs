using System.Collections;
using UnityEngine;

public class CPU_Animator : MonoBehaviour{

    int cheatingOpportunity;
    public int sweatOpportunity;
    public int dartOpportunity;
    public int shakeOpportunity;
    public int coughOpportunity;
    public bool catchOpportunity;
    public bool isCheating;
    public bool caughtCheating;

    // Shared with the poker engine's CheatingPlayer decorator for this bot.
    public CheatState CheatState { get; } = new CheatState();

    public Animator _animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        catchOpportunity = false;
        isCheating = false;
        sweatOpportunity = 13;
        dartOpportunity = 8;
        shakeOpportunity = 3;
        coughOpportunity = 13;
        StartCoroutine(Cheat());
    }

    // // Update is called once per frame
    // void Update(){
        
    // }

    void ClearCheatStates(){
        _animator.SetBool("isSweating", false);
        _animator.SetBool("isDarting", false);
        _animator.SetBool("isShaking", false);
        _animator.SetBool("isCoughing", false);
    }

    IEnumerator Cheat(){
        while (true){
            cheatingOpportunity = Random.Range(0, 11);
            if (cheatingOpportunity >= sweatOpportunity && !isCheating){
                isCheating = true; // In the process of cheating, can not cheat again while cheating
                _animator.SetBool("isSweating", true);
                StartCoroutine(CatchOpportunity());
            }
            if (cheatingOpportunity >= dartOpportunity && !isCheating){
                isCheating = true;
                _animator.SetBool("isDarting", true);
                StartCoroutine(CatchOpportunity());
            }
            if (cheatingOpportunity >= shakeOpportunity && !isCheating){
                isCheating = true;
                _animator.SetBool("isShaking", true);
                StartCoroutine(CatchOpportunity());
            }
            if (cheatingOpportunity >= coughOpportunity && !isCheating){
                isCheating = true;
                _animator.SetBool("isCoughing", true);
                StartCoroutine(CatchOpportunity());
            }
            yield return new WaitForSeconds(3f);
        }
    }


    IEnumerator CatchOpportunity(){
        catchOpportunity = true; // This is your chance! Catch the cheater!
        yield return new WaitForSeconds(1.0f);
        ClearCheatStates();
        yield return new WaitForSeconds(2.0f);
        catchOpportunity = false; // Times up! Can no longer be caught
        if (!caughtCheating){
            CheatState.Active = true; // Got away with it - the poker engine gets a one-shot edge
        }
        isCheating = false; // Can start cheating again
    }
}
