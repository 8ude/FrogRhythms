using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Beat;

/// <summary>
/// The purpose of this class is twofold:
/// - Get the Clock-synchronized timing of the user's input
/// - Check that against the windows of currently existing obstacles
/// </summary>
public class FrogGameInputEvaluator : MonoBehaviour
{

    public List<FrogGameCueBug> activeBugs;
    public List<RhythmInput> CachedInputs = new List<RhythmInput>();

    public FrogGameBeatmap currentBeatmap;

    //Updated Input system uses Unity's Input manager
    public bool usingUnityInputManager;

    //ideally we'd manage score on a seperate script
    public int gameScore;

    public AudioSource[] pooledAudioSources;

    public AudioClip[] missedClips, hitClips;

    //prevent button mashing
    public float earlyMissCooldown = 0.1f;


    void Awake()
    {
        usingUnityInputManager = currentBeatmap.usingInputClass;
    }


    void Update()
    {

        if (usingUnityInputManager)
        {
            for (int i = 0; i < currentBeatmap.frogGameCues.Length; i++)
            {

                if (Input.GetButtonDown(currentBeatmap.frogGameCues[i].playerInput))
                {
                    RhythmInput _rhythmInput = new RhythmInput();
                    _rhythmInput.inputString = currentBeatmap.frogGameCues[i].playerInput;
                    _rhythmInput.inputTime = Clock.Instance.TimeMS;

                    CachedInputs.Add(_rhythmInput);
                }
            }

        }
        else
        {
            for (int i = 0; i < currentBeatmap.playerInputKeys.Length; i++)
            {
                if (Input.GetKeyDown(currentBeatmap.playerInputKeys[i]))
                {
                    RhythmInput _rhythmInput = new RhythmInput();
                    _rhythmInput.inputKey = currentBeatmap.playerInputKeys[i];
                    _rhythmInput.inputTime = Clock.Instance.TimeMS;

                    CachedInputs.Add(_rhythmInput);
                }
            }
        }


        //compare inputs to current beatMap windows

        //first find any non-destroyed cues

        FrogGameCueBug[] allBugs = FindObjectsOfType<FrogGameCueBug>();

        activeBugs.AddRange(allBugs);

        //go through each of our inputs from this frame, and check them against this gem
        for (int j = 0; j < CachedInputs.Count; j++)
        {

            //want to register an early input if 
            //there's no other (non-early) active bug in this lane
            
            bool earlyInput = true;

            for(int i = 0; i < activeBugs.Count; i++)
            {
                if (activeBugs[i].bugCueState != FrogGameCueBug.CueState.Early)
                {
                    if (CachedInputs[j].inputKey == activeBugs[i].bmEvent.inputKey
                    || CachedInputs[j].inputString == activeBugs[i].bmEvent.unityInput)
                    {
                        ScoreGem(activeBugs[i]);
                        earlyInput = false;
                    }
                }
            }

            if(earlyInput)
            {
                PlayFeedbackSound(missedClips[Random.Range(0, missedClips.Length)]);
            }
            
        }



        //check 

        //clear Lists
        activeBugs.Clear();
        CachedInputs.Clear();

    }

    void ScoreGem(FrogGameCueBug bug)
    {
        switch (bug.bugCueState)
        {
            case FrogGameCueBug.CueState.OK:
                gameScore += 1;
                Debug.Log("OK!");
                PlayFeedbackSound(hitClips[Random.Range(0, hitClips.Length)]);
                Destroy(bug.gameObject);
                break;
            case FrogGameCueBug.CueState.Good:
                gameScore += 2;
                Debug.Log("Good!");
                PlayFeedbackSound(hitClips[Random.Range(0, hitClips.Length)]);
                Destroy(bug.gameObject);
                break;
            case FrogGameCueBug.CueState.Perfect:
                gameScore += 3;
                Debug.Log("Perfect!");
                PlayFeedbackSound(hitClips[Random.Range(0, hitClips.Length)]);
                Destroy(bug.gameObject);
                break;
            case FrogGameCueBug.CueState.Late:
                Debug.Log("Missed!");
                PlayFeedbackSound(missedClips[Random.Range(0, missedClips.Length)]);
                break;
        }


    }

    public void PlayFeedbackSound(AudioClip clip)
    {
        AudioSource sourceToUse = null;

        foreach(AudioSource source in pooledAudioSources)
        {
            if (!source.isPlaying)
            {
                sourceToUse = source;
                break;
            }
        }

        if (sourceToUse == null)
        {
            return;
        }
        else
        {
            sourceToUse.clip = clip;
            sourceToUse.Play();
        }

        

    }





}
