using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenAI;
using UnityEngine.Events;
using TMPro; // For TextMeshPro input/output fields
using UnityEngine.Networking; // For HTTP requests
using Newtonsoft.Json; // For parsing JSON data

public class ChatGPTManager : MonoBehaviour
{
    public TMP_InputField inputField; // Reference to the input field (Enter text box)
    public TMP_Text outputText;       // Reference to the output text field (Grey box)
    public OnResponseEvent OnResponse;

    [System.Serializable]
    public class OnResponseEvent : UnityEvent<string> { }

    private OpenAIApi openAI; // OpenAI API instance
    private List<ChatMessage> messages = new List<ChatMessage>();

    // Direct public link to your JSON file on Google Drive
    private string jsonFileUrl = "https://drive.google.com/uc?export=download&id=1LWjG7ams76R3YEOwJF2Gduf-EDQZ-6DO"; 

    void Start()
    {
        // Initialize ChatGPT with a system message
        messages.Add(new ChatMessage { Role = "system", Content = "You are a helpful assistant." });

        // Start the process of fetching the API key and organization from Google Drive
        StartCoroutine(FetchAPIKeyFromGoogleDrive());
    }

    private IEnumerator FetchAPIKeyFromGoogleDrive()
    {
        UnityWebRequest request = UnityWebRequest.Get(jsonFileUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Successfully fetched JSON file from Google Drive.");

            try
            {
                // Parse the JSON file content to extract the API key and organization
                var jsonData = JsonConvert.DeserializeObject<JsonData>(request.downloadHandler.text);
                string apiKey = jsonData.api_key;
                string organization = jsonData.organization;

                Debug.Log("Fetched API Key: "); // Debug to verify
                Debug.Log("Fetched Organization: "); // Debug to verify

                // Initialize OpenAI API with the fetched API key and organization
                openAI = new OpenAIApi(apiKey, organization);

                Debug.Log("OpenAI API initialized successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to parse JSON file: " + ex.Message);
            }
        }
        else
        {
            Debug.LogError("Failed to fetch JSON file: " + request.error);
        }
    }

    public void OnSendButtonClick()
    {
        if (!string.IsNullOrEmpty(inputField.text))
        {
            string userInput = inputField.text;
            inputField.text = ""; // Clear the input field
            AskChatGPT(userInput); // Send the user input to ChatGPT
        }
    }

    public async void AskChatGPT(string newText)
    {
        if (openAI == null)
        {
            Debug.LogError("OpenAI API is not initialized. Cannot send message.");
            return;
        }

        ChatMessage newMessage = new ChatMessage();
        newMessage.Content = newText;
        newMessage.Role = "user";

        messages.Add(newMessage);

        CreateChatCompletionRequest request = new CreateChatCompletionRequest();
        request.Messages = messages;
        request.Model = "gpt-3.5-turbo";

        var response = await openAI.CreateChatCompletion(request);

        if (response.Choices != null && response.Choices.Count > 0)
        {
            var chatResponse = response.Choices[0].Message;
            messages.Add(chatResponse);

            Debug.Log(chatResponse.Content);

            outputText.text = chatResponse.Content; // Display ChatGPT's response
            OnResponse.Invoke(chatResponse.Content);
        }
    }

    // Class for deserializing JSON data
    private class JsonData
    {
        public string api_key;     // API key field in the JSON file
        public string organization; // Organization field in the JSON file
    }
}
