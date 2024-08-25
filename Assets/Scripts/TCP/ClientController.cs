using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;


namespace AiWorldGeneration.TCP
{
    /// <summary>
    /// Controls a TCP client connection.
    /// </summary>
    public class ClientController : MonoBehaviour
    {
        [Tooltip("File path to the TCP client configuration.")]
        [SerializeField]
        TextAsset configurationFile;

        /// <summary>
        /// An instance of the <see cref="NetworkCommunicator"/> class to commmunicate with the server.
        /// </summary>
        NetworkCommunicator networkCommunicator;

        /// <summary>
        /// Converts a hexadecimal string into a byte array.
        /// </summary>
        /// <param name="hex">The hexadecimal string to be converted.</param>
        /// <returns>A byte array representation of the hexadecimal string.</returns>
        public byte[] ByteArrayFromHexString(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }


        /// <summary>
        /// Retrieves the completion status of a task with the given task ID.
        /// </summary>
        /// <param name="taskId">The ID of the task to retrieve the completion status for.</param>
        /// <returns>The completion status of the task.</returns>
        public float GetCompletionStatus(int taskId)
        {
            return networkCommunicator.GetCompletionStatus(taskId);
        }


        /// <summary>
        /// Sends a ping request to the server and returns the response time in milliseconds.
        /// </summary>
        /// <returns>The response time in milliseconds.</returns>
        public async Task<int> SendPing()
        {
            PingQuery wrapper = new();
            var initialTime = DateTime.UtcNow.Millisecond;
            var pingData = (PingResponse)await networkCommunicator.SpawnNewTask(wrapper);
            Debug.Log("Pong: " + (DateTime.UtcNow.Millisecond - initialTime));
            return pingData.responseMilliseconds;
        }

        /// <summary>
        /// Sends an audio file path to the server for text conversion and returns the transcribed text.
        /// </summary>
        /// <param name="audioPath">The path to the audio file path to be transcribed.</param>
        /// <returns>The transcribed text from the audio file.</returns>
        public async Task<string> TextFromAudio(string audioPath)
        {
            LocalAsrQuery wrapper = new(Application.dataPath + "/../" + audioPath);
            var audioData = (AsrResponse)await networkCommunicator.SpawnNewTask(wrapper);

            return audioData.transcription;
        }


        /// <summary>
        /// Sends an audio file as bytes to the server for text conversion and returns the transcribed text.
        /// </summary>
        /// <param name="audioBytes">The bytes of the audio file path to be transcribed.</param>
        /// <returns>The transcribed text from the audio file.</returns>
        public async Task<string> TextFromAudio(byte[] audioBytes)
        {
            var encodedBytes = BitConverter.ToString(audioBytes);

            AsrQuery wrapper = new(encodedBytes);

            var audioData = (AsrResponse)await networkCommunicator.SpawnNewTask(wrapper);

            return audioData.transcription;
        }

        /// <summary>
        /// Take a text prompt and asks the server to generate a new skybox.
        /// </summary>
        /// <param name="prompt">The prompt skybox prompt.</param>
        /// <param name="reportCompletion">A value indicating whether to report the completion status of the task.</param>
        /// <param name="taskId">An optional unique identifier for the task. Default is -1.</param>
        /// <returns>The path to the generated skybox (image), or the error message.</returns>
        public async Task<string> CreateNewSkyboxLocal(string prompt, bool reportCompletion = false, int taskId = -1)
        {
            string destinationPath = Application.dataPath + "/../Temp/skybox.png";
            LocalNewSkyboxQuery wrapper = new(prompt, destinationPath, reportCompletion ? 1 : 0)
            {
                taskId = taskId
            };
            if (reportCompletion)
            {
                wrapper.reportCompletion = 1;
            }

            var skyboxData = (LocalImageResponse)await networkCommunicator.SpawnNewTask(wrapper);

            return skyboxData.skyboxFilePath;
        }


        /// <summary>
        /// Take a text prompt and asks the server to generate a new skybox.
        /// </summary>
        /// <param name="prompt">The prompt skybox prompt.</param>
        /// <param name="reportCompletion">A value indicating whether to report the completion status of the task.</param>
        /// <param name="taskId">An optional unique identifier for the task. Default is -1.</param>
        /// <param name="quick">Stop the generation at the first pipeline element.</param>
        /// <returns>The path to the generated skybox (image), or the error message.</returns>
        public async Task<string> CreateNewSkybox(string prompt, bool reportCompletion = false, int taskId = -1, bool quick = true)
        {
            NewSkyboxQuery wrapper = new(prompt, reportCompletion, quick)
            {
                taskId = taskId
            };
            if (reportCompletion)
            {
                wrapper.reportCompletion = 1;
            }

            var skyboxData = (ImageResponse)await networkCommunicator.SpawnNewTask(wrapper);
            Debug.Log("Data received.");
            byte[] imageAsBytes = ByteArrayFromHexString(skyboxData.imageHexBytes);

            // Save as a file
            string destinationPath = Application.dataPath + "/../Temp/skybox.png";
            await File.WriteAllBytesAsync(destinationPath, imageAsBytes);
            return destinationPath;
        }

        /// <summary>
        /// Inpaints an image using its mask and a prompt.
        ///
        /// Sends image and mask paths along with a prompt to the server for inpainting and returns the inpainted image path.
        /// </summary>
        /// <param name="imageBytes">The image to be inpainted, bytes in PNG format.</param>
        /// <param name="maskBytes">The bytes composing the mask.</param>
        /// <param name="query">The prompt for the inpainting process.</param>
        /// <param name="taskId">Id to track this task, any unique number.</param>
        /// <param name="reportCompletion">A value indicating whether to report the completion status of the task.</param>
        /// <returns>The path to the inpainted image.</returns>
        public async Task<string> InpaintLocal(
            byte[] imageBytes,
            byte[] maskBytes,
            string query,
            int taskId = -1,
            bool reportCompletion = false
        )
        {
            // Save images to local files
            var baseExchangePath = Application.dataPath + "/../Temp/";

            var imagePath = Path.Join(baseExchangePath, "inpainting_candidate.png");
            await File.WriteAllBytesAsync(imagePath, imageBytes);

            var maskPath = Path.Join(baseExchangePath, "inpainting_mask.png");
            await File.WriteAllBytesAsync(maskPath, maskBytes);

            // Prepare the query
            LocalInpaintingQuery wrapper = new(
                imagePath,
                maskPath,
                Path.Join(baseExchangePath, "inpainting_result.png"),
                query,
                reportCompletion ? 1 : 0
            )
            {
                taskId = taskId
            };

            // Post the inpainting query and wait for answer
            var inpaintingData = (LocalInpaintingResponse)await networkCommunicator.SpawnNewTask(wrapper);

            return inpaintingData.inpaintedFilePath;
        }


        /// <summary>
        /// Inpaints an image using its mask and a prompt, without saving files locally.
        ///
        /// Sends image and mask bytes along with a prompt to the server for inpainting,
        /// saves the inpainted image bytes and returns its path.
        /// </summary>
        /// <param name="imageBytes">The image to be inpainted, bytes in PNG format.</param>
        /// <param name="maskBytes">The bytes composing the mask.</param>
        /// <param name="query">The prompt for the inpainting process.</param>
        /// <param name="taskId">Id to track this task, any unique number.</param>
        /// <param name="reportCompletion">A value indicating whether to report the completion status of the task.</param>
        /// <returns>The path to the inpainted image.</returns>
        public async Task<string> Inpaint(
            byte[] imageBytes,
            byte[] maskBytes,
            string query,
            int taskId = -1,
            bool reportCompletion = false
        )
        {
            InpaintingQuery wrapper = new(
                BitConverter.ToString(imageBytes),
                BitConverter.ToString(maskBytes),
                query,
                reportCompletion ? 1 : 0
            )
            {
                taskId = taskId
            };

            var inpaintingData = (ImageResponse)await networkCommunicator.SpawnNewTask(wrapper);

            // Note: .NET 8 introduces Convert.FromHexString that should be faster
            byte[] imageAsBytes = ByteArrayFromHexString(inpaintingData.imageHexBytes);

            string destinationPath = Application.dataPath + "/../Temp/inpainted_skybox.png";
            await File.WriteAllBytesAsync(destinationPath, imageAsBytes);
            return destinationPath;
        }

        /// <summary>
        /// Refine a skybox to increase its quality.
        /// </summary>
        /// <param name="imageBytes">The image to be inpainted, bytes in PNG format.</param>
        /// <param name="query">The prompt for the inpainting process.</param>
        /// <param name="taskId">Id to track this task, any unique number.</param>
        /// <param name="reportCompletion">A value indicating whether to report the completion status of the task.</param>
        /// <returns>The path to the inpainted image.</returns>
        public async Task<string> RefineSkybox(
            byte[] imageBytes,
            string query,
            int taskId = -1,
            bool reportCompletion = false
        )
        {
            RefineSkyboxQuery wrapper = new(
                BitConverter.ToString(imageBytes),
                query,
                reportCompletion ? 1 : 0
            )
            {
                taskId = taskId
            };

            var refiningData = (ImageResponse)await networkCommunicator.SpawnNewTask(wrapper);

            // Note: .NET 8 introduces Convert.FromHexString that should be faster
            byte[] imageAsBytes = ByteArrayFromHexString(refiningData.imageHexBytes);

            string destinationPath = Application.dataPath + "/../Temp/refined_skybox.png";
            await File.WriteAllBytesAsync(destinationPath, imageAsBytes);
            return destinationPath;
        }


        /// <summary>
        /// Remove the seam line at the borders of a skybox making it an asymetric tiling.
        /// </summary>
        /// <param name="imageBytes">The image to be inpainted, bytes in PNG format.</param>
        /// <param name="taskId">Id to track this task, any unique number.</param>
        /// <param name="reportCompletion">A value indicating whether to report the completion status of the task.</param>
        /// <returns>The path to the inpainted image.</returns>
        public async Task<string> RemoveSkyboxSeam(
            byte[] imageBytes,
            int taskId = -1,
            bool reportCompletion = false
        )
        {
            RemoveSeamQuery wrapper = new(
                BitConverter.ToString(imageBytes),
                reportCompletion ? 1 : 0
            )
            {
                taskId = taskId
            };

            var refiningData = (ImageResponse)await networkCommunicator.SpawnNewTask(wrapper);

            // Note: .NET 8 introduces Convert.FromHexString that should be faster
            byte[] imageAsBytes = ByteArrayFromHexString(refiningData.imageHexBytes);

            string destinationPath = Application.dataPath + "/../Temp/noseam_skybox.png";
            await File.WriteAllBytesAsync(destinationPath, imageAsBytes);
            return destinationPath;
        }


        /// <summary>
        /// Extend the input skybox to triple its heights.
        /// </summary>
        /// <param name="imageBytes">The image to be extend, bytes in PNG format.</param>
        /// <param name="taskId">Id to track this task, any unique number.</param>
        /// <param name="reportCompletion">A value indicating whether to report the completion status of the task.</param>
        /// <returns>The path to the extended image.</returns>
        public async Task<string> ExtendSkybox(
            byte[] imageBytes,
            int taskId = -1,
            bool reportCompletion = false
        )
        {
            ExtendSkyboxQuery wrapper = new(
                BitConverter.ToString(imageBytes),
                reportCompletion ? 1 : 0
            )
            {
                taskId = taskId
            };

            var extendedData = (ImageResponse)await networkCommunicator.SpawnNewTask(wrapper);

            // Note: .NET 8 introduces Convert.FromHexString that should be faster
            byte[] imageAsBytes = ByteArrayFromHexString(extendedData.imageHexBytes);

            string destinationPath = Application.dataPath + "/../Temp/extended_skybox.png";
            await File.WriteAllBytesAsync(destinationPath, imageAsBytes);
            return destinationPath;
        }


        /// <summary>
        /// Initializes the client controller and sets up the connection details.
        /// </summary>
        NetworkCommunicator AutosetConnection()
        {
            string iPAddress;
            int port;
            var jsonInterface = JsonUtility.FromJson<JsonInterface>(configurationFile.text);
            // Get the server IP address and port
            if (File.Exists(jsonInterface.pythonFallbackApiFile))
            {
                Debug.LogWarning("Getting server configuration from a local file.");

                var jsonString = File.ReadAllText(jsonInterface.pythonFallbackApiFile);
                var pythonInterface = JsonUtility.FromJson<ServerInterface>(jsonString);

                iPAddress = pythonInterface.serverIp;
                port = pythonInterface.serverPort;
            }
            else
            {
                iPAddress = jsonInterface.serverDefaultIp;
                port = jsonInterface.serverDefaultPort;
            }
            return new(iPAddress, port);
        }

        /// <summary>
        /// Initializes the TCP client.
        /// </summary>
        void Start()
        {
            networkCommunicator = AutosetConnection();
        }

        /// <summary>
        /// Runs the client thread for network communication, check for data every second.
        /// </summary>
        void Update()
        {
            networkCommunicator.UpdateClient();
        }
    }
}
