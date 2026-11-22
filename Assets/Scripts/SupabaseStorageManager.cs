
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Runtime.InteropServices;

[System.Serializable]
public class RecognitionRequest
{
    public string object_path;
    public string quest_target;
}

[System.Serializable]
public class RecognitionResponse
{
    public bool success;
    public string identified_object;
    public string category;
    public float confidence;
    public bool matches_quest;
    public string reason;
}

public class SupabaseStorageManager : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RemoveBackgroundAndUpload(
        byte[] imageBytes,
        int imageLength,
        string projectUrl,
        string anonKey,
        string callbackObject,
        string callbackMethod
    );
#endif

    [Header("Quest Recognition")]
    public QuestManager questManager;
    public TMP_Text recognitionResultText;

    [Header("Supabase")]
    private string projectUrl =
        "https://irrcsuojvvyyoxfvwtcw.supabase.co";

    private string anonKey =
        "sb_publishable_ElwvbbodWfvNH_NrImHKgg_3OllgtwE";

    private string bucketName = "quest_photos";

    [Header("UI")]
    public RawImage uploadedPhotoDisplay;
    public Button uploadButton;

    private Texture2D capturedPhoto;
    private string uploadedFilePath;
    private bool isUploading;

    void Start()
    {
        if (uploadButton != null)
        {
            uploadButton.onClick.AddListener(UploadCapturedPhoto);
            uploadButton.interactable = false;
        }

        if (questManager == null)
        {
            Debug.LogError("QuestManager is not assigned!");
        }
    }

    public void SetCapturedPhoto(Texture2D photo)
    {
        capturedPhoto = photo;

        if (uploadButton != null)
            uploadButton.interactable = photo != null && !isUploading;
    }

    public void UploadCapturedPhoto()
    {
        if (capturedPhoto == null)
        {
            Debug.LogError("No captured photo to upload.");
            return;
        }

        if (isUploading)
            return;

        StartCoroutine(UploadPhoto());
    }

    IEnumerator UploadPhoto()
    {
        isUploading = true;

        if (uploadButton != null)
            uploadButton.interactable = false;

        byte[] imageBytes = capturedPhoto.EncodeToJPG(80);

        uploadedFilePath = "test-" + Guid.NewGuid().ToString() + ".jpg";

        string url = projectUrl.TrimEnd('/') +
            "/storage/v1/object/" +
            bucketName + "/" + uploadedFilePath;

        Debug.Log("Uploading photo: " + url);

        UnityWebRequest request = new UnityWebRequest(url, "POST");

        request.uploadHandler = new UploadHandlerRaw(imageBytes);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("apikey", anonKey);
        request.SetRequestHeader("Authorization", "Bearer " + anonKey);
        request.SetRequestHeader("Content-Type", "image/jpeg");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Photo uploaded successfully!");
            Debug.Log("File path: " + uploadedFilePath);

            StartCoroutine(DownloadPhoto(uploadedFilePath));

            yield return StartCoroutine(RecognizePhoto(uploadedFilePath));
        }
        else
        {
            Debug.LogError(
                "Upload failed: " +
                request.responseCode + " " +
                request.error + " " +
                request.downloadHandler.text
            );

            if (recognitionResultText != null)
                recognitionResultText.text = "Photo upload failed. Try again!";
        }

        request.Dispose();

        isUploading = false;

        if (uploadButton != null)
            uploadButton.interactable = capturedPhoto != null;
    }

    IEnumerator RecognizePhoto(string filePath)
    {
        if (questManager == null)
        {
            Debug.LogError("QuestManager is not assigned!");
            yield break;
        }

        List<QuestData> activeQuests = questManager.GetActiveQuests();

        if (activeQuests.Count == 0)
        {
            Debug.LogWarning("No available quests to check.");

            if (recognitionResultText != null)
                recognitionResultText.text = "No available quests!";

            yield break;
        }

        Debug.Log("Checking photo against " + activeQuests.Count + " quests.");

        bool questCompleted = false;

        foreach (QuestData quest in activeQuests)
        {
            // Use the quest title as the recognition target.
            string questTarget = GetQuestTarget(quest);

            Debug.Log("Checking against quest: " + quest.title +
                      " | Target: " + questTarget);

            string url = projectUrl.TrimEnd('/') +
                "/functions/v1/recognize-photo";

            RecognitionRequest body = new RecognitionRequest
            {
                object_path = filePath,
                quest_target = questTarget
            };

            string json = JsonUtility.ToJson(body);
            byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest request = new UnityWebRequest(url, "POST");

            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("apikey", anonKey);
            request.SetRequestHeader("Authorization", "Bearer " + anonKey);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "Recognition failed: " +
                    request.responseCode + " " +
                    request.downloadHandler.text
                );

                request.Dispose();
                continue;
            }

            string responseJson = request.downloadHandler.text;

            Debug.Log("Gemini response: " + responseJson);

            RecognitionResponse result =
                JsonUtility.FromJson<RecognitionResponse>(responseJson);

            request.Dispose();

            if (result != null && result.success && result.matches_quest)
            {
                questCompleted = true;

                QuestResult completion =
                    questManager.CompleteQuest(quest.id);

                if (completion != null)
                {
                    Debug.Log("QUEST COMPLETED: " + quest.title);
                    Debug.Log("XP gained: " + completion.xpGained);

                    if (recognitionResultText != null)
                    {
                        recognitionResultText.text =
                            "Quest completed! " + quest.title +
                            "\n+" + completion.xpGained + " XP" +
                            "\nFound: " + result.identified_object;
                    }
                }

                break;
            }
        }

        if (!questCompleted && recognitionResultText != null)
        {
            recognitionResultText.text =
                "No quest completed. Keep exploring!";
        }
    }

    string GetQuestTarget(QuestData quest)
    {
        switch (quest.id)
        {
            case "q1":
                return "tree";

            case "q2":
                return "book";

            case "q3":
                return "coffee cup";

            default:
                return quest.title;
        }
    }

    public void CreateCollectible()
    {
        if (capturedPhoto == null)
        {
            Debug.LogError("No captured photo available!");
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        byte[] imageBytes = capturedPhoto.EncodeToJPG(90);

        Debug.Log("Starting background removal...");

        RemoveBackgroundAndUpload(
            imageBytes,
            imageBytes.Length,
            projectUrl,
            anonKey,
            gameObject.name,
            "OnBackgroundRemovalComplete"
        );
#else
        Debug.LogWarning("Background removal only works in a WebGL build.");
#endif
    }

    public void OnBackgroundRemovalComplete(string json)
    {
        Debug.Log("Background removal response: " + json);

        BackgroundRemovalResponse result =
            JsonUtility.FromJson<BackgroundRemovalResponse>(json);

        if (result != null && result.success)
        {
            Debug.Log("Collectible created: " + result.asset_path);

            if (recognitionResultText != null)
                recognitionResultText.text = "Collectible created!";
        }
        else
        {
            string error = result != null ? result.error : "Invalid response";

            Debug.LogError("Background removal failed: " + error);

            if (recognitionResultText != null)
                recognitionResultText.text =
                    "Couldn't create collectible. Try again!";
        }
    }

    IEnumerator DownloadPhoto(string filePath)
    {
        string url = projectUrl.TrimEnd('/') +
            "/storage/v1/object/public/" +
            bucketName + "/" + filePath;

        UnityWebRequest request = UnityWebRequest.Get(url);

        request.SetRequestHeader("apikey", anonKey);
        request.SetRequestHeader("Authorization", "Bearer " + anonKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D downloadedTexture = new Texture2D(2, 2);

            if (downloadedTexture.LoadImage(request.downloadHandler.data))
            {
                if (uploadedPhotoDisplay != null)
                    uploadedPhotoDisplay.texture = downloadedTexture;

                Debug.Log("Photo retrieved and displayed successfully!");
            }
            else
            {
                Debug.LogError("Could not decode downloaded image.");
            }
        }
        else
        {
            Debug.LogError(
                "Download failed: " +
                request.responseCode + " " +
                request.error + " " +
                request.downloadHandler.text
            );
        }

        request.Dispose();
    }
}

[System.Serializable]
public class BackgroundRemovalResponse
{
    public bool success;
    public string asset_path;
    public string error;
}
