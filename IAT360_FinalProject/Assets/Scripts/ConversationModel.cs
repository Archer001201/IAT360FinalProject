using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ConversationModel : MonoBehaviour
{
    [Header("API Settings")]
    public string apiURL;
    public string apiKey;
    
    [Header("Prompts")]
    [TextArea] public string currentPrompt;
    [TextArea] public List<string> responseList;
    [HideInInspector] public string allResponses;
    [HideInInspector] public string startPrompt;
    [HideInInspector] public string areaPrompt;
    [HideInInspector] public string suspectPrompt;

    [Header("Buttons")] 
    public Button startButton;
    public List<Button> areaButtons;
    public List<Button> suspectButtons;

    [Header("Panels")] 
    public GameObject titlePanel;
    public GameObject gameViewPanel;
    public GameObject tipsPanel;
    public GameObject informationPanel;

    private GameState _state;
    
    private enum GameState
    {
        Start, Playing, Finished
    }

    private void Awake()
    {
        _state = GameState.Start;
        
        gameViewPanel.SetActive(false);
        titlePanel.SetActive(true);
        tipsPanel.SetActive(false);
        informationPanel.SetActive(false);
        
        startPrompt = "Please answer the following case details, areas to explore, and suspects in the given format: " +
                      "--- " + "***Start***" +
                      "**Case Background** " +
                      "Case Description: [Description here] " +
                      "Victim Information: [Description here] " +
                      "**Areas** " +
                      "[Area 1] [Area 2] [Area 3] ... " +
                      "**Suspects** " +
                      "[Name 1] - [Role] [Name 2] - [Role] [Name 3] - [Role] ... " +
                      "**Culprit** " +
                      "[Name of the culprit] - [Explain the motive and how they committed the crime] " +
                      "***End***" + "--- " +
                      "Ensure the information is suitable for a detective game with a clear and concise format. Keep the 'Areas' section as simple location names, and in the 'Suspects' section include only the names and roles without additional explanations.";
        areaPrompt = "Please answer the clue about the case from an area based on the given premise and in the given format: " +
                     "premise: " + allResponses +
                     "--- " + "***Start***" + "[Area] - [Detailed Clue]" + "***End***" + "---";
        suspectPrompt = "Please answer the clue about the case from a suspect based on the given premise and in the given format: " +
                        "premise: " + allResponses +
                        "--- " + "***Start***" + "[Suspect] - [Detailed Clue]" + "***End***" + "---";
    }

    private void Start()
    {
        startButton.onClick.AddListener(() =>
        {
            currentPrompt = startPrompt;
            StartCoroutine(SendRequest(currentPrompt,500));
            titlePanel.SetActive(false);
            gameViewPanel.SetActive(true);
        });

        foreach (var aBtn in areaButtons)
        {
            aBtn.onClick.AddListener(() =>
            {
                currentPrompt = areaPrompt + " Area to explore: " + aBtn.GetComponentInChildren<TextMeshProUGUI>().text;
                StartCoroutine(SendRequest(currentPrompt,300));
            });
        }
        
        foreach (var sBtn in suspectButtons)
        {
            sBtn.onClick.AddListener(() =>
            {
                currentPrompt = suspectPrompt + " Suspect to ask: " + sBtn.GetComponentInChildren<TextMeshProUGUI>().text;
                StartCoroutine(SendRequest(currentPrompt,300));
            });
        }
    }
    
    private IEnumerator SendRequest(string prompt, int maxTokens)
    {
        tipsPanel.GetComponentInChildren<TextMeshProUGUI>().text = "Generating ... ...";
        tipsPanel.SetActive(true);
        informationPanel.SetActive(false);
        
        var json = "{\"model\": \"Qwen/Qwen2.5-Coder-32B-Instruct\"," +
                   "\"messages\": [{\"role\": \"user\", \"content\": \"" + prompt + "\"}]," +
                   "\"max_tokens\": " + maxTokens + "," +
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
            Debug.Log("Received: " + webRequest.downloadHandler.text);
            ParseContent(webRequest.downloadHandler.text);
        }
        
        tipsPanel.SetActive(false);
        informationPanel.SetActive(true);
    }
    
    private void ParseContent(string content)
    {
        if (content == string.Empty)
        {
            return;
        }

        var response = ExtractSection(content, "***Start***", "***End***");
        responseList.Add(response);

        if (_state == GameState.Start)
        {
            // 提取背景信息
            var caseBackground = ExtractSection(content, "**Case Background**", "**Areas**");
            informationPanel.GetComponentInChildren<TextMeshProUGUI>().text = caseBackground;

            // 提取区域
            var areas = ExtractSection(content, "**Areas**", "**Suspects**");
            UpdateButtons(areas, areaButtons);
        
            // 提取嫌疑人
            var suspects = ExtractSection(content, "**Suspects**", "**Culprit**");
            UpdateButtons(suspects, suspectButtons);

            _state = GameState.Playing;
        }
        else
        {
            informationPanel.GetComponentInChildren<TextMeshProUGUI>().text = response;
        }
        
        allResponses += response;
        Debug.Log("all response: " + allResponses);
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
    
}
