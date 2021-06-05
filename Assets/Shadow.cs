using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using UnityEngine.Networking;

public class Shadow : MonoBehaviour {
    public MeshRenderer BurntBackground;
    public KMBombModule Module;
    public KMBombInfo Info;
    public KMAudio Audio;
    public KMSelectable Button;
    public ParticleSystem Particles;

    public AudioClip Woosh;

    public ParticleManager Manager, Outer, Outer2;

    [SerializeField]
    private string URL;

    [SerializeField]
    private float waitTime;

    private bool hasDumped = false, hasPressed = false, solved = false;

    private int change = 0;

    private NetData data = new NetData(5);
    private int accurate = 5;

    private static int _idCounter = 1;
    private int _id;

    private int defaultRandom = 5;

	void Start () {
        defaultRandom = Random.Range(3, 10);
        data.Count = defaultRandom;
        _id = _idCounter++;
        change = 1;
        StartCoroutine(SendRequest(false));
        StartCoroutine(Connect());
        Module.OnActivate += Activate;
	}
	
    private void Activate()
    {
        Button.OnInteract += delegate () { ButtonPress(); return false; };
    }

    private void ButtonPress()
    {
        Audio.PlaySoundAtTransform(Woosh.name, transform);
        Particles.Play();
        if (solved) return;
        StopAllCoroutines();
        if (!hasPressed)
        {
            hasPressed = true;
            Debug.LogFormat("[Burnt #{0}] Press one.", _id);
            Manager.timeScaler *= -1;
            Outer.timeScaler *= -1;
            Outer2.timeScaler *= -1;
        }
        else
        {
            int sol = 1;

            // Individual digits
            int a = ((data.Count - (data.Count % 100)) / 100);
            int b = (((data.Count % 100) - (data.Count % 10)) / 10);
            int c = data.Count % 10;

            var query = Info.QueryWidgets("volt", "");
            if (query.Count != 0)
                if (float.Parse(JsonConvert.DeserializeObject<VoltData>(query.First()).voltage) > c)
                    b = Math.Min(b + 2, 9);

            for (int i = 0; i < 10 - b ; i++)
            {
                sol *= Math.Max(c - a, 1);
                sol = DigitalRoot(sol);
            }

            if (sol == (int)(Info.GetTime() - (Info.GetTime() % 1f)) % 10)
            {
                Debug.LogFormat("[Burnt #{0}] Press two was at {1} seconds. This was correct.", _id, (int)(Info.GetTime() - (Info.GetTime() % 1f)));
                Module.HandlePass();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                solved = true;
                Manager.timeScaler *= -1;
                Outer.timeScaler *= -1;
                Outer2.timeScaler *= -1;
                change = -1;
                hasDumped = true;
                StartCoroutine(SendRequest(false));
            }
            else
            {
                Debug.LogFormat("[Burnt #{0}] Press two was at {1} seconds. This was incorrect.", _id, (int)(Info.GetTime() - (Info.GetTime() % 1f)));
                Module.HandleStrike();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);
                Manager.timeScaler *= -1;
                Outer.timeScaler *= -1;
                Outer2.timeScaler *= -1;
                hasPressed = false;
                StartCoroutine(Connect());

            }
        }
    }

    #region Helper Functions
    private int DigitalRoot(int input)
    {
        int acc = 0;
        while (input > 0)
        {
            acc += input % 10;
            input -= input % 10;
            input /= 10;
        }
        if (acc < 10)
            return acc;
        else
            return DigitalRoot(acc);
    }

    private void UpdateFlames()
    {
        data.Count = accurate;
        Manager.SetParticles(data.Count % 10);
        Outer.SetParticles(((data.Count % 100) - (data.Count % 10)) / 10);
        Outer2.SetParticles((data.Count - (data.Count % 100)) / 100);
        Debug.LogFormat("[Burnt #{0}] The flames read {1}.", _id, data.Count);
    }

    private IEnumerator Connect()
    {
        yield return null;
        while (true)
        {
            StartCoroutine(SendRequest(true));
            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator SendRequest(bool update)
    {
        using (var http = UnityWebRequest.Get(URL))
        {
            if (change > 0)
            { http.SetRequestHeader("SIMPLEDBADD", "TRUE"); change--; }
            if (change < 0)
            { http.SetRequestHeader("SIMPLEDBSUB", "TRUE"); change++; }

            // Request and wait for the desired page.
            yield return http.SendWebRequest();

            if (http.isNetworkError)
            {
                Debug.LogFormat(@"<Burnt #{0}> Website responded with error: {1}", _id, http.error);
                yield break;
            }

            if (http.responseCode != 200)
            {
                Debug.LogFormat(@"<Burnt #{0}> Website responded with code: {1}", _id, http.error);
                yield break;
            }

            var response = JObject.Parse(http.downloadHandler.text)["Count"];
            if (response == null)
            {
                Debug.LogFormat("<Burnt #{0}> Website did not respond with a value at \"Count\" key.", _id);
                yield break;
            }

            Debug.LogFormat(@"<Burnt #{0}> Response loaded.", _id);
            accurate = response.Value<int>();
        }
        if (update)
        {
            UpdateFlames();
        }
    }
    #endregion
}

public class NetData
{
    public int Count { get; set; }

    public NetData(int Count)
    {
        this.Count = Count;
    }
}

public class VoltData
{
    public string voltage { get; set; }
}