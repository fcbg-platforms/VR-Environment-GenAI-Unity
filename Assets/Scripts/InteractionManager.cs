using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

using AiWorldGeneration.ASR;
using AiWorldGeneration.Skybox;
using AiWorldGeneration.TCP;


namespace AiWorldGeneration
{
    /// <summary>
    /// Handles the interactions betwwen the user and the scene components.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {

        [Tooltip("Show the progress on the progress bar")]
        [SerializeField]
        bool showProgress;

        [Header("Controller scripts")]

        [Tooltip("Speech recognition component.")]
        [SerializeField]
        AudioRecorder audioRecorder;

        [SerializeField, Tooltip("UI Controller script.")]
        UIManager uiManager;

        [Tooltip("Client controller for the TCP connection.")]
        [SerializeField]
        ClientController clientController;

        [Tooltip("Main component to control the behavior of the skybox interactions.")]
        [SerializeField]
        SkyboxOrchestrator skyboxOrchestrator;

        [Tooltip("Main component for the inpainting feature.")]
        [SerializeField]
        SkyboxMasker skyboxMasker;

        [Header("Advanced Configuration")]

        [Tooltip(
            "Initial path to the skybox texture at the start of the scene. " +
            "Don't forget to change it in build configuration."
        )]
        [SerializeField]
        string skytexturePath;

        /// <summary>
        /// Tracks if the audio is currently being recorded.
        /// </summary>
        bool isRecordingAudio;

        /// <summary>
        /// Control which phase of the skybox generation it is.
        /// </summary>
        [SerializeField]
        int skyboxPhase;

        /// <summary>
        /// If a skybox task is running.
        /// </summary
        bool skyboxTaskRunning;

        /// <summary>
        /// Start an inpainting mode.
        /// </summary>
        bool inpaintingMode;


        /// <summary>
        /// Changes the UI state based on the selected edition mode.
        /// </summary>
        /// <param name="editionValue">Current dropdown value.</param>
        public void OnEditionModeChange(int editionValue)
        {
            var editionModeText = uiManager.GetEditionMode(editionValue);
            if (editionModeText == EditionMode.PAINTING)
            {
                skyboxMasker.PaintingEnabled = true;
            }
        }

        /// <summary>
        /// Changes the UI state based on the selected edition mode.
        /// The dropdown value is automatically selected.
        /// </summary>
        public void OnEditionModeChange()
        {
            if (uiManager.GetEditionMode(inpaintingMode) == EditionMode.PAINTING)
            {
                skyboxMasker.PaintingEnabled = true;
            }
        }

        /// <summary>
        /// Changes the edition mode and triggers the associated events.
        /// </summary>
        /// <param name="newValue">Value to set to the dropdown.</param>
        public void ChangeEditionMode(int newValue)
        {
            uiManager.SetEditionMode(newValue);
            OnEditionModeChange();
        }

        /// <summary>
        /// Set the value of the inpainting mode.
        /// </summary>
        /// <param name="newValue">true if the current state is inpainting.</param>
        public void SetInpaintingMode(bool newValue)
        {
            inpaintingMode = newValue;
        }


        /// <summary>
        /// Initiates speech recording.
        /// </summary>
        public void StartSpeechRecording()
        {
            isRecordingAudio = true;
            uiManager.SetRecordingButtonText("Speak...");
            audioRecorder.StartRecording();
            // If the audio is too long, stop early
            Invoke(nameof(AnalyseSpeech), audioRecorder.maxAudioLength);
        }

        /// <summary>
        /// Sets the good input field's text to the transcription and enables the recording button.
        /// 
        /// It uses the value of <see cref="inpaintingMode"/> to find the good text area to set. 
        /// </summary>
        /// <param name="transcription">The transcribed speech from the audio recording.</param>
        void CompleteSpeechRecording(string transcription)
        {
            uiManager.SetInputFieldText(transcription, inpaintingMode);

            audioRecorder.OnComplete();
        }

        /// <summary>
        /// Stops the recording.
        /// </summary>
        /// <returns>Audio recording bytes in WAV format.</returns>
        public void StopSpeechRecording()
        {
            isRecordingAudio = false;
            audioRecorder.StopRecording();
        }


        /// <summary>
        /// Stops speech recording and do the transcription.
        /// </summary>
        public void AnalyseSpeech()
        {
            CancelInvoke(nameof(AnalyseSpeech));
            isRecordingAudio = false;

            uiManager.SetAudioAnalyse();

            var audioBytes = audioRecorder.StopRecording();

            var tcs = new TaskCompletionSource<string>();
            Task.Run(async () =>
            {
                tcs.SetResult(await clientController.TextFromAudio(audioBytes));
            });

            // ConfigureAwait must be true to get unity main thread context
            tcs.Task.ConfigureAwait(true).GetAwaiter().OnCompleted(() =>
            {
                CompleteSpeechRecording(tcs.Task.Result);
            });
        }

        /// <summary>
        /// Toggles the speech recording feature.
        /// </summary>
        public void ToggleSpeechRecording()
        {
            if (isRecordingAudio)
            {
                AnalyseSpeech();
            }
            else
            {
                StartSpeechRecording();
            }
        }


        /// <summary>
        /// Handles the changes in the generation process state.
        /// </summary>
        /// <param name="isGenerationStarting">
        /// Indicates whether the generation process is starting or not.
        /// </param>
        void OnGenerationChange(bool isGenerationStarting)
        {
            uiManager.SetGenerationChange(isGenerationStarting);
            skyboxTaskRunning = isGenerationStarting;
        }


        /// <summary>
        /// Handles the submission of a new generation request based on the expert mode.
        /// </summary>
        /// <param name="expertMode">
        /// Indicates whether the user wants to use expert mode for the skybox generation.
        /// If true, the skybox will be generated in several phases.
        /// </param>
        public void OnSubmitGeneration(bool expertMode)
        {
            OnGenerationChange(true);
            skyboxOrchestrator.GenerateNewSkybox("(landscape), (photorealistic), " + uiManager.GetPrompt(), expertMode);
            skyboxPhase = expertMode ? 1 : 4;
        }


        /// <summary>
        /// Handles the submission of a new generation request based on the expert mode.
        /// </summary>
        /// <param name="expertMode">
        /// Indicates whether the user wants to use expert mode for the skybox generation.
        /// If true, the skybox will be generated in several phases.
        /// </param>
        public void OnSubmitGeneration()
        {
            OnSubmitGeneration(uiManager.IsModeExpert());
        }

        /// <summary>
        /// Submits the user's input to the appropriate skybox creation or inpainting process.
        /// </summary>
        public void OnSubmitPrompt()
        {
            if (uiManager.GetEditionMode(inpaintingMode) == EditionMode.PAINTING)
            {
                OnGenerationChange(true);
                skyboxOrchestrator.StartInpainting(uiManager.GetInpaintingPrompt());
            }
            else
            {
                OnSubmitGeneration(false);
            }
        }

        /// <summary>
        /// Start an inpainting task.
        /// </summary>
        public void OnInpaintingSubmit()
        {
            OnGenerationChange(true);
            skyboxOrchestrator.StartInpainting(uiManager.GetInpaintingPrompt());
        }


        /// <summary>
        /// Call it whenever a skybox task gets completed.
        /// </summary>
        public void OnCompletePrompt()
        {
            OnGenerationChange(false);
            uiManager.SetActiveButtons(skyboxPhase);
        }

        /// <summary>
        /// Starts a skybox refining process.
        /// </summary>
        public void OnRefine()
        {
            OnGenerationChange(true);
            skyboxOrchestrator.RefineSkybox(uiManager.GetPrompt());
            skyboxPhase = 2;
        }


        /// <summary>
        /// Starts a skybox refining process.
        /// </summary>
        public void OnRemoveSeam()
        {
            OnGenerationChange(true);
            skyboxOrchestrator.RemoveSeam();
            skyboxPhase = 3;
        }


        /// <summary>
        /// Starts a skybox extension process.
        /// </summary>
        public void OnExtendSkybox()
        {
            OnGenerationChange(true);
            skyboxOrchestrator.ExtendSkybox();
            skyboxPhase = 4;
        }

        /// <summary>
        /// Set the current texture path, in editor only.
        /// </summary>
        /// <returns>The texture path.</returns>
        void SetTexturePath()
        {
#if UNITY_EDITOR
            var skyTexture = RenderSettings.skybox.GetTexture("_MainTex");
            var currentTexturePath = AssetDatabase.GetAssetPath(skyTexture);
            if (skytexturePath != currentTexturePath)
            {
                Debug.LogWarning(
                    "The skybox texture path is different from the current texture," +
                    "it may result in unchangeable skybox in build mode. " +
                    "Registred path '" + skytexturePath + "', current path '" + currentTexturePath + "'.",
                    gameObject
                );
                skytexturePath = currentTexturePath;
            }
#endif
        }


        /// <summary>
        /// Update the progress bar.
        /// </summary>
        void UpdateProgressBar()
        {
            uiManager.SetProgressBar(skyboxOrchestrator.GetProgress());
        }


        /// <summary>
        /// Initializes the UI state.
        /// </summary>
        void Start()
        {
            SetTexturePath();
            OnEditionModeChange();
            skyboxOrchestrator.ReportProgress = showProgress;
        }


        /// <summary>
        /// Updates the progress bar based on the completion status of the skybox task.
        /// </summary>
        void Update()
        {
            if (showProgress && skyboxTaskRunning)
            {
                UpdateProgressBar();
            }
        }
    }
}
