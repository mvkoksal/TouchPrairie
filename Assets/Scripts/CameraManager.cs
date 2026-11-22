
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameraManager : MonoBehaviour
{

    [Header("Managers")]
    public SupabaseStorageManager storageManager;

    [Header("Camera UI")]
    public RawImage cameraPreview;
    public RawImage capturedPhoto;

    [Header("Buttons")]
    public Button captureButton;
    public Button retakeButton;

    private WebCamTexture webcamTexture;
    private bool isCameraInitialized;

    void Start()
    {
        if (cameraPreview == null || capturedPhoto == null ||
            captureButton == null || retakeButton == null)
        {
            Debug.LogError("Camera UI references are not assigned.");
            return;
        }

        captureButton.onClick.AddListener(CapturePhoto);
        retakeButton.onClick.AddListener(RetakePhoto);

        capturedPhoto.gameObject.SetActive(false);
        retakeButton.gameObject.SetActive(false);

        StartCoroutine(StartCamera());
    }

    IEnumerator StartCamera()
    {
#if UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(
                UserAuthorization.WebCam
            );
        }

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.LogError(
                "Camera permission denied. Grant Unity access in macOS " +
                "System Settings > Privacy & Security > Camera.");
            yield break;
        }

        WebCamDevice[] devices = null;
        float deviceTimeout = Time.realtimeSinceStartup + 5f;

        while (Time.realtimeSinceStartup < deviceTimeout)
        {
            devices = WebCamTexture.devices;
            if (devices != null && devices.Length > 0)
            {
                break;
            }
            yield return null;
        }

        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("No camera detected.");
            yield break;
        }

        Debug.Log($"Found {devices.Length} camera device(s):");
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"  [{i}] {devices[i].name} (frontFacing={devices[i].isFrontFacing})");
        }

        int[] widths = { 1280, 640, 480, 320, 0 };
        int[] heights = { 720, 480, 360, 240, 0 };
        bool frameReceived = false;
        string cameraName = null;
        WebCamDevice selectedDevice = devices[0];

        foreach (WebCamDevice device in devices)
        {
            for (int i = 0; i < widths.Length; i++)
            {
                if (webcamTexture != null)
                {
                    webcamTexture.Stop();
                    webcamTexture = null;
                }

                webcamTexture = widths[i] == 0
                    ? new WebCamTexture(device.name)
                    : new WebCamTexture(device.name, widths[i], heights[i], 30);
                cameraPreview.texture = webcamTexture;
                webcamTexture.Play();

                float timeout = Time.realtimeSinceStartup + 8f;
                while (Time.realtimeSinceStartup < timeout)
                {
                    if (webcamTexture.isPlaying &&
                        webcamTexture.width > 16 &&
                        webcamTexture.height > 16 &&
                        webcamTexture.didUpdateThisFrame)
                    {
                        frameReceived = true;
                        break;
                    }
                    yield return null;
                }

                if (frameReceived)
                {
                    cameraName = device.name;
                    selectedDevice = device;
                    break;
                }

                webcamTexture.Stop();
                Debug.LogWarning(
                    $"Camera {device.name} at {widths[i]}x{heights[i]} did not produce a frame.");
            }

            if (frameReceived)
            {
                break;
            }
        }

        if (!frameReceived ||
            webcamTexture == null ||
            !webcamTexture.isPlaying ||
            webcamTexture.width <= 16 ||
            webcamTexture.height <= 16)
        {
            isCameraInitialized = false;
            Debug.LogError(
                "Camera timed out while starting. Verify macOS camera permission " +
                "and close other apps using the camera.");
            yield break;
        }

        cameraPreview.rectTransform.localEulerAngles =
            new Vector3(0f, 0f, -webcamTexture.videoRotationAngle);
        cameraPreview.uvRect = selectedDevice.isFrontFacing
            ? new Rect(1f, 0f, -1f, 1f)
            : new Rect(0f, 0f, 1f, 1f);

        isCameraInitialized = true;
        Debug.Log("Camera started: " + cameraName);
#else
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(
                UserAuthorization.WebCam
            );
        }

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.LogError("Camera permission denied.");
            yield break;
        }

        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("No camera detected.");
            yield break;
        }

        string cameraName = devices[0].name;

        foreach (WebCamDevice device in devices)
        {
            if (!device.isFrontFacing)
            {
                cameraName = device.name;
                break;
            }
        }

        WebCamDevice selectedDevice = devices[0];
        foreach (WebCamDevice device in devices)
        {
            if (device.name == cameraName)
            {
                selectedDevice = device;
                break;
            }
        }

        webcamTexture = new WebCamTexture(cameraName, 1280, 720, 30);
        cameraPreview.texture = webcamTexture;
        webcamTexture.Play();

        while (webcamTexture.width <= 16)
        {
            yield return null;
        }

        cameraPreview.rectTransform.localEulerAngles =
            new Vector3(0f, 0f, -webcamTexture.videoRotationAngle);
        cameraPreview.uvRect = selectedDevice.isFrontFacing
            ? new Rect(1f, 0f, -1f, 1f)
            : new Rect(0f, 0f, 1f, 1f);

        isCameraInitialized = true;
        Debug.Log("Camera started: " + cameraName);
#endif
    }


    void CapturePhoto()
    {
        if (webcamTexture == null ||
            !webcamTexture.isPlaying ||
            !isCameraInitialized)
        {
            return;
        }

        Texture2D photo = new Texture2D(
            webcamTexture.width,
            webcamTexture.height,
            TextureFormat.RGB24,
            false
        );

        photo.SetPixels(webcamTexture.GetPixels());
        photo.Apply();

        capturedPhoto.texture = photo;

       
        if (storageManager != null)
        {
            storageManager.SetCapturedPhoto(photo);
        }
        else
        {
            Debug.LogError("SupabaseStorageManager not found!");
        }

        // Copy the live camera's orientation and mirroring.
        capturedPhoto.rectTransform.localEulerAngles =
            cameraPreview.rectTransform.localEulerAngles;

        capturedPhoto.uvRect = cameraPreview.uvRect;

        webcamTexture.Stop();

        cameraPreview.gameObject.SetActive(false);
        capturedPhoto.gameObject.SetActive(true);

        captureButton.gameObject.SetActive(false);
        retakeButton.gameObject.SetActive(true);
    }


    void RetakePhoto()
    {
        capturedPhoto.gameObject.SetActive(false);
        cameraPreview.gameObject.SetActive(true);

        retakeButton.gameObject.SetActive(false);
        captureButton.gameObject.SetActive(true);

        webcamTexture.Play();
    }

    void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}
