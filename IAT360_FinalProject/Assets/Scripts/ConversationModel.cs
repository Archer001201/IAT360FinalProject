using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConversationModel : MonoBehaviour
{
    [Header("API Settings")]
    public string apiURL;
    public string apiKey;
    
    [Header("Prompts")]
    public int maxContextLength = 10;
    public string culprit;
    private readonly List<Dictionary<string, string>> _messages = new();
    private string _responseLog;

    [Header("Buttons")] 
    public Button startButton;
    public Button firstButton;
    public Button resultButton;
    public Button quitButton;
    public List<Button> titleButtons;
    public List<Button> areaButtons;
    public List<Button> suspectButtons;
    public List<Button> identifyButtons;

    [Header("Panels")] 
    public GameObject titlePanel;
    public GameObject gameViewPanel;
    public GameObject resultPanel;
    public GameObject tipsPanel;
    public GameObject informationPanel;
    public GameObject actionPanel;
    public TextHeightAdjustment logText;
    public TextHeightAdjustment resultText;
    public TextMeshProUGUI actionText;

    [Header("State")] 
    public int totalActionCount;
    public bool isGenerating;
    public bool isIdentified;
    private int _takenActionCount;
    private GameState _state;
    
    private enum GameState
    {
        Start, Playing, Finished
    }

    private void Awake()
    {
        _state = GameState.Start;
        
        gameViewPanel.SetActive(false);
        resultPanel.SetActive(false);
        titlePanel.SetActive(true);
        tipsPanel.SetActive(false);
        informationPanel.SetActive(false);
        resultButton.gameObject.SetActive(false);
        actionPanel.SetActive(false);
        
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        _messages.Add(new Dictionary<string, string>
        {
            { "role", "system" },
            {
                "content", "Let's play a detective game." +
                           $"Create a unique detective story first. This session ID is {timestamp}." +
                           "Please provide the following case details, 5 areas to explore, and 5 suspects in the given format: " +
                           "--- " + "***Start***" +
                           "**Case Background** " +
                           "Case Description: [Description here] " +
                           "Victim Information: [Description here] " +
                           "**Areas** " +
                           "[Area 1] [Area 2] [Area 3] ... " +
                           "**Suspects** " +
                           "[Name 1] - [Role] [Name 2] - [Role] [Name 3] - [Role] ... " +
                           "**Culprit** " + "[Name of the culprit, formation follow the suspect above] " + "**CulpritEnd** " +
                           "***End***" + "--- " +
                           "Ensure the information is suitable for a detective game with a clear and concise format. Keep the 'Areas' section as simple location names, and in the 'Suspects' section include only the names and roles without additional explanations."
            }
        });

        actionText.text = "Action Remain: " + (totalActionCount - _takenActionCount);
    }

    private void Start()
    {
        startButton.onClick.AddListener(() =>
        {
            AddMessage("user", "Provide the case background.");
            StartCoroutine(SendRequest(500));
            titlePanel.SetActive(false);
            gameViewPanel.SetActive(true);
        });

        foreach (var aBtn in areaButtons)
        {
            aBtn.onClick.AddListener(() =>
            {
                var area = aBtn.GetComponentInChildren<TextMeshProUGUI>().text;
                RequestClue("Area", area);
            });
        }
        
        foreach (var sBtn in suspectButtons)
        {
            sBtn.onClick.AddListener(() =>
            {
                var suspect = sBtn.GetComponentInChildren<TextMeshProUGUI>().text;
                RequestClue("Suspect", suspect);
            });
        }
        
        foreach (var iBtn in identifyButtons)
        {
            iBtn.onClick.AddListener(() =>
            {
                if (isGenerating)
                {
                    return;
                }
                
                if (isIdentified)
                {
                    StartCoroutine(ShowTips("Culprit is identified"));
                    return; 
                }
                
                var suspect = iBtn.GetComponentInChildren<TextMeshProUGUI>().text;
                var str = "The Culprit is " + culprit + ".";
                if (suspect.Contains(culprit) || culprit.Contains(suspect))
                {
                    str += " Right.";
                }
                else
                {
                    str += " Wrong.";
                }

                informationPanel.GetComponentInChildren<TextMeshProUGUI>().text = str;
                resultButton.gameObject.SetActive(true);
                isIdentified = true;
            });
        }
        
        resultButton.onClick.AddListener(() =>
        {
            _state = GameState.Finished;
            RequestResult();
            titlePanel.SetActive(false);
            gameViewPanel.SetActive(false);
            resultPanel.SetActive(true);
        });

        foreach (var tBtn in titleButtons)
        {
            tBtn.onClick.AddListener(() =>
            {
                SceneManager.LoadScene("SampleScene");
            });
        }
        
        quitButton.onClick.AddListener(Application.Quit);
    }
    
    private void RequestClue(string type, string target)
    {
        if (isGenerating)
        {
            return;
        }
        
        if (_takenActionCount > totalActionCount - 1)
        {
            StartCoroutine(ShowTips("No more actions"));
            return;
        }
        
        if (isIdentified)
        {
            StartCoroutine(ShowTips("Culprit is identified"));
            return;
        }

        _takenActionCount++;
        actionText.text = "Action Remain: " + (totalActionCount - _takenActionCount);
        AddMessage("user", $"Provide a piece of clue about the {type}: {target}, start with ***Start*** and end with ***End***");
        StartCoroutine(SendRequest(300));
    }

    private void RequestResult()
    {
        AddMessage("user", $"The culprit is {culprit}. " + 
                           "Explain in detail about the motive of culprit and how to commit the crime. Start the explain with ***Start*** and end with ***End***");
        StartCoroutine(SendRequest(1000));
    }

    private void AddMessage(string role, string content)
    {
        _messages.Add(new Dictionary<string, string>
        {
            { "role", role },
            { "content", content }
        });

        if (_messages.Count > maxContextLength)
        {
            _messages.RemoveAt(1);
        }
    }
    
    private string ConstructPrompt()
    {
        var prompt = _messages.Aggregate("", (current, message) => current + $"{message["role"]}: {message["content"]} --- ");
        prompt += "Assistant:";
        return prompt;
    }
    
    private IEnumerator SendRequest(int maxTokens)
    {
        isGenerating = true;
        tipsPanel.GetComponentInChildren<TextMeshProUGUI>().text = "Generating ... ...";
        tipsPanel.SetActive(true);
        informationPanel.SetActive(false);
        
        var prompt = ConstructPrompt();
        var json = "{\"model\": \"Qwen/Qwen2.5-Coder-32B-Instruct\"," +
                   "\"messages\": [{\"role\": \"user\", \"content\": \"" + prompt + "\"}]," +
                   "\"max_tokens\": " + maxTokens + "," +
                   "\"temperature\": 0.8," +
                   "\"top_p\": 0.9," +
                   "\"stream\": false}";

        using var webRequest = new UnityWebRequest(apiURL, "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");
        webRequest.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return webRequest.SendWebRequest();

        if (webRequest.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("Error: " + webRequest.error);
        }
        else
        {
            Debug.Log(webRequest.downloadHandler.text);
            var response = ExtractSection(webRequest.downloadHandler.text, "***Start***", "***End***");
            AddMessage("assistant", response);
            
            switch (_state)
            {
                case GameState.Start:
                {
                    var caseBackground = ExtractSection(response, "**Case Background**", "**Areas**");
                    informationPanel.GetComponentInChildren<TextMeshProUGUI>().text = caseBackground;
                    _responseLog += caseBackground;

                    var areas = ExtractSection(response, "**Areas**", "**Suspects**");
                    UpdateButtons(areas, areaButtons);

                    var suspects = ExtractSection(response, "**Suspects**", "**Culprit**");
                    UpdateButtons(suspects, suspectButtons);
                    UpdateButtons(suspects, identifyButtons);

                    culprit = ExtractSection(response, "**Culprit**", "**CulpritEnd**").Replace("\\n", "").Replace("\\r", "");
                    firstButton.onClick.Invoke();
                    logText.UpdateText(_responseLog);
                    actionPanel.SetActive(true);
                    _state = GameState.Playing;
                    break;
                }
                case GameState.Playing:
                    informationPanel.GetComponentInChildren<TextMeshProUGUI>().text = response;
                    _responseLog += response;
                    logText.UpdateText(_responseLog);
                    break;
                case GameState.Finished:
                default:
                    resultText.UpdateText(response);
                    break;
            }
        }
        
        tipsPanel.SetActive(false);
        informationPanel.SetActive(true);
        isGenerating = false;
    }
    
    private static string ExtractSection(string content, string startMarker, string endMarker)
    {
        var startIndex = content.IndexOf(startMarker, StringComparison.Ordinal) + startMarker.Length;
        var endIndex = content.IndexOf(endMarker, StringComparison.Ordinal);
        if (startIndex < 0 || endIndex < 0 || startIndex > endIndex) return string.Empty;
        return content.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private static void UpdateButtons(string content, List<Button> buttons)
    {
        content = content.Replace("\\n", "\n").Replace("\\r", "\r");
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < lines.Length && i < buttons.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                buttons[i].GetComponentInChildren<TextMeshProUGUI>().text = lines[i].Trim();
            }
        }
    }

    private IEnumerator ShowTips(string content)
    {
        tipsPanel.SetActive(true);
        tipsPanel.GetComponentInChildren<TextMeshProUGUI>().text = content;
        yield return new WaitForSeconds(2);
        tipsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
