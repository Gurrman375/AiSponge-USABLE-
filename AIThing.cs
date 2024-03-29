using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Cinemachine;
using DialogueAI;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;
using OpenAI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Net;
using Y2Sharp;
using static System.Net.WebRequestMethods;
using File = System.IO.File;
using System.Threading;
using UnityEngine.UIElements;

#pragma warning disable CS4014
public class AIThing : MonoBehaviour
{
    public CharacterManager characterManager;
    private Random _random = new Random();
    [SerializeField] private Transform[] KrustyKrab;
    [SerializeField] private Transform[] ConchStreet;
    [SerializeField] private Transform[] OutsideKrustyKrab;
    [SerializeField] private Transform[] apkrab;
    [SerializeField] private Transform[] apconch;
    [SerializeField] private Transform[] SquidwardLocations;
    [SerializeField] private Transform[] cameraLocations; // Add this line at the field declarations
    [SerializeField] private Transform[] cumbucket;
    // Should probably change this to a safer method of storing the keys
    string openAIKey = "***********************************"; // Get a key from https://platform.openai.com/api-keys
    [SerializeField] private string fakeYouUsernameOrEMail = "*************************"; // Create an account on https://fakeyou.com
    [SerializeField] private string fakeYouPassword = "*************************";

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private TextMeshProUGUI topicText;
    [SerializeField] public AudioClip[] audioClips; // Put in here for a character like Gary that does not have a voice model and speaks gibberish
    public GameObject[] characterPrefabs;

    [SerializeField] private CinemachineVirtualCamera _cinemachineVirtualCamera;
    [SerializeField] private TextMeshProUGUI subtitles;
    [SerializeField] private VideoPlayer videoPlayer;

    [SerializeField] private VideoClip[] videoClips;
    private HttpClient _client = new();
    private OpenAIApi _openAI;

    public VideoClip clipToPlay;
    public VideoClip clip;

    // Singleton instance of the AIDirector script
    public static AIThing Instance;
    public GameObject[] gt { get; private set; }
    // Reference to the speaking character's animator
    public Animator speakingCharacterAnimator;
    private void TeleportCharacters()
    {
        // Map each spawn point group to a Squidward location
        Dictionary<Transform[], Transform> squidwardLocationMap = new Dictionary<Transform[], Transform>()
    {
        { KrustyKrab, SquidwardLocations[0] },
        { ConchStreet, SquidwardLocations[1] },
        { OutsideKrustyKrab, SquidwardLocations[2] },
        { apkrab, SquidwardLocations[3] },
        { apconch, SquidwardLocations[4] },
        { cumbucket, SquidwardLocations[5] }
    };
        // Map each spawn point group to a camera location
        Dictionary<Transform[], Transform> cameraLocationMap = new Dictionary<Transform[], Transform>()
    {
        { KrustyKrab, cameraLocations[0] },
        { ConchStreet, cameraLocations[1] },
        { OutsideKrustyKrab, cameraLocations[2] },
        { apkrab, cameraLocations[3] },
        { apconch, cameraLocations[4] },
        { cumbucket, cameraLocations[5] }
    };

        // Put your spawn point arrays into a list
        List<Transform[]> spawnPointGroups = new List<Transform[]>()
    {
        ConchStreet, OutsideKrustyKrab, apkrab, apconch, cumbucket
    };

        // Select a random spawn point group
        Transform[] spawnPoints = spawnPointGroups[_random.Next(spawnPointGroups.Count)];

        // Get Squidward's location for the selected spawn point group
        Transform squidwardLocation = squidwardLocationMap[spawnPoints];

        // Create a list of spawn points and shuffle it
        List<Transform> shuffledSpawnPoints = spawnPoints.OrderBy(x => _random.Next()).ToList();

        int index = 0;

        foreach (GameObject characterPrefab in characterPrefabs)
        {
            // Select a spawn point from the shuffled list
            Transform spawnPoint = shuffledSpawnPoints[index % shuffledSpawnPoints.Count];

            // If the character is Squidward, instantiate him at his special location
            if (characterPrefab.name == "squidward")
            {
                GameObject newCharacter = Instantiate(characterPrefab, squidwardLocation.position, squidwardLocation.rotation);
            }
            else
            {
                // Instantiate a new character at the spawn point
                GameObject newCharacter = Instantiate(characterPrefab, spawnPoint.position, spawnPoint.rotation);
            }
            FindAndAssignCharacterInstances();
            // Position the Cinemachine camera at the last selected spawn point
            if (_cinemachineVirtualCamera != null)
            {
                Transform cameraLocation = cameraLocationMap[spawnPoints];
                _cinemachineVirtualCamera.transform.position = cameraLocation.position;
                _cinemachineVirtualCamera.transform.rotation = cameraLocation.rotation;
                TeleportNarratorToSquidward();
            }

            index++;
        }
    }
    private void TeleportNarratorToSquidward()
    {
        GameObject narrator = GameObject.Find("narrator"); // Assuming the narrator's name in the hierarchy
        GameObject squidward = GameObject.Find("patrick(Clone)");

        if (narrator != null && squidward != null)
        {
            narrator.transform.position = squidward.transform.position; // Teleporting to Squidward's position
        }
        else
        {
            Debug.LogWarning("Narrator or Squidward not found!");
        }
    }
            // Awake method to set up the singleton instance
            private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else Destroy(gameObject);
    }
    private string previousCharacter;

    private void PlayRandomVideoClip()
    {
        // Select a random video clip from your array
        VideoClip clip = videoClips[_random.Next(0, videoClips.Length)];

        // Assign the video clip to the VideoPlayer
        videoPlayer.clip = clip;

        // Start playing the video clip
        videoPlayer.Play();
    }
    IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    void Start()
    {

        TeleportCharacters();
        PlayRandomVideoClip();
        _openAI = new OpenAIApi(openAIKey, null); ;
        Init();
        characters.Add("spongebob", GameObject.Find("spongebob(Clone)"));
        characters.Add("patrick", GameObject.Find("patrick(Clone)"));
        characters.Add("squidward", GameObject.Find("squidward(Clone)"));
        characters.Add("mrkrabs", GameObject.Find("mrkrabs(Clone)"));
        characters.Add("gary", GameObject.Find("gary(Clone)"));
        characters.Add("plankton", GameObject.Find("plankton(Clone)"));
        characters.Add("larry", GameObject.Find("larry(Clone)"));
        characters.Add("mrspuff", GameObject.Find("mrspuff(Clone)"));
        characters.Add("sandy", GameObject.Find("sandy(Clone)"));
        previousCharacter = "";
        currentCharacter = "";
    }
    private void FindAndAssignCharacterInstances()
    {
        gt = new GameObject[8];
        gt[0] = GameObject.Find("mrkrabs(Clone)");
        gt[1] = GameObject.Find("squidward(Clone)");
        gt[2] = GameObject.Find("spongebob(Clone)");
        gt[3] = GameObject.Find("gary(Clone)");
        gt[4] = GameObject.Find("larry(Clone)");
        gt[5] = GameObject.Find("sandy(Clone)");
        gt[6] = GameObject.Find("plankton(Clone)");
        gt[7] = GameObject.Find("patrick(Clone)");
    }
    private string currentCharacter;
    async void Init()
    {
        string cookie = LoadCookie();

        if (cookie == "")
        {
            cookie = await FetchAndStoreCookie();
        }

        ConfigureHttpClient(cookie);

        // Check cookie validity
        await CheckCookieValidity(_client);
        // Read the blacklist
        List<string> blacklist = LoadBlacklist();

        // Pick a random topic
        Queue<string> topics = LoadTopics();

        // If there are no topics, play a video clip and restart in 10 seconds
        if (topics.Count == 0)
        {
            PlayVideoClipAndWait(clipToPlay, "1.0.1", 5f);
            return;
        }

        string topic = SelectTopic(topics);
        DisplayTopic(topic);

        // Add the chosen topic to the blacklist and write it back to the file
        UpdateBlacklist(blacklist, topic, topics);

        // Play the random video clip


        // Generate the dialogue
        Generate(topic);
    }

    private int _proxyIndex = 0;
    [SerializeField] private string[] proxyArray;
    private HttpClientHandler _clientHandler = new HttpClientHandler();
    private string LoadCookie()
    {
        string cookieFilePath = $"{Environment.CurrentDirectory}\\Assets\\Scripts\\key.txt";
        if (!File.Exists(cookieFilePath))
            File.WriteAllText(cookieFilePath, "");

        return File.ReadAllText(cookieFilePath);
    }

    private async Task<string> FetchAndStoreCookie()
    {
        var loginDetails = new
        {
            username_or_email = fakeYouUsernameOrEMail,
            password = fakeYouPassword
        };

        var response = await _client.PostAsync("https://api.fakeyou.com/login",
            new StringContent(JsonConvert.SerializeObject(loginDetails), Encoding.UTF8, "application/json"));

        var cookieData = JsonConvert.SerializeObject(response.Headers.GetValues("set-cookie").First());
        var cookieParts = cookieData.Split(';');
        string cookie = cookieParts[0].Replace("session=", "").Replace("\"", "");

        File.WriteAllText($"{Environment.CurrentDirectory}\\Assets\\Scripts\\key.txt", cookie);

        return cookie;
    }

    private void ConfigureHttpClient(string cookie)
    {
        var handler = new HttpClientHandler();
        handler.CookieContainer = new CookieContainer();
        handler.CookieContainer.Add(new Uri("https://api.fakeyou.com"), new Cookie("session", cookie));
        if (proxyArray.Length > 0)
        {
            // Set proxy for HttpClientHandler only if proxies are available
            string[] proxyParts = proxyArray[_proxyIndex].Split(':');
            var proxy = new WebProxy(proxyParts[0] + ":" + proxyParts[1]);
            proxy.Credentials = new NetworkCredential(proxyParts[2], proxyParts[3]);
            handler.UseProxy = true;
            handler.Proxy = proxy;
        }


        _client = new HttpClient(handler);
        _client.DefaultRequestHeaders.Add("Accept", "application/json");
        _fakeYouClient = new HttpClient(handler);  // Create _fakeYouClient with the handler
        _fakeYouClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }
    private async Task CheckCookieValidity(HttpClient client)
    {
        var checkKey = await client.GetAsync("https://api.fakeyou.com/v1/billing/active_subscriptions");
        var checkString = await checkKey.Content.ReadAsStringAsync();
        Debug.Log(checkString);
    }

    private List<string> LoadBlacklist()
    {
        string blacklistPath = $"{Environment.CurrentDirectory}\\Assets\\Scripts\\blacklist.json";
        if (File.Exists(blacklistPath))
        {
            return JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(blacklistPath));
        }

        return new List<string>();
    }

    private Queue<string> LoadTopics()
    {
        return new Queue<string>(JsonConvert.DeserializeObject<List<string>>(
            File.ReadAllText($"{Environment.CurrentDirectory}\\Assets\\Scripts\\topics.json")));
    }

    private void PlayVideoClipAndWait(VideoClip clip, string sceneName, float delay)
    {
        videoPlayer.clip = clip;
        videoPlayer.Play();

        StartCoroutine(LoadSceneAfterDelay(sceneName, delay));
    }

    private string SelectTopic(Queue<string> topics)
    {
        return topics.Dequeue();
    }

    private void DisplayTopic(string topic)
    {
        if (topicText != null)
        {
            topicText.text = "Next Topic: " + topic;
        }
    }

    private void UpdateBlacklist(List<string> blacklist, string topic, Queue<string> topics)
    {
        string blacklistPath = $"{Environment.CurrentDirectory}\\Assets\\Scripts\\blacklist.json";

        if (!blacklist.Contains(topic))
        {
            blacklist.Add(topic);
            File.WriteAllText(blacklistPath, JsonConvert.SerializeObject(blacklist));
        }

        // Write the remaining topics back to the topics file
        File.WriteAllText($"{Environment.CurrentDirectory}\\Assets\\Scripts\\topics.json", JsonConvert.SerializeObject(topics.ToList()));
    }


    public AIThing()
    {
        _fakeYouClient = new HttpClient(_clientHandler);
    }
    private HttpClient _fakeYouClient; // This client will be used for FakeYou API calls

    private IEnumerator WaitForTransition(string topic)
    {
        while (videoPlayer.isPlaying)
        {
            yield return null;
        }

        Generate(topic);
    }

    IEnumerator RetryGenerateAfterDelay(string topic)
    {
        yield return new WaitForSeconds(15);
        Generate(topic);
    }
    private Dictionary<string, GameObject> characters = new Dictionary<string, GameObject>();
    IEnumerator LoadAndPlayAudioClipCoroutine(string path)
    {
        using (var uwr = UnityWebRequestMultimedia.GetAudioClip($"file:///{path}", AudioType.MPEG)) // Unity does not support MP3 for this method
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                audioSource.clip = DownloadHandlerAudioClip.GetContent(uwr);

                // Look at the character who is singing
                if (characters.ContainsKey(currentCharacter))
                {
                    GameObject character = characters[currentCharacter];

                    // Make the character look at the Cinemachine virtual camera
                    Vector3 directionToCamera = _cinemachineVirtualCamera.transform.position - character.transform.position;
                    directionToCamera.y = 0; // Keep the character's rotation level on the Y-axis
                    character.transform.rotation = Quaternion.LookRotation(directionToCamera);

                    previousCharacter = currentCharacter;  // Store the current character as the previous character
                    currentCharacter = character.name;  // Update the current character

                    _cinemachineVirtualCamera.LookAt = character.transform;
                    _cinemachineVirtualCamera.Follow = character.transform;
                    if (characters.ContainsKey(previousCharacter))
                    {
                        GameObject previousCharacterObject = characters[previousCharacter];

                        //If using new Character script, do the new way of LookAt, otherwise do it the old way
                        StartCoroutine(TurnToSpeaker(character.transform, previousCharacterObject.transform));

                    }
                    Debug.Log("Camera is now following: " + currentCharacter);  // Debug statement
                    if (subtitles != null)
                        subtitles.text = "*sings*";
                    foreach (GameObject obj in gt)
                    {
                        if (obj != null && obj != character && obj.name != "karen")
                        {

                            StartCoroutine(TurnToSpeaker(obj.transform, character.transform));
                        }
                    }

                    // Get the Animator component from the character
                    Animator characterAnimator = character.GetComponent<Animator>();
                    if (characterAnimator != null)
                    {
                        // Start the speaking animation
                        characterAnimator.SetBool("isSpeaking", true);
                    }
                    videoPlayer.Stop();
                    audioSource.Play();
                    float startTime = Time.time;
                    while (audioSource.isPlaying)
                    {
                        // Check if the playback has exceeded 2 minutes and 30 seconds
                        if (Time.time - startTime > 150.0f) // 150 seconds = 2 minutes and 30 seconds
                        {
                            audioSource.Stop(); // Stop the playback if the time limit is exceeded
                            Debug.Log("Audio playback stopped after 2 minutes and 30 seconds.");
                            break; // Exit the while loop
                        }
                        yield return null;
                    }

                    if (characterAnimator != null)
                    {
                        // Stop the speaking animation
                        characterAnimator.SetBool("isSpeaking", false);
                    }

                    // After the audio is finished playing, reload the scene
                    string currentSceneName = SceneManager.GetActiveScene().name;
                    SceneManager.LoadScene(currentSceneName);
                }
                else
                {
                    Debug.LogError("Character not found in dictionary: " + currentCharacter);  // Debug statement
                    string currentSceneName = SceneManager.GetActiveScene().name;
                    SceneManager.LoadScene(currentSceneName);
                }
            }
        }
    }

    async void Generate(string topic)
    {
        // Define dialogues at the beginning of the function
        List<Dialogue> dialogues = new List<Dialogue>();

        // Check if the topic contains a YouTube link
        if (topic.Contains(" sings https://www.youtube.com/watch?v="))
        {
            // Split the topic string to get the character and the video ID
            string[] topicParts = topic.Split(new string[] { " sings https://www.youtube.com/watch?v=" }, StringSplitOptions.None);
            string character = topicParts[0].ToLower();
            string videoId = topicParts[1];

            // Fetch the information for the YouTube video
            try
            {
                // Your code that might throw an error
                await Y2Sharp.Youtube.Video.GetInfo(videoId);
            }

            catch (Exception e)
            {
                // Check if the error message is what you're looking for
                if (e.Message.Contains("videoId was too short or long"))
                {
                    Debug.Log("Caught an error: " + e.Message);
                    Debug.Log("Restarting scene...");

                    // Get the current scene name
                    Scene scene = SceneManager.GetActiveScene();
                    // Reload the scene
                    SceneManager.LoadScene(scene.name);
                }
            }
            // Create a new Y2Sharp.Youtube.Video object
            var video = new Y2Sharp.Youtube.Video();

            // Check if the Audio directory exists, if not, create it
            string audioDirectoryPath = Path.Combine(Application.dataPath, "Audio");
            if (!Directory.Exists(audioDirectoryPath))
            {
                Directory.CreateDirectory(audioDirectoryPath);
            }

            // Define the path where the audio file will be saved
            string audioFilePath = Path.Combine(audioDirectoryPath, $"{videoId}.wav");

            // Download the video as a WAV file
            await video.DownloadAsync(audioFilePath, "mp3", "128");

            // Add a new dialogue item that represents the character singing
            dialogues.Add(new Dialogue
            {
                uuid = videoId, // We'll use the YouTube video ID as the UUID for this dialogue item
                text = "*sings*", // The character is singing
                character = character // The character is the one extracted from the topic
            });

            // Store the current character who is singing
            currentCharacter = character;
            Debug.Log("Singing character: " + currentCharacter);  // Debug statement
            videoPlayer.Stop();
            // Start the coroutine that loads and plays the audio file
            StartCoroutine(LoadAndPlayAudioClipCoroutine(audioFilePath));

        }
        else
        {
            string[] text = CheckAndGetScriptLines();

            if (text.Length == 0)
            {
                await GenerateNext(topic);
                text = LoadScriptLines();
                string newTopic = SelectTopic(LoadTopics());
                GenerateNext(newTopic);
            }
            else
            {
                GenerateNext(topic);
            }

            List<Task> ttsTasks = CreateTTSRequestTasks(text, dialogues);
            await Task.WhenAll(ttsTasks);

            StartCoroutine(Speak(dialogues));
        }
    }

    private string[] CheckAndGetScriptLines()
    {
        string scriptPath = "Assets/Scripts/Next.txt";
        if (File.Exists(scriptPath)) return File.ReadAllLines(scriptPath);

        // Delete the script from the file so you don't get the same script twice
        File.WriteAllText(scriptPath, "");
        return new string[] { };
    }

    private string[] LoadScriptLines()
    {
        return File.ReadAllLines("Assets/Scripts/Next.txt");
    }

    private List<Task> CreateTTSRequestTasks(string[] text, List<Dialogue> dialogues)
    {
        List<Task> ttsTasks = new List<Task>();
        foreach (var line in text)
        {
            if (TryParseCharacterLine(line, out string voicemodelUuid, out string textToSay, out string character))
            {
                ttsTasks.Add(CreateTTSRequest(textToSay, voicemodelUuid, dialogues, character));
            }
        }

        return ttsTasks;
    }

    private bool TryParseCharacterLine(string line, out string voicemodelUuid, out string textToSay, out string character)
    {
        voicemodelUuid = "";
        textToSay = "";
        character = "";

        if (line.StartsWith("SpongeBob:"))
        {
            textToSay = line.Replace("SpongeBob:", "");
            voicemodelUuid = "TM:618j8qwddnsn";
            character = "spongebob(Clone)";
        }
        else if (line.StartsWith("Spongebob:"))
        {
            textToSay = line.Replace("Spongebob:", "");
            voicemodelUuid = "TM:618j8qwddnsn";
            character = "spongebob(Clone)";
        }
        else if (line.StartsWith("Patrick:"))
        {
            textToSay = line.Replace("Patrick:", "");
            voicemodelUuid = "TM:ptcaavcfhwxd";
            character = "patrick(Clone)";
        }
        else if (line.StartsWith("Mr. Krabs"))
        {
            voicemodelUuid = "TM:ade4ta7rc720";
            textToSay = line.Replace("Mr. Krabs:", "");
            character = "mrkrabs(Clone)";
        }
        else if (line.StartsWith("Squidward:"))
        {
            voicemodelUuid = "TM:4e2xqpwqaggr";
            textToSay = line.Replace("Squidward:", "").ToUpper(); // Converting to caps because funny Loudward
            textToSay = textToSay.TrimEnd() + "!"; // Add "!" at the end of Squidward's sentences
            character = "squidward(Clone)";
        }
        else if (line.StartsWith("Sandy:"))
        {
            voicemodelUuid = "TM:eaachm5yecgz";
            textToSay = line.Replace("Sandy:", "");
            character = "sandy(Clone)";
        }
        else if (line.StartsWith("Gary:"))
        {
            voicemodelUuid = "TM:eaachm5yecgz";
            textToSay = line.Replace("Gary:", "");
            character = "gary(Clone)";
        }
        else if (line.StartsWith("Plankton:"))
        {
            voicemodelUuid = "TM:ym446j7wkewg";
            textToSay = line.Replace("Plankton:", "");
            character = "plankton(Clone)";
        }
        else if (line.StartsWith("French Narrator:"))
        {
            voicemodelUuid = "TM:vjzq7981swey";
            textToSay = line.Replace("French Narrator:", "");
            character = "narrator";
        }
        else if (line.StartsWith("Larry The Lobster:"))
        {
            voicemodelUuid = "TM:t57xkhm1t12q";
            textToSay = line.Replace("Larry The Lobster:", "");
            character = "larry(Clone)";
        }

        return textToSay != "";
    }

    private async Task CreateTTSRequest(string textToSay, string voicemodelUuid, List<Dialogue> dialogues, string character)
    {
        var jsonObj = new
        {
            inference_text = textToSay,
            tts_model_token = voicemodelUuid,
            uuid_idempotency_token = Guid.NewGuid().ToString()
        };
        var content = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");

        bool retry = true;
        while (retry)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();

            if (proxyArray.Length > 0)
            {
                // Update the HttpClient to use the next proxy
                _proxyIndex = (_proxyIndex + 1) % proxyArray.Length; // This will loop back to 0 when it reaches the end of the array
                string[] proxyParts = proxyArray[_proxyIndex].Split(':');
                var proxy = new WebProxy(proxyParts[0] + ":" + proxyParts[1]);
                proxy.Credentials = new NetworkCredential(proxyParts[2], proxyParts[3]);
                httpClientHandler.UseProxy = true;
                httpClientHandler.Proxy = proxy;
            }

            // Set up the CookieContainer
            CookieContainer cookieContainer = new CookieContainer();
            string cookieFilePath = $"{Environment.CurrentDirectory}\\Assets\\Scripts\\key.txt";
            string cookieData = File.Exists(cookieFilePath) ? File.ReadAllText(cookieFilePath) : "";
            cookieContainer.Add(new Uri("https://api.fakeyou.com"), new Cookie("session", cookieData));
            httpClientHandler.CookieContainer = cookieContainer;

            // Create the new HttpClient
            HttpClient fakeYouClient = proxyArray.Length > 0 ? new HttpClient(httpClientHandler) : _client;
            fakeYouClient.DefaultRequestHeaders.Add("Accept", "application/json");

            // Make the request
            var response2 = await fakeYouClient.PostAsync("https://api.fakeyou.com/tts/inference", content);
            var responseString = await response2.Content.ReadAsStringAsync();
            SpeakResponse speakResponse = null;
            try
            {
                speakResponse = JsonConvert.DeserializeObject<SpeakResponse>(responseString);
            }
            catch (JsonReaderException)
            {
                Debug.Log("Error parsing API response. Probably due to rate limiting. Waiting 10 seconds before retrying.");
                await Task.Delay(5);
                continue;
            }

            if (!speakResponse.success)
            {
                continue;
            }

            retry = false;

            dialogues.Add(new Dialogue
            {
                uuid = speakResponse.inference_job_token,
                text = textToSay,
                character = character
            });
            Debug.Log(responseString);
            await Task.Delay(250); // For rate limiting, With proxys set to 250, Without set to 5500
        }
    }

    //private Task GenerateNext(string topic)
    //{
    //    return GenerateNext(topic);
    //}

    private async Task GenerateNext(string topic) // , CreateChatCompletionResponse text
    {
        var request = new CreateChatCompletionRequest
        {
            Model = "gpt-3.5-turbo",
            Messages = new List<ChatMessage>()
            {
                new ChatMessage()
                {
                    Role = "user",
                    Content = $"Create a vulgar, nsfw, politically incorrect script for a scene from Spongebob where characters discuss a topic. Possible Characters Include Spongebob, Patrick, Squidward, Sandy, Mr. Krabs, Plankton, Larry The Lobster and very rarely Gary and French Narrator. Use the format: Character: <dialogue>. Only reply with coherent character dialogue. Around 12 - 15 lines of dialogue with talking only. The topic is: {topic}"
                }
            }
        };
        _openAI.CreateChatCompletionAsync(request,
            (responses) => {
                var result = string.Join("", responses.Select(response => response.Choices[0].Delta.Content));
                Debug.Log(result);
                File.WriteAllText("Assets/Scripts/Next.txt", result);
            },
            () => {
                Debug.Log("Prompt Completed successfully. If any errors are present, make line 753 not a comment.");
            },
            new CancellationTokenSource()
        );
        //if (response.Error != null || response.Choices == null)
        //{
        //    await GenerateNext(topic);
        //}
        //else
        //{
        //    string text;
        //    if (response.Choices != null && response.Choices.Count > 0)
        //    {
        //        text = response.Choices[0].Delta.Content;
        //        for (int i = 1; i < response.Choices.Count; i++)
        //        {
        //            text += $"\n {response.Choices[i].Delta.Content}";
        //            Debug.Log("GPT Response:\n" + text);
        //        }
        //    }
        //    else
        //    {
        //        text = "Spongebob: Hey, Gary! What's up?\r\n\r\nGary: Meow?\r\n\r\nPatrick: What's he saying?\r\n\r\nSpongebob: I think he's asking why is Mr. Krabs is eating something?\r\n\r\nMr. Krabs: I'm not eating something!\r\n\r\nSquidward: Yeah? What are you doing?\r\n\r\nMr. Krabs: Oh, I'm just trying to take a bite of Gary.\r\n\r\nPatrick:Wait a second, Mr. Krabs do you realise Gary is a pet not a meal?\r\n\r\nMr. Krabs: But he tastes so good!\r\n\r\nSquidward: No, no, no, no, no, no, no! That's not okay, Mr. Krabs! Put Gary down!\r\n\r\nSandy: Haha, this is ridiculous!\r\n\r\nSpongebob: Maybe we should just call animal care Mr. Krabs.\r\n\r\nGary: Meow.\r\n\r\nMr. Krabs: Alright, alright. I won't eat Gary. I promise.\r\n\r\nSquidward: Thank goodness.";
        //        Debug.Log("GPT Response:\n" + text);
        //    }
        //    File.WriteAllText("Assets/Scripts/Next.txt", text);
        //}
    }

    private IEnumerator Speak(List<Dialogue> dialogues)
    {
        foreach (var dialogue in dialogues)
        {
            yield return Speak(dialogue);
        }

        while (File.ReadAllText("Assets/Scripts/Next.txt") == "")
        {
            yield return null;
        }

        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    private IEnumerator CreateNewVoiceRequest(Dialogue d, Action<string> callback)
    {
        var jsonObj = new
        {
            tts_model_token = d.model,
            uuid_idempotency_token = Guid.NewGuid().ToString(),
            inference_text = d.text,
        };

        var content = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");
        var response = _client.PostAsync("https://api.fakeyou.com/tts/inference", content).Result;

        if (response.IsSuccessStatusCode)
        {
            var responseString = response.Content.ReadAsStringAsync().Result;
            var speakResponse = JsonConvert.DeserializeObject<SpeakResponse>(responseString);

            callback(speakResponse.inference_job_token);
        }
        else
        {
            Debug.LogError("Error in FakeYou API request: " + response.StatusCode);
            callback(null);
        }
        yield return null;
    }



    private IEnumerator TurnToSpeaker(Transform objectTransform, Transform speakerTransform)
    {
        Vector3 direction = (speakerTransform.position - objectTransform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            while (Quaternion.Angle(objectTransform.rotation, targetRotation) > 0.05f)
            {
                objectTransform.rotation = Quaternion.Slerp(objectTransform.rotation, targetRotation, Time.deltaTime * 2.0f);
                yield return null;
            }
        }
    }

    private IEnumerator Speak(Dialogue d)
    {
        var content = _client.GetAsync($"https://api.fakeyou.com/tts/job/{d.uuid}").Result.Content;
        var responseContent = content.ReadAsStringAsync().Result;
        var v = JsonConvert.DeserializeObject<GetResponse>(responseContent);
        Debug.Log(responseContent);

        if (v.state == null || v.state.status == "pending" || v.state.status == "started" || v.state.status == "attempt_failed")
        {
            yield return new WaitForSeconds(1.5f);
            yield return Speak(d);
        }
        else if (v.state.status == "complete_success")
        {
            videoPlayer.Stop();
            yield return HandleSuccessfulTTSRequest(d, v);
        }
        else
        {
            string newUuid = null;
            yield return CreateNewVoiceRequest(d, result => { newUuid = result; });

            if (!string.IsNullOrEmpty(newUuid))
            {
                d.uuid = newUuid;
                yield return Speak(d);
            }
            else
            {
                Debug.LogError("Failed to create new voice request");
            }
        }
    }

    private IEnumerator HandleSuccessfulTTSRequest(Dialogue d, GetResponse v)
    {
        if (GameObject.Find(d.character) != null && _cinemachineVirtualCamera != null)
        {
            GameObject character = GameObject.Find(d.character);
            Transform t = GameObject.Find(d.character).transform;

            // Update previous and current character
            previousCharacter = currentCharacter;
            currentCharacter = d.character;

            _cinemachineVirtualCamera.LookAt = t;
            _cinemachineVirtualCamera.Follow = t;

            // Turn the current speaker towards the previous speaker
            if (characters.ContainsKey(previousCharacter))
            {
                GameObject previousCharacterObject = characters[previousCharacter];
                StartCoroutine(TurnToSpeaker(t, previousCharacterObject.transform));
            }

            yield return new WaitForSeconds(1);

            if (gt.Length > 0)
            {
                foreach (GameObject obj in gt)
                {
                    if (obj != null && obj != character)
                    {
                        StartCoroutine(TurnToSpeaker(obj.transform, t));
                    }
                }
            }
        }



        if (subtitles != null)
            subtitles.text = d.text;
        videoPlayer.Stop();

        using (var uwr = UnityWebRequestMultimedia.GetAudioClip($"https://storage.googleapis.com/vocodes-public{v.state.maybe_public_bucket_wav_audio_path}", AudioType.WAV))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                audioSource.clip = DownloadHandlerAudioClip.GetContent(uwr);

                if (d.character == "squidward(Clone)")
                {
                    float[] volumeLevels = new float[] { 1.5f, 0.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 3.0f };
                    System.Random rand = new System.Random();

                    int randomIndex = rand.Next(volumeLevels.Length);
                    float randomVolume = volumeLevels[randomIndex];

                    float[] clipData = new float[audioSource.clip.samples * audioSource.clip.channels];
                    audioSource.clip.GetData(clipData, 0);
                    for (int i = 0; i < clipData.Length; i++)
                    {
                        clipData[i] *= randomVolume;
                    }

                    audioSource.clip.SetData(clipData, 0);
                }

                else if (d.character == "gary(Clone)")
                {
                    audioSource = GetComponent<AudioSource>();
                    audioSource.clip = audioClips[0];

                    audioSource.Play();
                    audioSource.Stop();
                }

                GameObject character = GameObject.Find(d.character);

                if (character != null)
                {

                    Animator characterAnimator = character.GetComponent<Animator>();
                    if (characterAnimator != null)
                    {
                        characterAnimator.SetBool("isSpeaking", true);
                    }
                }

                audioSource.Play();
                float audioStartTime = Time.time;

                while (audioSource.isPlaying && Time.time - audioStartTime <= 60.0f)
                {
                    yield return null;
                }

                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }

                if (character != null)
                {
                    Animator characterAnimator = character.GetComponent<Animator>();
                    if (characterAnimator != null)
                    {
                        characterAnimator.SetBool("isSpeaking", false);
                    }
                }

                while (audioSource.isPlaying)
                {
                    yield return null;
                }
            }
        }
    }
}