using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class TapFast : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public MeshRenderer borderColour;
    public GameObject button;

    private string tapCodeString = "ABCDE0FGHIJ1LMNOP2QRSTU3VWXYZ456789K";
    private string codeSequence = "";
    private bool isPlaying = false;
    private int nowPlaying = -1;
    private float tempo = 0.11f;

    private int wrongNumber;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved; // Some helpful booleans

    void Awake()
    {
        moduleId = moduleIdCounter++;

        button.GetComponent<KMSelectable>().OnInteract += delegate {
            ButtonFunction();
            button.GetComponent<KMSelectable>().AddInteractionPunch(0.5f);
            return false;
        };
    }

    void Start()
    {
        float h = UnityEngine.Random.Range(0f, 1f);//Randomises border colour
        borderColour.material.color = Color.HSVToRGB(h, 1f, 0.7f);

        var sn = bomb.GetSerialNumber();
        var sb = new StringBuilder();
        int decoy = UnityEngine.Random.Range(0, 2); 
        int toAppend = 0;
        wrongNumber = UnityEngine.Random.Range(0, 6);
        for (int i = 0; i < sn.Length; i++)//Converting serial number to tap code
        {
            if (wrongNumber == i)//Randomising that one character to a different tap code pair
            {
                if (decoy == 0)
                    do
                    {
                        decoy = 10 * (tapCodeString.IndexOf(sn[i]) / 6 + 1) + UnityEngine.Random.Range(0, 6);
                    } while (decoy == 10 * (tapCodeString.IndexOf(sn[i]) / 6 + 1) + (tapCodeString.IndexOf(sn[i]) % 6 + 1));
                else
                    do
                    {
                        decoy = 10 * UnityEngine.Random.Range(0, 6) + (tapCodeString.IndexOf(sn[i]) % 6 + 1);
                    } while (decoy == 10 * (tapCodeString.IndexOf(sn[i]) / 6 + 1) + (tapCodeString.IndexOf(sn[i]) % 6 + 1)); sb.Append(decoy);
                sb.Append(decoy);
            }
            else
            {
                sb.Append(10 * (tapCodeString.IndexOf(sn[i]) / 6 + 1) + (tapCodeString.IndexOf(sn[i]) % 6 + 1));
            }
        }
        codeSequence = sb.ToString();
        Debug.LogFormat("[Tap Fast #{0}] The code sequence is {1}.", moduleId, codeSequence);
        Debug.LogFormat("[Tap Fast #{0}] The wrongly encoded character is pair #{1}, which encodes for {2}, when it should be {3} in the serial number.", moduleId, wrongNumber + 1, tapCodeString[6 * (codeSequence[2 * wrongNumber] - '0' - 1) + codeSequence[2 * wrongNumber + 1] - '0' - 1], sn[wrongNumber]);
    }

    void ButtonFunction()
    {
        if (moduleSolved) { return; }
        if (isPlaying == false)
        {
            Debug.LogFormat("<Tap Fast #{0}> Playing sequence...", moduleId);
            StartCoroutine("PlaySeq");
        }
        else
        {
            Debug.LogFormat("[Tap Fast #{0}] Button pressed on pair #{1}.", moduleId, nowPlaying + 1);
            if (nowPlaying == wrongNumber)
            {
                Debug.LogFormat("[Tap Fast #{0}] That is correct, module solved!", moduleId);
                module.HandlePass();
                moduleSolved = true;
                StopCoroutine("PlaySeq");
                audio.PlaySoundAtTransform("bell", transform);
            }
            else
            {
                StopCoroutine("PlaySeq");
                isPlaying = false;
                Debug.LogFormat("[Tap Fast #{0}] That is incorrect, strike.", moduleId);
                audio.PlaySoundAtTransform("gong", transform);
                tempo *= 1.10f;
                module.HandleStrike();
            }
        }
    }

    IEnumerator PlaySeq()
    {
        isPlaying = true;
        audio.PlaySoundAtTransform("kick", transform);//First beat
        for (int i = 0; i < codeSequence.Length; i++)
        {
            nowPlaying = i/2;
            /*if (i % 2 == 0) 
                audio.PlaySoundAtTransform("kick", transform);
            else
                audio.PlaySoundAtTransform("snare", transform);*/
            for (int j = 0; j < codeSequence[i] - '0'; j++)
            {
                audio.PlaySoundAtTransform("tap", transform);
                if (i == codeSequence.Length - 1 && j == codeSequence[i] - '0' - 1)//Last beat
                    audio.PlaySoundAtTransform("snare", transform);
                yield return new WaitForSeconds(tempo);
            }
            yield return new WaitForSeconds(tempo);
        }
        nowPlaying = -1;
        isPlaying = false;
    }

    void Update() //Runs every frame.
    {

    }

    //Twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press/tap/play to press the button, !{0} press/tap # to press the button (if the sequence is not playing), then press it again at the #th pair in the sequence, # ranges from 1 to 6 inclusive";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(command, @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(command, @"^\s*tap\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(command, @"^\s*play\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            button.GetComponent<KMSelectable>().OnInteract();
        }
        else if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(parameters[0], @"^\s*tap\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length > 2)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            int temp = -1;
            if (!int.TryParse(parameters[1], out temp))
            {
                yield return "sendtochaterror The specified pair '" + parameters[1] + "' is invalid!";
                yield break;
            }
            if (temp < 1 || temp > 6)
            {
                yield return "sendtochaterror The specified pair '" + parameters[1] + "' is out of range 1-6!";
                yield break;
            }
            if (!isPlaying) { button.GetComponent<KMSelectable>().OnInteract(); }
            while (isPlaying && nowPlaying <= temp - 1)
            {
                yield return "trycancel";
                yield return null;
                if (nowPlaying == temp - 1)
                {
                    button.GetComponent<KMSelectable>().OnInteract();
                    yield break;
                }
            }
            if (nowPlaying > temp - 1)
            {
                yield return "sendtochaterror The specified pair is no longer available!";
                yield break;
            }
            else if (!isPlaying)
            {
                yield return "sendtochaterror The sequence is no longer playing!";
                yield break;
            }
        }
    }


    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            if (!isPlaying) { button.GetComponent<KMSelectable>().OnInteract(); }
            if (nowPlaying == wrongNumber)
            {
                button.GetComponent<KMSelectable>().OnInteract();
            }
            yield return null;
        }
        yield return null;
    }

}
