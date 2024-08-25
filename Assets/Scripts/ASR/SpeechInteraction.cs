using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using AiWorldGeneration.TCP;


namespace AiWorldGeneration.ASR
{
    /// <summary>
    /// A demo class for automatic speech recognition with interactions in the scene.
    /// </summary>
    [RequireComponent(typeof(AudioRecorder))]
    public class SpeechInteraction : MonoBehaviour
    {
        [Tooltip("Button to start recording.")]
        [SerializeField]
        Button startButton;

        [Tooltip("Button to stop recording.")]
        [SerializeField]
        Button stopButton;

        [Tooltip("Interface text.")]
        [SerializeField]
        TextMeshProUGUI text;

        [Tooltip("Speech recognition component.")]
        [SerializeField]
        AudioRecorder audioRecorder;

        /// <summary>
        /// Client controller for the communication with the server.
        /// </summary>
        ClientController clientController;

        void Start()
        {
            audioRecorder = GetComponent<AudioRecorder>();
            clientController = GetComponent<ClientController>();
        }

        /// <summary>
        /// Starts the recording of the audio.
        /// </summary>
        public void StartRecording()
        {
            text.color = Color.white;
            text.text = "Recording...";
            startButton.interactable = false;
            stopButton.interactable = true;
            audioRecorder.StartRecording();
        }


        /// <summary>
        /// Stops the recording of the audio, and sends the audio for transcription.
        /// </summary>
        public void StopRecording()
        {
            var audioBytes = audioRecorder.StopRecording();
            SendRecording(audioBytes);
        }

        /// <summary>
        /// Show a text as the audio transcription.
        /// </summary>
        /// <param name="transcription">Transcription of the audio.</param>
        void ShowTranscription(string transcription)
        {
            Debug.Log(transcription);
            text.color = Color.white;
            text.text = transcription;
            startButton.interactable = true;
        }


        /// <summary>
        /// Send the audio to the server for transcription.
        /// </summary>
        /// <param name="audioBytes">Audio data.</param>
        void SendRecording(byte[] audioBytes)
        {
            text.color = Color.yellow;
            text.text = "Sending...";
            stopButton.interactable = false;

            var tcs = new TaskCompletionSource<string>();
            Task.Run(async () =>
            {
                tcs.SetResult(await clientController.TextFromAudio(audioBytes));
            });

            // ConfigureAwait must be true to get unity main thread context
            tcs.Task.ConfigureAwait(true).GetAwaiter().OnCompleted(() =>
            {
                ShowTranscription(tcs.Task.Result);
            });
        }
    }
}
