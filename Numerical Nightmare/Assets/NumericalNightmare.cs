using System.Text.RegularExpressions;
using System.Collections.Generic;
using Rnd = UnityEngine.Random;
using System.Collections;
using Math = ExMath;
using System.Linq;
using UnityEngine;
using KModkit;
using System;

public class NumericalNightmare : MonoBehaviour
{
    //Unity Publics
    public GameObject[] WireABC123;
    public KMSelectable[] Buttons;
    public TextMesh[] Displays;
    public Transform[] Dials;
    public Transform Hatch;
    public KMBombInfo Bomb;
    public KMAudio Audio;

    //Dictionarys
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

    static Dictionary<int, string> EasterEggSounds = new Dictionary<int, string>
    {
        {0, "Lucky!"},
        {1, "DoubleGolden1UP" },
    };

    //Quaternions for Hatch Movement
    static Quaternion targetQuaternionOpen = Quaternion.Euler(new Vector3(0, -105f, 0));
    static Quaternion targetQuaternionClose = Quaternion.Euler(new Vector3(0, 0f, 0));

    //Variabels
    bool SymbolsAreWorking = true;  //Are the symbols working?
    bool FaultyThisStage = false;   //Was/Is there a fault this stage?
    bool StageIsWorking = true;     //Is the stage working?
    bool DialsBroken = false;       //Are the dials broken?
    bool WiresBroken = false;       //Are the wires broken?
    bool ChipBroken = false;        //Is the chip broken?
    bool FinalInput = false;        //Module ready for final input?
    bool HatchOpen = false;         //Hatch opened or closed?

    float FaultyProbability = 1.5625f;  //Chance for fault
    float RotationSpeed = 10f;          //Hatch default rotationspeed
    float Angle;                        //Angle for Dial turning

    int LastValidAndThisStageSecondSymbolValue; //Last fault stage symbols will be safed as soon as a fault symbol gets generated
    int LastValidAndThisStageFirstSymbolValue;  //so its also the current one aswell
    int LastAndCurrentFaultyStage = 0;          //Same goes with the stages
    int LastInputNumber;                        //Last Inputed number for the final sequence
    int CorrectWire123;                         //Correct wire connection points (1, 2, 3) for current fault stage
    int CorrectWireABC;                         //Same for connections (A, B, C)
    int StagesDone = 0;                         //Completed Stages
    int LastWire123;                            //Last Selected connection from (1, 2, 3)
    int LastWireABC;                            //Last Selected connection from (A, B, C)
    int Dial1Goal;                              //End position of dial 1
    int Dial2Goal;                              //and dial 2
    int Dial3Goal;                              //and dial 3

    char FirstSerialLetter;

    string CurrentActiveWire;
    string FaultySymbol;
    string Symbol1Temp;
    string Symbol2Temp;

    bool final = true;

    List<int> CurrentDialPositions = new List<int> { 0, 0, 0 };
    List<int> EasterEggKeypadList = new List<int>();
    List<int> FinalInputList = new List<int>();
    List<int> PinList = new List<int>();

    //Boss mod shit
    public static string[] ignoredModules = null;
    static int ModuleIdCounter = 1;
    public int SolvedModCount = 0;
    private bool ModuleSolved;
    int SolvableModCount = 10;
    bool WaitForModCount;
    int Stage = 0;
    int ModuleId;

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
                "Whiteout",
                "Numerical Nightmare"
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
        LastInputNumber = -2;

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

    }

    void Update()
    { //Shit that happens at any point after initialization

        if (FinalInput == true)
        {
            if (final)
            {
                for (int i = 0; i < FinalInputList.Count; i++)
                {
                    Debug.Log(FinalInputList[i]);
                }
                final = false;
            }
            if (FinalInputList.Count < 1)
            {
                Solve();
            }
            return;
        }

        if (HatchOpen)
        {
            Hatch.localRotation = Quaternion.Slerp(Hatch.localRotation, targetQuaternionOpen, RotationSpeed * Time.deltaTime);
        }
        else
        {
            Hatch.localRotation = Quaternion.Slerp(Hatch.localRotation, targetQuaternionClose, RotationSpeed * 1.3f * Time.deltaTime);
        }


        SolvableModCount = Bomb.GetSolvableModuleNames().Count(x => !ignoredModules.Contains(x));

        SolvedModCount = Bomb.GetSolvedModuleNames().Count(x => !ignoredModules.Contains(x));



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
            Stage++;
            StageAdvanceHandler();
            StagesDone++;
            ResetInside();
            if (HatchOpen == true) HatchOpen = false;
        }
    }

    #region Display Stuff

    void DisplayRandomSymbolsWorking()
    {
        SymbolsAreWorking = true;

        Displays[1].text = Symbol1Temp;
        Displays[2].text = Symbol2Temp;
    }

    void GetRandomSymbolsReady()
    {
        Symbol1Temp = SymbolDictionary.Keys.PickRandom();
        Symbol2Temp = SymbolDictionary.Keys.Where(x => x != Symbol1Temp).PickRandom();

        LastValidAndThisStageFirstSymbolValue = SymbolDictionary[Symbol1Temp];
        LastValidAndThisStageSecondSymbolValue = SymbolDictionary[Symbol2Temp];
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

        int displayTemp = (Stage + Rnd.Range(-(Stage / 3), 10));
        if (displayTemp == Stage)
        {
            Displays[0].text = (displayTemp - 3).ToString();
        }
        Displays[0].text = displayTemp.ToString();
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
        Dials[dialIndex].localRotation = Quaternion.Euler(-90f, 0f, Angle);
        CurrentDialPositions[dialIndex] = newPositionIndex;

        if (CurrentDialPositions[0] == Dial1Goal && CurrentDialPositions[1] == Dial2Goal && CurrentDialPositions[2] == Dial3Goal)
        {
            DialsBroken = false;
        }
        else
        {
            DialsBroken = true;
        }
    }

    #endregion

    #region Menacing Microchip

    void PinsToPress()
    {
        int faultySymbolValue = SymbolDictionary.ContainsKey(FaultySymbol) ? SymbolDictionary[FaultySymbol] : 0;
        int startNumber = LastAndCurrentFaultyStage + faultySymbolValue;

        for (int repeat = 0; repeat < 3; repeat++)
        {
            if (startNumber < 0)
            {
                return;
            }
            int modnumber = (startNumber % 20 == 0) ? 1 : startNumber % 20;
            PinList.Add(modnumber);
            startNumber -= 3;
        }
    }

    void PinPresses(int pinIndex)
    {
        for (int i = 0; i < PinList.Count; i++)
        {
            Debug.Log(PinList[i]);
        }

        if (PinList.Count == 0)
        {
            Strike();
            return;
        }

        if (pinIndex == PinList[0])
        {
            PinList.RemoveAt(0);
        }
        else
        {
            Strike();
        }

        if (PinList.Count <= 0)
        {
            ChipBroken = false;
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
        CorrectWire123 = (Math.DRoot(stageNumber) % 3 == 0) ? 1 : Math.DRoot(stageNumber) % 3;

        int modResult = (FirstSerialLetter - 'A' + 1 + LastAndCurrentFaultyStage) % 3;
        CorrectWireABC = (modResult == 0) ? 1 : modResult;

        Debug.Log(CorrectWireABC.ToString() + CorrectWire123.ToString());
    }

    void SwitchWires()
    {
        CurrentActiveWire = LastWireABC.ToString() + LastWire123.ToString();
        for (int i = 0; i < WireABC123.Length; i++)
        {
            WireABC123[i].SetActive(false);
        }
        switch (CurrentActiveWire)
        {
            case "11":
                WireABC123[0].SetActive(true);
                break;
            case "12":
                WireABC123[1].SetActive(true);
                break;
            case "13":
                WireABC123[2].SetActive(true);
                break;
            case "21":
                WireABC123[3].SetActive(true);
                break;
            case "22":
                WireABC123[4].SetActive(true);
                break;
            case "23":
                WireABC123[5].SetActive(true);
                break;
            case "31":
                WireABC123[6].SetActive(true);
                break;
            case "32":
                WireABC123[7].SetActive(true);
                break;
            case "33":
                WireABC123[8].SetActive(true);
                break;
        }

        if (CurrentActiveWire == (CorrectWireABC.ToString() + CorrectWire123.ToString()))
        {
            WiresBroken = false;
        }
        else
        {
            WiresBroken = true;
        }
    }

    #endregion

    #region Stage Handling

    void StageAdvanceHandler()
    {
        GetRandomSymbolsReady();
        FinalInputBuilding();
        if (Rnd.Range(0f, 100f) < FaultyProbability && StagesDone > 2 && FaultyThisStage == false)
        {
            HatchOpen = false;

            switch (Rnd.Range(1,4))
            {
                case 1:
                    //Symbols and Stage are Faulty
                    DisplayAndStoreRandomSymbolsFaulty();
                    DisplayAndStoreStageFaulty();
                    PinsToPress();
                    ChipBroken = true;
                    break;
                case 2:
                    //Only Symblos are Faulty
                    DisplayAndStoreRandomSymbolsFaulty();
                    DisplayStageWorking();
                    DialGoalPosition();
                    DialsBroken = true;
                    break;
                case 3:
                    //Only Stage is Faulty
                    DisplayRandomSymbolsWorking();
                    DisplayAndStoreStageFaulty();
                    WireCombination();
                    WiresBroken = true;
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
        }

        Debug.Log("You are at Stage: " + Stage);
        Debug.Log("Your FaultyProbability is at: " + FaultyProbability);
        Debug.Log("Do your stages work? " + StageIsWorking);
        Debug.Log("Do your symbols work? " + SymbolsAreWorking);

    }

    #endregion

    #region Reset Inside

    void ResetInside()
    {
        //Wires
        LastWire123 = 0;
        LastWireABC = 0;

        for (int i = 0; i < WireABC123.Length; i++)
        {
            WireABC123[i].SetActive(false);
        }

        //Dials
        for (int i = 0; i < 3; i++)
        {
            Dials[i].localRotation = Quaternion.Euler(-90f, 0f, 0);
            CurrentDialPositions[i] = 0;
        }

    }

    #endregion

    #region Input Handling

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
                        EasterEggs(0);
                        break;
                    case 1:
                        SolveWithFinalInput(1);
                        EasterEggs(1);
                        break;
                    case 2:
                        SolveWithFinalInput(2);
                        EasterEggs(2);
                        break;
                    case 3:
                        SolveWithFinalInput(3);
                        EasterEggs(3);
                        break;
                    case 4:
                        SolveWithFinalInput(4);
                        EasterEggs(4);
                        break;
                    case 5:
                        SolveWithFinalInput(5);
                        EasterEggs(5);
                        break;
                    case 6:
                        SolveWithFinalInput(6);
                        EasterEggs(6);
                        break;
                    case 7:
                        SolveWithFinalInput(7);
                        EasterEggs(7);
                        break;
                    case 8:
                        SolveWithFinalInput(8);
                        EasterEggs(8);
                        break;
                    case 9:
                        SolveWithFinalInput(9);
                        EasterEggs(9);
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
                        LastWireABC = 1;
                        SwitchWires();
                        break;
                    case 36:
                        LastWireABC = 2;
                        SwitchWires();
                        break;
                    case 37:
                        LastWireABC = 3;
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

    #endregion

    #region Final Input

    //Check Final Input
    void SolveWithFinalInput(int keypad)
    {
        if (!FinalInput)
        {
            return;
        }

        if (keypad == FinalInputList[0] && keypad != LastInputNumber)
        {
            FinalInputList.RemoveAt(0);
        }
        else if (keypad == -1 && FinalInputList[0] == LastInputNumber)
        {
            FinalInputList.RemoveAt(0);
        }
        else
        {
            Strike();
        }

        if (keypad != -1)
        {
            LastInputNumber = keypad;
        }
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

    #endregion

    #region Hatch and Fix

    //Hatch / Confirm Fix
    void ToggleHatch()
    {
        if (FinalInput)
        {
            HatchOpen = false;
            Audio.PlaySoundAtTransform("HatchCloseSound", Hatch);
            return;
        }

        if (HatchOpen == false)
        {
            Audio.PlaySoundAtTransform("HatchOpenSound", Hatch);
            HatchOpen = true;
        }
        else
        {
            Audio.PlaySoundAtTransform("HatchCloseSound", Hatch);
            HatchOpen = false;
            if (FaultyThisStage == true)
            {
                CheckFix();
            }
        }
    }

    void CheckFix()
    {
        if (!ChipBroken && !DialsBroken && !WiresBroken)
        {
            StageAdvanceHandler();
        }
        else
        {
            Strike();
        }
    }

    #endregion

    #region Easter Eggs
    void EasterEggs(int keypad)
    {
        EasterEggKeypadList.Add(keypad);

        if (EasterEggKeypadList.Count < 4 || Stage != 0)
        {
            return;
        }

        //Luck! - Type in the year that the first paper mario came out in.
        if (EasterEggKeypadList.SequenceEqual(new List<int> { 2, 0, 0, 0}))
        {
            Audio.PlaySoundAtTransform(EasterEggSounds[0], Hatch);
        }

        //Double Golden 1-UP - Type in the year that Celeste came out in.
        if (EasterEggKeypadList.SequenceEqual(new List<int> { 2, 0, 1, 8 }))
        {
            Audio.PlaySoundAtTransform(EasterEggSounds[1], Hatch);
        }

        if (EasterEggKeypadList.Count >= 4)
        {
            EasterEggKeypadList.RemoveRange(0, 4);
        }
    }


    #endregion

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