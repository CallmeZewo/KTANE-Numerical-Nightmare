﻿using System.Text.RegularExpressions;
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

    //Quaternions for Hatch Movement
    static Quaternion targetQuaternionOpen = Quaternion.Euler(new Vector3(0, -105f, 0));
    static Quaternion targetQuaternionClose = Quaternion.Euler(new Vector3(0, 0f, 0));

    //Variabels
    bool SymbolsAreWorking = true;  //Are the symbols working?
    bool FaultyThisStage = false;   //Was/Is there a fault this stage?
    bool BuildThisStage = false;    //Stage value calculated?
    bool StageIsWorking = true;     //Is the stage working?
    bool DialsBroken = false;       //Are the dials broken?
    bool WiresBroken = false;       //Are the wires broken?
    bool ChipBroken = false;        //Is the chip broken?
    bool FinalInput = false;        //Module ready for final input?
    bool HatchOpen = false;         //Hatch opened or closed?

    float FaultyProbability = 1.5625f;  //Chance for fault
    float RotationSpeed = 10f;          //Hatch default rotationspeed
    float Angle;                        //Angle for Dial turning

    int LastValidStageSecondSymbolValue;        //Last fault stage symbols will be safed as soon as a fault symbol gets generated
    int LastValidStageFirstSymbolValue;         //so its also the current one aswell
    int LastAndCurrentFaultyStage = 0;          //Same goes with the stages
    int ThisStageSecondSymbolValue;             //This is just the current symbol that is gonna get displayed this stage
    int ThisStageFirstSymbolValue;              //not depended on fault
    int totalinputcountones = 0;
    int totalinputcounttens = 0;
    int FinalInputIndex = 0;                    //Wich stage is being input into final
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

    string CorrectWireABCString;
    string LastWireABCString;
    string CurrentActiveWire;
    string FinalInputHolder = "";
    string FaultySymbol;
    string Symbol1Temp;
    string Symbol2Temp;

    bool final = true;

    List<List<string>> StageSymbolList = new List<List<string>>();
    List<int> CurrentDialPositions = new List<int> { 0, 0, 0 };
    List<int> FinalInputList = new List<int>();
    List<int> PinList = new List<int>();

    List<string> DashList = new List<string>();

    //Boss mod shit
    public static string[] ignoredModules = null;
    static int ModuleIdCounter = 1;
    public int SolvedModCount = 0;
    private bool ModuleSolved;
    int SolvableModCount = 15;
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
        //// Setup Variables
        //

        FirstSerialLetter = Bomb.GetSerialNumberLetters().First();  //Get Last Serialnumber from bomb
        LastInputNumber = -2;                                       //Last Inputted number for final input (for skip function)

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
        if (ModuleSolved) return;

        if (FinalInput == true)
        {
            if (final)
            {
                string formattedString = "";
                for (int i = 0; i < FinalInputList.Count; i++)
                {
                    formattedString += FinalInputList[i];

                    // Add a space after every third number (except the last one)
                    if ((i + 1) % 3 == 0 && i != FinalInputList.Count - 1)
                    {
                        formattedString += " ";
                    }
                }
                Debug.LogFormat("[Numerical Nightmare #{0}] Your final input sequence is: {1}", ModuleId, formattedString);
                final = false;
            }

            if (FinalInputList.Count < 1)
            {
                Solve();
            }
            else if (StagesDone == 0)
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

        if (!StageIsWorking)
        {
            Displays[1].text = SymbolDictionary.Keys.PickRandom();
            Displays[2].text = SymbolDictionary.Keys.Where(x => x != Displays[1].text).PickRandom();
            return;
        }

        Displays[1].text = Symbol1Temp;
        Displays[2].text = Symbol2Temp;
    }

    void GetRandomSymbolsReady()
    {
        Symbol1Temp = SymbolDictionary.Keys.PickRandom();
        Symbol2Temp = SymbolDictionary.Keys.Where(x => x != Symbol1Temp).PickRandom();

        StageSymbolList.Add(new List<string> { Symbol1Temp, Symbol2Temp });

        if (Stage != 0)
        {
            LastValidStageFirstSymbolValue = ThisStageFirstSymbolValue;
            LastValidStageSecondSymbolValue = ThisStageSecondSymbolValue;
        }

        ThisStageFirstSymbolValue = SymbolDictionary[Symbol1Temp];
        ThisStageSecondSymbolValue = SymbolDictionary[Symbol2Temp];
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
                //Only Second Symbol
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
            displayTemp -= 3;
        }
        Displays[0].text = displayTemp.ToString();
        LastAndCurrentFaultyStage = displayTemp;
    }

    void HandleDisplayOnFinalInput(string input)
    {
        Displays[0].characterSize = 0.0029f;
        FinalInputHolder += input;
        if (FinalInputHolder.Count() > 12)
        {
            string temp = "";
            for (int i = 1; i < 13; i++)
            {
                temp += FinalInputHolder[i];
            }
            FinalInputHolder = temp;
        }
        string formattedstring = "";
        int index = 0;
        foreach (char c in FinalInputHolder)
        {
            if (index % 3 == 0 && index != 12 && index > 0)
            {
                formattedstring += " ";
            }
            index++;
            formattedstring += c;
        }

        FinalInputList.RemoveAt(0);
        FinalInputIndex++;
        Displays[0].text = formattedstring;
        Displays[1].text = totalinputcountones.ToString();
        Displays[2].text = totalinputcounttens.ToString();
        totalinputcountones++;
        if (totalinputcountones > 9)
        {
            totalinputcountones = 0;
            totalinputcounttens++;
        }
    }

    #endregion

    #region Demonic Dials

    void DialGoalPosition()
    {
        Dial1Goal = (LastValidStageFirstSymbolValue * LastValidStageSecondSymbolValue) % 8;
        Dial2Goal = (LastValidStageFirstSymbolValue + LastValidStageSecondSymbolValue) % 8;
        Dial3Goal = (Bomb.GetIndicators().Count() + LastAndCurrentFaultyStage) % 8;

        Debug.LogFormat("[Numerical Nightmare #{0}] Symbols faulty -> Dial positions: | 1. {1} | 2. {2} | 3. {3}", ModuleId, Dial1Goal, Dial2Goal, Dial3Goal);
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

        PinList.Add((faultySymbolValue + LastAndCurrentFaultyStage) % 20 + 1);
        if (PinList[0] % 2 == 0)
        {
            PinList.Add(((PinList[0] * 5) % 20) + 1);
        }
        else
        {
            PinList.Add(((PinList[0] * Bomb.GetBatteryCount()) % 20) + 1);
        }
        int pinDif = Mathf.Abs(PinList[0] - PinList[1]);
        PinList.Add(pinDif == 0 ? 1 : pinDif);

        Debug.LogFormat("[Numerical Nightmare #{0}] Symbols and stage faulty -> Microchip pins: | 1. {1} | 2. {2} | 3. {3}", ModuleId, PinList[0], PinList[1], PinList[2]);
    }

    void PinPresses(int pinIndex)
    {
        if (PinList.Count == 0)
        {
            Strike();
            Debug.LogFormat("[Numerical Nightmare #{0}] Dont touch the Microchip when its not broken!!!", ModuleId);
            return;
        }

        if (pinIndex == PinList[0])
        {
            PinList.RemoveAt(0);
        }
        else
        {
            Strike();
            Debug.LogFormat("[Numerical Nightmare #{0}] Your input {1} for Microchip was incorrect, expected was the pin {2}", ModuleId, pinIndex, PinList[0]);
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
        CorrectWire123 = (LastAndCurrentFaultyStage + ((Stage - 1) * (Stage - 1)) + ((Stage - 2) * (Stage - 3))) % 3 + 1;
        CorrectWireABC = (((FirstSerialLetter - 'A' + 1 + LastAndCurrentFaultyStage)) % 3) + 1;
        if (CorrectWireABC == 3)
        {
            CorrectWireABCString = "C";
        }
        else if (CorrectWireABC == 2)
        {
            CorrectWireABCString = "B";
        }
        else
        {
            CorrectWireABCString = "A";
        }

        Debug.LogFormat("[Numerical Nightmare #{0}] stage faulty -> Wires: | Connect {1} to {2}", ModuleId, CorrectWireABCString, CorrectWire123);
    }

    void SwitchWires()
    {
        if (LastWireABC == 3)
        {
            LastWireABCString = "C";
        }
        else if (LastWireABC == 2)
        {
            LastWireABCString = "B";
        }
        else
        {
            LastWireABCString = "A";
        }
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
        if (ChipBroken || DialsBroken || WiresBroken)
        {
            ChipBroken = false;
            DialsBroken = false;
            WiresBroken = false;
            BuildThisStage = false;
        }

        if (BuildThisStage == false)
        {
            GetRandomSymbolsReady();
            FinalInputBuilding();
            BuildThisStage = true;
        }

        if (Rnd.Range(0f, 100f) < FaultyProbability && StagesDone > 2 && FaultyThisStage == false)
        {
            HatchOpen = false;

            switch (Rnd.Range(1, 4))
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
                    DisplayAndStoreStageFaulty();
                    DisplayRandomSymbolsWorking();
                    WireCombination();
                    WiresBroken = true;
                    break;
            }

            Debug.LogFormat("[Numerical Nightmare #{0}] Stage {1} was faulty. | Faulty stage number: {2} | Faulty symbol values: {3}, {4}", ModuleId, Stage, Displays[0].text, SymbolDictionary.ContainsKey(Displays[1].text) ? SymbolDictionary[Displays[1].text] : 0, SymbolDictionary.ContainsKey(Displays[2].text) ? SymbolDictionary[Displays[2].text] : 0);
            FaultyProbability = 3.125f;
            FaultyThisStage = true;
        }
        else
        {
            DisplayStageWorking();
            DisplayRandomSymbolsWorking();
            FaultyProbability *= 2;
            FaultyThisStage = false;
            BuildThisStage = false;
            Debug.LogFormat("[Numerical Nightmare #{0}] Stage {1} valid symbol values: {2}, {3}. Calculated value for final input: {4}", ModuleId, Stage, ThisStageFirstSymbolValue, ThisStageSecondSymbolValue, FinalInputList.Last());
        }

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
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
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
                    case 13:
                    case 14:
                    case 15:
                    case 16:
                    case 17:
                    case 18:
                    case 19:
                    case 20:
                    case 21:
                    case 22:
                    case 23:
                    case 24:
                    case 25:
                    case 26:
                    case 27:
                    case 28:
                    case 29:
                    case 30:
                    case 31:
                        PinPresses(i - 11);
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
            HandleDisplayOnFinalInput(FinalInputList[0].ToString());
        }
        else if (keypad == -1 && FinalInputList[0] == LastInputNumber)
        {
            HandleDisplayOnFinalInput(FinalInputList[0].ToString());
            LastInputNumber = keypad;
        }
        else
        {
            Displays[0].characterSize = 0.005f;
            Strike();
            Displays[0].text = FinalInputIndex.ToString();
            Displays[1].text = StageSymbolList[FinalInputIndex][0];
            Displays[2].text = StageSymbolList[FinalInputIndex][1];
            string debugKeypad = keypad == -1 ? "Skip" : keypad.ToString();
            string debugKeypadCorrect = FinalInputList[0] == LastInputNumber ? "Skip" : FinalInputList[0].ToString();
            Debug.LogFormat("[Numerical Nightmare #{0}] Your input {1} was incorrect, expected was {2}", ModuleId, debugKeypad, debugKeypadCorrect);
            return;
        }

        if (keypad != -1)
        {
            LastInputNumber = keypad;
        }
    }

    //Making the Sequence
    void FinalInputBuilding()
    {
        if (ExMath.IsPrime(Stage))
        {
            FinalInputList.Add(((ThisStageFirstSymbolValue + ThisStageSecondSymbolValue + Stage) * Bomb.GetBatteryCount()) % 10);
        }
        else if (ExMath.IsSquare(Stage))
        {
            FinalInputList.Add((ThisStageFirstSymbolValue + ThisStageSecondSymbolValue + LastAndCurrentFaultyStage) % 10);
        }
        else
        {
            FinalInputList.Add((ThisStageFirstSymbolValue * ThisStageSecondSymbolValue) % 10);
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
            return;
        }

        if (HatchOpen == false)
        {
            HatchOpen = true;
        }
        else
        {
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
            if (ChipBroken) Debug.LogFormat("[Numerical Nightmare #{0}] Your microchip is missing some input(s): {1}", ModuleId, string.Join(", ",PinList.Select(x => x.ToString()).ToArray()));
            if (DialsBroken) Debug.LogFormat("[Numerical Nightmare #{0}] Your dial input {1}, {2}, {3} is incorrect, expected was {4}, {5}, {6}", ModuleId, CurrentDialPositions[0], CurrentDialPositions[1], CurrentDialPositions[2], Dial1Goal, Dial2Goal, Dial3Goal);
            if (WiresBroken) Debug.LogFormat("[Numerical Nightmare #{0}] Your wire connection {1} to {2} is incorrect, expected was {3} to {4}", ModuleId, LastWireABCString, LastWire123, CorrectWireABCString, CorrectWire123);
            Strike();
        }
    }

    #endregion

    //Solve and Stuff

    void Solve()
    {
        ModuleSolved = true;
        Displays[0].characterSize = 0.0036f;
        Displays[0].text = "GOOD NIGHT!";
        Displays[1].text = "";
        Displays[2].text = "";
        GetComponent<KMBombModule>().HandlePass();
    }

    void Strike()
    {
        if (HatchOpen == true)
        {
            HatchOpen = false;
        }
        ResetInside();

        GetComponent<KMBombModule>().HandleStrike();
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} press 834S45S.. for the sequence input on the keypad, 'S' for skip | !{0} dials [Pos 1] [Pos 2] [Pos 3] | !{0} wire [A/B/C] [1/2/3] | !{0} microchip/mc [Pin 1] [Pin 2] [Pin 3]";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

        yield return null;

        if ("PRESS".ContainsIgnoreCase(split[0]))
        {
            if (split.Length < 2)
            {
                yield return "sendtochaterror This command is to short!";
                yield break;
            }

            var validNumberInputs = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8' ,'9', 'S' };

            char[] numberSplit = string.Concat(split.Skip(1).ToArray()).ToCharArray();

            if (!numberSplit.All(validNumberInputs.Contains))
            {
                yield return $"sendtochaterror {numberSplit.Where(x => !validNumberInputs.Contains(x)).Join(", ")} is/are invalid!";
                yield break;
            }

            var setupNumbers = numberSplit.Select(x => Array.IndexOf(validNumberInputs, x)).ToArray();

            foreach (int number in setupNumbers)
            {
                PressHandler(Buttons[number]);
                yield return new WaitForSeconds(.05f);
            }
        }

        if ("DIALS".Contains(split[0]))
        {
            if (split.Length < 4)
            {
                yield return "sendtochaterror Make sure to input a position for all three dials!";
                yield break;
            }
            else if (split.Length > 4)
            {
                yield return "sendtochaterror You only need to input the position of three dials!";
                yield break;
            }

            var validDialInputs = new[] { '0', '1', '2', '3', '4', '5', '6', '7' };

            char[] dialSplit = string.Concat(split.Skip(1).ToArray()).ToCharArray();

            if (!dialSplit.All(validDialInputs.Contains))
            {
                yield return $"sendtochaterror {dialSplit.Where(x => !validDialInputs.Contains(x)).Join(", ")} is/are invalid!";
                yield break;
            }

            var setupDials = dialSplit.Select(x => Array.IndexOf(validDialInputs, x)).ToArray();

            int dialIndex = 32;

            ToggleHatch();

            yield return new WaitForSeconds(.3f);

            foreach (int dialPos in setupDials)
            {
                for (int i = dialPos; i > 0; i--)
                {
                    Debug.Log(i);
                    PressHandler(Buttons[dialIndex]);
                    yield return new WaitForSeconds(.05f);
                }
                dialIndex++;
            }

            ToggleHatch();
        }

        if ("WIRE".Contains(split[0]))
        {
            if (split.Length < 2)
            {
                yield return "sendtochaterror Make sure to input an alphabetic and a numeral position for the wire connection!";
                yield break;
            }
            else if (split.Length > 3)
            {
                yield return "sendtochaterror You only need two inputs for the wire connection!";
                yield break;
            }

            var validWireInputs = new[] { 'A', 'B', 'C', '1', '2', '3' };

            char[] wireSplit = string.Concat(split.Skip(1).ToArray()).ToCharArray();

            if (!wireSplit.All(validWireInputs.Contains))
            {
                yield return $"sendtochaterror {wireSplit.Where(x => !validWireInputs.Contains(x)).Join(", ")} is/are invalid!";
                yield break;
            }

            var setupWire = wireSplit.Select(x => Array.IndexOf(validWireInputs, x)).ToArray();

            if (setupWire.Length > 2)
            {
                yield return "sendtochaterror There is to many inputs for the wire connection!";
                yield break;
            }

            if (setupWire[0] < 3 && setupWire[1] < 3)
            {
                yield return "sendtochaterror There is no numerical input for the wire connection!";
                yield break;
            }

            if (setupWire[0] > 3 && setupWire[1] > 3)
            {
                yield return "sendtochaterror There is no alphabetical input for the wire connection!";
                yield break;
            }

            ToggleHatch();

            yield return new WaitForSeconds(.3f);
            PressHandler(Buttons[35 + setupWire[0]]);
            yield return new WaitForSeconds(.1f);
            PressHandler(Buttons[38 + setupWire[1] - 3]);
            yield return new WaitForSeconds(.1f);
            ToggleHatch();
        }

        if ("MICROCHIP".Contains(split[0]) || "MC".Contains(split[0]))
        {
            if (split.Length < 4)
            {
                yield return "sendtochaterror Make sure to input all three pins!";
                yield break;
            }
            else if (split.Length > 4)
            {
                yield return "sendtochaterror You only need to input three pins!";
                yield break;
            }

            var validChipInputs = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
                                          "11", "12", "13", "14", "15", "16", "17", "18", "19", "20" };

            string[] splitChip = split.Skip(1).ToArray();

            foreach (string pinString in splitChip)
            {
                int pinIndex = int.Parse(pinString);
                if (pinIndex > 20 || pinIndex < 0)
                {
                    yield return "sendtochaterror Check your inputs again, one of them is out of range ( 1 - 20 )!";
                    yield break;
                }
            }

            ToggleHatch();

            yield return new WaitForSeconds(.3f);

            foreach (string pinInput in splitChip)
            {
                PressHandler(Buttons[int.Parse(pinInput) + 11]);
                yield return new WaitForSeconds(.1f);
            }

            ToggleHatch();
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!FinalInput)
        {
            yield return true;
        }
        yield return new WaitForSeconds(0.1f);
        while (!ModuleSolved)
        {
            if (FinalInputList[0] != LastInputNumber)
            {
                PressHandler(Buttons[FinalInputList[0]]);
            }
            else
            {
                PressHandler(Buttons[10]);
            }
            yield return new WaitForSeconds(0.1f);
        }
        
    }
}