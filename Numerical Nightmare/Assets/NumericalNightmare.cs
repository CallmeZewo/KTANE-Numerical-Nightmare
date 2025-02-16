using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using Math = ExMath;

public class NumericalNightmare : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;

    public KMSelectable[] Buttons;
    public TextMesh[] Displays;

    public Transform[] Dials;

    public Transform Hatch;

    public GameObject[] WireABC123;

    //Constants
    static Dictionary<string, int> SymbolDictionary = new Dictionary<string, int>
    {
        { "ᢰ", 7 }, { "ᢱ", 3 }, { "ᢲ", 9 }, { "ᢳ", 2 }, { "ᢴ", 5 },
        { "ᢵ", 0 }, { "ᢶ", 8 }, { "ᢷ", 4 }, { "ᢸ", 6 }, { "ᢹ", 1 },
        { "ᢺ", 3 }, { "ᢻ", 7 }, { "ᢼ", 0 }, { "ᢽ", 9 }, { "ᢾ", 2 },
        { "ᢿ", 5 }, { "ᣀ", 4 }, { "ᣁ", 6 }, { "ᣂ", 8 }, { "ᣃ", 1 },
        { "ᣄ", 9 }, { "ᣅ", 2 }, { "ᣆ", 5 }, { "ᣇ", 3 }, { "ᣈ", 7 },
        { "ᣉ", 0 }, { "ᣊ", 6 }, { "ᣋ", 4 }, { "ᣌ", 8 }, { "ᣍ", 1 },
        { "ᣎ", 2 }, { "ᣏ", 9 }, { "ᣐ", 5 }, { "ᣑ", 7 }, { "ᣒ", 3 },
        { "ᣓ", 0 }, { "ᣠ", 8 }, { "ᣡ", 4 }, { "ᣢ", 6 }, { "ᣣ", 1 },
        { "ᣤ", 5 }, { "ᣥ", 9 }, { "ᣦ", 2 }, { "ᣧ", 8 }, { "ᣨ", 7 },
        { "ᣩ", 3 }, { "ᣪ", 0 }, { "ᣫ", 6 }, { "ᣬ", 4 }, { "ᣭ", 1 },
        { "ᣮ", 7 }, { "ᣯ", 2 }, { "ᣰ", 9 }, { "ᣱ", 5 }, { "ᣲ", 8 }
    };

    static Dictionary<int, float> DialPositionDictionary = new Dictionary<int, float>
    {
        { 0, 0f },
        { 1, 45f },
        { 2, 90f },
        { 3, 135f },
        { 4, 180f },
        { 5, 225f },
        { 6, 270f },
        { 7, 315f }
    };

    char FirstSerialLetter;
    string WireABCConst;

    static Quaternion targetQuaternionOpen = Quaternion.Euler(new Vector3(0, -105f, 0));
    static Quaternion targetQuaternionClose = Quaternion.Euler(new Vector3(0, 0f, 0));

    const float RotationsSpeed = 10f;

    //Variables
    float FaultyProbability = 1.5625f;
    int StagesDone = 0;

    bool StageIsWorking = true;
    bool SymbolsAreWorking = true;
    bool FinalInput = false;
    bool FaultyThisStage = false;
    public bool HatchOpen = false;

    float Angle;
    int LastAndCurrentFaultyStage = 0;
    string FaultySymbol;
    int LastValidAndThisStageFirstSymbolValue;
    int LastValidAndThisStageSecondSymbolValue;

    string CorrectWireABC;
    int CorrectWire123;

    string LastWireABC = "A";
    int LastWire123 = 1;

    int Dial1Goal;
    int Dial2Goal;
    int Dial3Goal;

    List<int> PinList = new List<int>();
    List<int> FinalInputList = new List<int>();
    List<int> CurrentDialPositions = new List<int> { 0, 0, 0 };

    //Boss mod shit
    public static string[] ignoredModules = null;
    int SolvableModCount = 8;
    public int SolvedModCount = 0;
    int Stage = 0;
    bool WaitForModCount;
    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += Activate;

        foreach (KMSelectable button in Buttons) {
            button.OnInteract += delegate () { PressHandler(button); return false; };
        }
        
        //button.OnInteract += delegate () { buttonPress(); return false; };

        if (ignoredModules == null)
        {
            ignoredModules = GetComponent<KMBossModule>().GetIgnoredModules("Numerical Nightmare", new string[] {
                "14",
                "42",
                "501",
                "A>N<D",
                "Bamboozling Time Keeper",
                "Black Arrows",
                "Brainf---",
                "The Board Walk",
                "Busy Beaver",
                "Don't Touch Anything",
                "Floor Lights",
                "Forget Any Color",
                "Forget Enigma",
                "Forget Ligma",
                "Forget Everything",
                "Forget Infinity",
                "Forget It Not",
                "Forget Maze Not",
                "Forget Me Later",
                "Forget Me Not",
                "Forget Perspective",
                "Forget The Colors",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "Iconic",
                "Keypad Directionality",
                "Kugelblitz",
                "Multitask",
                "OmegaDestroyer",
                "OmegaForest",
                "Organization",
                "Password Destroyer",
                "Purgatory",
                "Reporting Anomalies",
                "RPS Judging",
                "Security Council",
                "Shoddy Chess",
                "Simon Forgets",
                "Simon's Stages",
                "Souvenir",
                "Speech Jammer",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn The Key",
                "The Twin",
                "Übermodule",
                "Ultimate Custom Night",
                "The Very Annoying Button",
                "Whiteout"
            });
        }
    }

    void OnDestroy()
    { //Shit you need to do when the bomb ends

    }

    void Activate()
    { //Shit that should happen when the bomb arrives (factory)/Lights turn on

    }

    void Start()
    { //Shit

        //
        ////Boss Things
        //

        WaitForModCount = true;

        //
        //// Setup Variables
        //

        FirstSerialLetter = Bomb.GetSerialNumberLetters().First();
        WireABCConstCalc();

        //
        //// Setup First Stage 0
        //

        StageAdvanceHandler();

        //
        //// Setup Wires ABC 123
        //

        foreach (GameObject wire in WireABC123)
        {
            wire.SetActive(false);
        }
        WireABC123[0].SetActive(true);

    }

    void Update()
    { //Shit that happens at any point after initialization


        if (FinalInput == true)
        {
            for (int i = 0; i < FinalInputList.Count; i++)
            {
                Debug.Log(FinalInputList[i]);
            }
            if (FinalInputList.Count < 1)
            {
                Solve();
            }
            return;
        }

        if (HatchOpen)
        {
            Hatch.localRotation = Quaternion.Slerp(Hatch.localRotation, targetQuaternionOpen, RotationsSpeed * Time.deltaTime);
        }
        else
        {
            Hatch.localRotation = Quaternion.Slerp(Hatch.localRotation, targetQuaternionClose, RotationsSpeed * 1.3f * Time.deltaTime);
        }


        //SolvableModCount = Bomb.GetSolvableModuleNames().Count(x => !ignoredModules.Contains(x));

        //SolvedModCount = Bomb.GetSolvedModuleNames().Count(x => !ignoredModules.Contains(x));



        if (SolvedModCount == SolvableModCount)
        { //Input End Result
            Displays[0].text = "";
            Displays[1].text = "";
            Displays[2].text = "";
            FinalInput = true;

            Debug.Log("All solvable module done, ready for final input");

            return;
        }
        if (SolvedModCount > Stage)
        { //Put whatever your mod is supposed to do after a solve here. If you want a delay of solves for the purposes of TP, make it a coroutine.

            //Stage is 0 indexed, so adjust what you need for your specific circumstances.
            Stage++;
            StageAdvanceHandler();
            StagesDone++;
        }
    }

    #region Display Stuff

    void DisplayRandomSymbolsWorking()
    {
        SymbolsAreWorking = true;

        Displays[1].text = SymbolDictionary.Keys.PickRandom();
        Displays[2].text = SymbolDictionary.Keys.Where(x => x != Displays[1].text).PickRandom();

        LastValidAndThisStageFirstSymbolValue = SymbolDictionary[Displays[1].text];
        LastValidAndThisStageSecondSymbolValue = SymbolDictionary[Displays[2].text];
    }

    void DisplayAndStoreRandomSymbolsFaulty()
    {

        SymbolsAreWorking = false;

        switch (Rnd.Range(1, 3))
        {
            case 1:
                //No Symbol
                Displays[1].text = "";
                Displays[2].text = "";
                FaultySymbol = "";
                break;
            case 2:
                //Only First Symbol
                Displays[1].text = SymbolDictionary.Keys.PickRandom();
                Displays[2].text = "";
                FaultySymbol = Displays[1].text;
                break;
            case 3:
                //Only SecondSymbol
                Displays[1].text = "";
                Displays[2].text = SymbolDictionary.Keys.PickRandom();
                FaultySymbol = Displays[2].text;
                break;
        }

    }

    void DisplayStageWorking()
    {
        StageIsWorking = true;

        Displays[0].text = Stage.ToString();
    }

    void DisplayAndStoreStageFaulty()
    {
        StageIsWorking = false;

        Displays[0].text = (Stage + Rnd.Range(-(Stage/3), 10)).ToString();
        LastAndCurrentFaultyStage = int.Parse(Displays[0].text);
    }

    #endregion

    #region Demonic Dials

    void DialGoalPosition()
    {
        Dial1Goal = (LastValidAndThisStageFirstSymbolValue * LastValidAndThisStageSecondSymbolValue) % 8;
        Dial2Goal = (LastValidAndThisStageFirstSymbolValue + LastValidAndThisStageSecondSymbolValue) % 8;
        Dial3Goal = (Dial1Goal + Dial2Goal) % 8;
    }

    void MoveDials(int dialIndex)
    {
        int newPositionIndex = CurrentDialPositions[dialIndex] + 1;
        if (newPositionIndex == DialPositionDictionary.Count)
        {
            newPositionIndex = 0;
        }
        Angle = DialPositionDictionary[newPositionIndex];
        Dials[dialIndex].rotation = Quaternion.Euler(-90f, 0f, Angle);
        CurrentDialPositions[dialIndex] = newPositionIndex;
    }

    #endregion

    #region Menacing Microchip

    void PinsToPress()
    {
        int faultySymbolValue = SymbolDictionary.ContainsKey(FaultySymbol) ? SymbolDictionary[FaultySymbol] : 0;
        int startNumber = LastAndCurrentFaultyStage + faultySymbolValue;
        int repeat = 0;

        while(startNumber > 0 || repeat == 3)
        {
            int modnumber = (startNumber % 20 == 0) ? 1 : startNumber % 20;
            PinList.Add(modnumber);
            startNumber = -3;
            repeat++;
        }
    }

    void PinPresses(int pinIndex)
    {
        if (PinList.Count < 1)
        {
            return;
        }

        if (pinIndex == PinList.First())
        {
            PinList.RemoveAt(0);
        }
        else
        {
            Strike();
        }
    }

    #endregion

    #region Wicked Wires

    void WireCombination()
    {
        //Last 3 stages times faulty stage mod 100
        int stageNumber = (((Stage - 1) + (Stage - 2) + (Stage - 3)) * LastAndCurrentFaultyStage % 100);
        //If less than 10 add 15
        stageNumber = (stageNumber < 10) ? stageNumber + 15 : stageNumber;
        //Take digital root then mod 3, if 0 add 1
        CorrectWire123 = Math.DRoot(stageNumber) % 3;

        string compare = "ABC";
        if (compare.Contains(FirstSerialLetter))
        {
            CorrectWireABC = FirstSerialLetter.ToString();
        }
        else
        {
            CorrectWireABC = WireABCConst;
        }
    }

    void WireABCConstCalc()
    {
        int alphabeticPosition = FirstSerialLetter - 'A' + 1;
        alphabeticPosition = (alphabeticPosition % 3 == 0) ? 1 : alphabeticPosition % 3;
        WireABCConst = ('A' + alphabeticPosition - 1).ToString();
    }

    void SwitchWires()
    {

        var onWire = WireABC123.First(x => x.activeInHierarchy == true);

        switch (LastWireABC + LastWire123.ToString())
        {
            case "A1":
                onWire.SetActive(false);
                WireABC123[0].SetActive(true);
                break;
            case "A2":
                onWire.SetActive(false);
                WireABC123[1].SetActive(true);
                break;
            case "A3":
                onWire.SetActive(false);
                WireABC123[2].SetActive(true);
                break;
            case "B1":
                onWire.SetActive(false);
                WireABC123[3].SetActive(true);
                break;
            case "B2":
                onWire.SetActive(false);
                WireABC123[4].SetActive(true);
                break;
            case "B3":
                onWire.SetActive(false);
                WireABC123[5].SetActive(true);
                break;
            case "C1":
                onWire.SetActive(false);
                WireABC123[6].SetActive(true);
                break;
            case "C2":
                onWire.SetActive(false);
                WireABC123[7].SetActive(true);
                break;
            case "C3":
                onWire.SetActive(false);
                WireABC123[8].SetActive(true);
                break;
        }
    }

    #endregion

    void StageAdvanceHandler()
    {
        if (Rnd.Range(0f, 100f) < FaultyProbability && StagesDone > 2 && FaultyThisStage == false)
        {
            switch (Rnd.Range(1,3))
            {
                case 1:
                    //Symbols and Stage are Faulty
                    DisplayAndStoreRandomSymbolsFaulty();
                    DisplayAndStoreStageFaulty();
                    WireCombination();
                    break;
                case 2:
                    //Only Symblos are Faulty
                    DisplayAndStoreRandomSymbolsFaulty();
                    DisplayStageWorking();
                    DialGoalPosition();
                    break;
                case 3:
                    //Only Stage is Faulty
                    DisplayRandomSymbolsWorking();
                    DisplayAndStoreStageFaulty();
                    break;
            }

            FaultyProbability = 3.125f;
            FaultyThisStage = true;
        }
        else
        {
            DisplayRandomSymbolsWorking();
            DisplayStageWorking();
            FaultyProbability *= 2;
            FaultyThisStage = false;
            FinalInputBuilding();
        }

        Debug.Log("You are at Stage: " + Stage);
        Debug.Log("Your FaultyProbability is at: " + FaultyProbability);
        Debug.Log("Do your stages work? " + StageIsWorking);
        Debug.Log("Do your symbols work? " + SymbolsAreWorking);

    }

    //Handling Presses
    void PressHandler(KMSelectable button)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);

        if (ModuleSolved)
        {
            return;
        }

        for (int i = 0; i < Buttons.Length; i++)
        {
            if (button == Buttons[i])
            {
                switch (i)
                {
                    case 0:
                        SolveWithFinalInput(0);
                        break;
                    case 1:
                        SolveWithFinalInput(1);
                        break;
                    case 2:
                        SolveWithFinalInput(2);
                        break;
                    case 3:
                        SolveWithFinalInput(3);
                        break;
                    case 4:
                        SolveWithFinalInput(4);
                        break;
                    case 5:
                        SolveWithFinalInput(5);
                        break;
                    case 6:
                        SolveWithFinalInput(6);
                        break;
                    case 7:
                        SolveWithFinalInput(7);
                        break;
                    case 8:
                        SolveWithFinalInput(8);
                        break;
                    case 9:
                        SolveWithFinalInput(9);
                        break;
                    case 10:
                        SolveWithFinalInput(-1);
                        break;
                    case 11:
                        ToggleHatch();
                        break;
                    case 12:
                        PinPresses(1);
                        break;
                    case 13:
                        PinPresses(2);
                        break;
                    case 14:
                        PinPresses(3);
                        break;
                    case 15:
                        PinPresses(4);
                        break;
                    case 16:
                        PinPresses(5);
                        break;
                    case 17:
                        PinPresses(6);
                        break;
                    case 18:
                        PinPresses(7);
                        break;
                    case 19:
                        PinPresses(8);
                        break;
                    case 20:
                        PinPresses(9);
                        break;
                    case 21:
                        PinPresses(10);
                        break;
                    case 22:
                        PinPresses(11);
                        break;
                    case 23:
                        PinPresses(12);
                        break;
                    case 24:
                        PinPresses(13);
                        break;
                    case 25:
                        PinPresses(14);
                        break;
                    case 26:
                        PinPresses(15);
                        break;
                    case 27:
                        PinPresses(16);
                        break;
                    case 28:
                        PinPresses(17);
                        break;
                    case 29:
                        PinPresses(18);
                        break;
                    case 30:
                        PinPresses(19);
                        break;
                    case 31:
                        PinPresses(20);
                        break;
                    case 32:
                        MoveDials(0);
                        break;
                    case 33:
                        MoveDials(1);
                        break;
                    case 34:
                        MoveDials(2);
                        break;
                    case 35:
                        LastWireABC = "A";
                        SwitchWires();
                        break;
                    case 36:
                        LastWireABC = "B";
                        SwitchWires();
                        break;
                    case 37:
                        LastWireABC = "C";
                        SwitchWires();
                        break;
                    case 38:
                        LastWire123 = 1;
                        SwitchWires();
                        break;
                    case 39:
                        LastWire123 = 2;
                        SwitchWires();
                        break;
                    case 40:
                        LastWire123 = 3;
                        SwitchWires();
                        break;
                }
            }
        }
    }

    //Check Final Input
    void SolveWithFinalInput(int keypad)
    {
        if (!FinalInput)
        {
            return;
        }
        Debug.Log(FinalInputList[0]);
        Debug.Log(keypad);
    }

    //Making the Sequence
    void FinalInputBuilding()
    {
        if (Math.DRoot(Stage) > 5)
        {
            FinalInputList.Add((LastValidAndThisStageFirstSymbolValue + LastValidAndThisStageSecondSymbolValue + Stage) % 10);
        }
        else if (Stage % 5 == 0)
        {
            FinalInputList.Add((LastValidAndThisStageFirstSymbolValue + LastValidAndThisStageSecondSymbolValue + LastAndCurrentFaultyStage) % 10);
        }
        else
        {
            FinalInputList.Add((LastValidAndThisStageFirstSymbolValue * LastValidAndThisStageSecondSymbolValue) % 10);
        }
    }

    //Hatch Bool
    void ToggleHatch()
    {
        HatchOpen = !HatchOpen;
    }

    //Solve and Stuff

    void Solve()
    {
        GetComponent<KMBombModule>().HandlePass();
        WaitForModCount = false;
    }

    void Strike()
    {
        GetComponent<KMBombModule>().HandleStrike();
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
    }
}