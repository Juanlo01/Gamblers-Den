using System.Collections;
using UnityEngine;

public class CPU_Animator : MonoBehaviour{

    // Display name for this character, shown on the dialogue nameplate.
    [field: SerializeField] public string Name { get; set; }

    // snake_case id used by the .yarn scripts (e.g. "general_niu"). Kept separate
    // from Name because that is the human-facing "General Niu" on the nameplate.
    // Must match one of DialogueManager.AllNpcIds.
    [field: SerializeField] public string YarnId { get; set; }

    int cheatingOpportunity = 0;
    public int sweatOpportunity = 3;
    public int dartOpportunity = 13;
    public int shakeOpportunity = 13;
    public int coughOpportunity = 13;
    public int blinkOpportunity = 13;
    public bool catchOpportunity;
    public bool isCheating;
    public bool caughtCheating;

    // Shared with the poker engine's CheatingPlayer decorator for this bot.
    public CheatState CheatState { get; } = new CheatState();
    [SerializeField] private SpriteRenderer sweatDrop;

    public Animator _animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        _animator.SetBool("isCaught", false);
        ClearStates();
        StartCoroutine(Cheat());
    }

    // // Update is called once per frame
    // void Update(){
        
    // }

    public void ClearStates(){
        sweatDrop.enabled = false;
        _animator.SetBool("isSweating", false);
        _animator.SetBool("isDarting", false);
        _animator.SetBool("isShaking", false);
        _animator.SetBool("isCoughing", false);
        _animator.SetBool("isBlinking", false);
        //_animator.SetBool("isCaught", false);
        isCheating = false;
        caughtCheating = false;
        catchOpportunity = false;
    }

    public void ClearAnimations(){
        sweatDrop.enabled = false;
        _animator.SetBool("isSweating", false);
        _animator.SetBool("isDarting", false);
        _animator.SetBool("isShaking", false);
        _animator.SetBool("isCoughing", false);
        _animator.SetBool("isBlinking", false);
        //_animator.SetBool("isCaught", false);
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
                ClearAnimations();
                yield return new WaitForSeconds(3f);
                isCheating = false; // redundant only if already was cheating
                //StartCoroutine(CatchOpportunity()); Can not get caught cus not cheating
            }
            yield return null;
        }
    }


    IEnumerator CatchOpportunity(){
        catchOpportunity = true; // This is your chance! Catch the cheater!
        yield return new WaitForSeconds(1.0f);
        ClearAnimations();
        yield return new WaitForSeconds(2.0f);
        catchOpportunity = false; // Times up! Can no longer be caught
        if (!caughtCheating){
            CheatState.Active = true; // Got away with it - the poker engine gets a one-shot edge
        }
        isCheating = false; // Can start cheating again
        ClearStates();
    }
}
