using System.IO;
using UnityEngine;

using AiWorldGeneration.TCP;

namespace AiWorldGeneration.ASR
{
    /// <summary>
    /// Record an audio sample from the microphone.
    /// </summary>
    public class AudioRecorder : MonoBehaviour
    {

        [Tooltip("Configuration file for speech recognition")]
        [SerializeField]
        private TextAsset configurationPath;

        [Tooltip("GameObject to disable when a new recording can start.")]
        [SerializeField]
        GameObject completionObject;

        /// <summary>
        /// Maximum audio length (s).
        /// </summary>
        public readonly int maxAudioLength = 10;

        /// <summary>
        /// Clip on which the audio is saved.
        /// </summary>
        AudioClip clip;

        /// <summary>
        /// Set to true to enable recording.
        /// </summary>
        bool isRecording;

        /// <summary>
        /// Starts the recording of the audio.
        /// </summary>
        public void StartRecording()
        {
            clip = Microphone.Start(null, false, maxAudioLength, 44100);
            isRecording = true;
        }

        /// <summary>
        /// Reads the audio from a saved WAV file.
        /// </summary>
        /// <returns>The content of the audio WAV file.</returns>
        byte[] ReadWav()
        {
            var data = JsonUtility.FromJson<JsonInterface>(configurationPath.text);
            return File.ReadAllBytes(data.audioPath);
        }


        /// <summary>
        /// Saves the audio as a WAV file.
        /// </summary>
        /// <param name="bytes">Bytes to save</param>
        /// <returns>Save file path</returns>
        string SaveWav(byte[] bytes)
        {
            var data = JsonUtility.FromJson<JsonInterface>(configurationPath.text);
            File.WriteAllBytes(data.audioPath, bytes);
            return data.audioPath;
        }

        /// <summary>
        /// Encode audios samples as a WAV file.
        /// </summary>
        /// <param name="samples">Audio samples</param>
        /// <param name="frequency">Frequency of the sample.</param>
        /// <param name="channels">Clip channels</param>
        /// <returns></returns>
        byte[] EncodeAsWAV(float[] samples, int frequency, int channels)
        {
            using var memoryStream = new MemoryStream(44 + samples.Length * 2);
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + samples.Length * 2);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2);
                writer.Write((ushort)(channels * 2));
                writer.Write((ushort)16);
                writer.Write("data".ToCharArray());
                writer.Write(samples.Length * 2);

                foreach (var sample in samples)
                {
                    writer.Write((short)(sample * short.MaxValue));
                }
            }
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Stops the recording of the audio, and returns the recorded bytes.
        /// </summary>
        public byte[] StopRecording()
        {
            if (isRecording)
            {
                isRecording = false;
                var position = Microphone.GetPosition(null);
                Microphone.End(null);
                if (position == 0)
                {
                    Debug.LogWarning("Audio recording is empty!");
                    return new byte[0];
                }
                var maxPosition = Mathf.Min(position, maxAudioLength * clip.frequency);
                var samples = new float[maxPosition * clip.channels];
                clip.GetData(samples, 0);
                return EncodeAsWAV(samples, clip.frequency, clip.channels);
            }
            else
            {
                Debug.LogWarning("Returning last recoded audio!");
                return ReadWav();
            }
        }

        /// <summary>
        /// Saves the audio as a WAV file.
        /// </summary>
        /// <param name="bytes">Bytes to save</param>
        /// <returns>Save file path</returns>
        public string SaveRecording()
        {
            var bytes = StopRecording();
            return SaveWav(bytes);
        }

        /// <summary>
        /// Notify that the recoding is completed.
        /// </summary>
        public void OnComplete()
        {
            if (completionObject != null)
                completionObject.SetActive(false);
        }

        /// <summary>
        /// If recording is active and the audio has been recorded for at least the duration of the clip, it saves the recording.
        /// </summary>
        void Update()
        {
            if (isRecording && Microphone.GetPosition(null) >= clip.samples - 1)
            {
                Debug.LogWarning("Recording is too long! Saving early...");
                SaveRecording();
            }
        }
    }
}
