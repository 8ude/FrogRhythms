using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Beat;


//in progress - more modular input system
[System.Serializable]
public class FrogGameCue
{
    [Tooltip("this must match the name in unity's input manager")]
    public string playerInput;
    public GameObject cueStartLocation;
    public GameObject cuePrefab;

}

/// <summary>
/// This class manages the different events in the beatmap.  Right now it's designed to go sequentially:
/// once the cueTime is reached, it instantiates the cue 
/// </summary>
public class FrogGameBeatmap : MonoBehaviour
{
    [Header("Our Beatmap Level")]
    public List<BeatMapEvent> beatEvents = new List<BeatMapEvent>();

    //the "cue" here can be a number of things
    //for now it's just the spawn time offset (in number of beats)
    //right now, this assumes that each beatEvent will have the same cue offset.
    //if you don't want this to be the case, have a seperate beatmap/input evaluator pair for these other events
    //(example - rhythm heaven has varying cue lengths)
    [Header("Cue Offset in Beats")]
    public int cueBeatOffset;

    //Make sure the OkWindow > GoodWindow > PerfectWindow!!!  Also make sure that you don't have successive beatmap at shorter timespans than your OkWindow
    [Header("Window Sizes in MS")]
    public double OkWindowMillis = 200d;
    public double GoodWindowMillis = 100d;
    public double PerfectWindowMillis = 50d;


    int beatEventIndex = 0;
    int cueIndex = 0;

    //these were for debugging
    double nextCueTime;
    double nextBeatEventTime;
    bool cueEndReached = false;



    //These should all be the same length, and should correspond to the different inputs, starting locations, and prefabs
    //It isn't the best pattern - it's error prone; we should have one array instead of three, 
    //so it has been improved with the "FallingGemInput" class
    [Header("Old - Keyboard Input")]
    [Tooltip("Make sure these three arrays are the same length!")]
    public KeyCode[] playerInputKeys;
    public GameObject[] cueStartLocations;
    public GameObject[] cuePrefabs;



    [Header("New - Using Unity Input Manager")]
    public bool usingInputClass;

    public FrogGameCue[] frogGameCues;

    //scriptable object that we use to parse MIDI Notes -> gameplay inputs
    public BeatmapParserKey beatmapParser;


    bool levelEndReached = false;

    //this is for loading data from Midi files
    [Header("Song file information")]
    public SongSource songSource;

    private Song currentSong;

    //this should match the name of your song in your Resources folder
    public string songName;

    private void Awake()
    {
        PrepareData(songName);
    }

    void Start()
    {

        for (int i = 0; i < beatEvents.Count; i++)
        {
            


            //set the cue times for each beat event
            //setting at 1/16th before so that we can play the audio cue exactly on beat
            beatEvents[i].cueTime = beatEvents[i].eventMBT.GetMilliseconds() - (beatEvents[i].beatCueOffset * Clock.Instance.BeatLengthD() * 1000d) - Clock.Instance.SixteenthLengthD();


        }

        beatEventIndex = 0;


        levelEndReached = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (beatEventIndex >= beatEvents.Count)
        {
            levelEndReached = true;
        }

        if (!levelEndReached)
        {

            if (Clock.Instance.TimeMS >= beatEvents[beatEventIndex].cueTime)
            {


                if (usingInputClass)
                {
                    CreateCueUnityInput();
                }
                
                beatEventIndex++;

            }
        }



    }

    public void Reset()
    {
        cueIndex = 0;
        beatEventIndex = 0;
    }

    public void CreateCueUnityInput()
    {
        //Debug.Log("create cue");
        //determining which cue lane we're in (which cue type we're using)
        int cueLaneIndex = int.MaxValue;


        for (int i = 0; i < frogGameCues.Length; i++)
        {
            if (frogGameCues[i].playerInput == beatEvents[beatEventIndex].unityInput)
            {
                cueLaneIndex = i;
            }
        }


        if (cueLaneIndex == int.MaxValue)
        {
            Debug.LogWarning("beatmap input doesn't match current inputs!");
        }

        GameObject newCue = Instantiate(frogGameCues[cueLaneIndex].cuePrefab, frogGameCues[cueLaneIndex].cueStartLocation.transform.position, Quaternion.identity);


        FrogGameCueBug cueBug = newCue.GetComponent<FrogGameCueBug>();

        cueBug.bmEvent = beatEvents[beatEventIndex];
        cueBug.nextLoopTime = Clock.Instance.AtNextSixteenth();

        //Set Window Timings
        cueBug.OkWindowStart = cueBug.bmEvent.eventMBT.GetMilliseconds() - (0.5d * OkWindowMillis);
        cueBug.OkWindowEnd = cueBug.bmEvent.eventMBT.GetMilliseconds() + (0.5d * OkWindowMillis);
        cueBug.GoodWindowStart = cueBug.bmEvent.eventMBT.GetMilliseconds() - (0.5d * GoodWindowMillis);
        cueBug.GoodWindowEnd = cueBug.bmEvent.eventMBT.GetMilliseconds() + (0.5d * GoodWindowMillis);
        cueBug.PerfectWindowStart = cueBug.bmEvent.eventMBT.GetMilliseconds() - (0.5d * PerfectWindowMillis);
        cueBug.PerfectWindowEnd = cueBug.bmEvent.eventMBT.GetMilliseconds() + (0.5d * PerfectWindowMillis);
    }

    //this makes our beatmap from the json file
    void PrepareData(string name)
    {
        currentSong = SongSource.getSong(name);
        if (currentSong.tracks.Length > 0)
        {

            for (int i = 0; i < currentSong.tracks.Length; i++)
            {
                //go through each track first
                for (int j = 0; j < currentSong.tracks[i].notes.Length; j++)
                {

                    //this will convert the note time from seconds to beats
                    currentSong.tracks[i].notes[j].beatTime = currentSong.tracks[i].notes[j].time * currentSong.header.bpm / 60f;
                    //assuming 4/4 time for now
                    int measure = Mathf.FloorToInt(currentSong.tracks[i].notes[j].beatTime / 4f);
                    int beat = Mathf.FloorToInt(currentSong.tracks[i].notes[j].beatTime - (measure * 4));
                    int tick = Mathf.FloorToInt((currentSong.tracks[i].notes[j].beatTime - (measure * 4) - beat) * 96);
                    currentSong.tracks[i].notes[j].mbtValue = new MBT(measure, beat, tick);

                    //assign this midi note to a beatmap event
                    BeatMapEvent bmEvent = new BeatMapEvent();
                    bmEvent.eventMBT = currentSong.tracks[i].notes[j].mbtValue;
                    bmEvent.eventMBT.Measure += 1;
                    bmEvent.eventMBT.Beat += 1;
                    bmEvent.eventMBT.Tick += 1;

                    //depending on which note it is, assign the prefab accordingly

                    if (!usingInputClass)
                    {
                        switch (currentSong.tracks[i].notes[j].name)
                        {
                            case "C1":
                                bmEvent.inputKey = KeyCode.R;
                                break;
                            case "C#1":
                                bmEvent.inputKey = KeyCode.G;
                                break;
                            case "D1":
                                bmEvent.inputKey = KeyCode.B;
                                break;
                        }
                    }
                    else
                    {
                        foreach (BeatmapPair bmPair in beatmapParser.beatmapPairs)
                        {
                            if (bmPair.midiNote == currentSong.tracks[i].notes[j].name)
                            {
                                bmEvent.unityInput = bmPair.unityInput;
                                bmEvent.beatCueOffset = bmPair.beatCueOffset;
                            }
                        }

                    }

                    beatEvents.Add(bmEvent);

                }
            }
        }


    }




}
