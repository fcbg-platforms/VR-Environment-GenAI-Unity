using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;


namespace AiWorldGeneration.TCP
{
    public class NetworkCommunicator
    {

        /// <summary>
        /// Server IP address.
        /// </summary>
        private IPAddress ipAddress;

        /// <summary>
        /// Target server port.
        /// </summary>
        private int port;

        /// <summary>
        /// Number of chunks for transmission.
        /// </summary>
        readonly uint chunkSize = 2048;

        /// <summary>
        /// Maximum size of a response, in bytes.
        /// </summary>
        readonly uint responseMaxSize = 10000000;

        /// <summary>
        /// All ongoing tasks, indexed by task ID.
        /// </summary>
        Dictionary<int, ClientWorker> clientWorkersDict;

        /// <summary>
        /// Retrieves the completion status of a worker with the given task ID.
        /// </summary>
        /// <param name="taskId">The ID of the task to retrieve the completion status for.</param>
        /// <returns>The completion status of the task.</returns>
        public float GetCompletionStatus(int taskId)
        {
            if (!clientWorkersDict.ContainsKey(taskId))
            {
                // Happens on tasks already complete
                return 100f;
            }
            return clientWorkersDict[taskId].EstimateProgress();
        }



        /// <summary>
        /// Adds a new query to the queue and returns a LongTCS object containing a TaskCompletionSource.
        /// </summary>
        /// <param name="query">The SerializedQuery object containing the task details.</param>
        /// <returns>A LongTCS object containing a TaskCompletionSource for the new query.</returns>
        public System.Threading.Tasks.Task<SerializableResponseData> SpawnNewTask(BaseClientQuery query)
        {

            ClientWorker clientWorker = new();

            clientWorker.TcpClient.Connect(ipAddress, port);
            NetworkStream networkStream = clientWorker.TcpClient.GetStream();

            var dataToSend = JsonUtility.ToJson(query);
            byte[] data = Encoding.UTF8.GetBytes(dataToSend);

            networkStream.Write(data, 0, data.Length);

            networkStream.Flush();
            
            clientWorkersDict.Add(query.taskId, clientWorker);
            return clientWorker.TaskCompletionSource.Task;
        }

        /// <summary>
        /// Handles the response received from the server.
        /// </summary>
        /// <param name="clientWorker">The ClientWorker that received the response.</param>
        /// <param name="response">The response object containing the type of response and its data.</param>
        /// <returns>true if the response terminates the task.</returns>
        bool HandleResponse(ClientWorker clientWorker, SerializableResponse response)
        {
            if (response.type == "completion")
            {
                var completionResponse = JsonUtility.FromJson<CompletionResponse>(response.data);
                clientWorker.Progress = completionResponse.taskCompletion;
                return false;
            }
            if (response.type == "error")
            {
                Debug.LogError("Error " + response.status + ": " + response.message);
                return false;
            }

            switch (response.type)
            {
                case "new-skybox":
                case "panorama":
                case "refine-skybox":
                case "remove-seam":
                case "extend-skybox":
                case "inpainting":
                    clientWorker.TaskCompletionSource.SetResult(
                        JsonUtility.FromJson<ImageResponse>(response.data)
                    );
                    break;
                case "new-skybox-local":
                    clientWorker.TaskCompletionSource.SetResult(
                        JsonUtility.FromJson<LocalImageResponse>(response.data)
                    );
                    break;
                case "inpainting-local":
                    clientWorker.TaskCompletionSource.SetResult(
                        JsonUtility.FromJson<LocalInpaintingResponse>(response.data)
                    );
                    break;
                case "asr-local":
                case "asr":
                    clientWorker.TaskCompletionSource.SetResult(
                        JsonUtility.FromJson<AsrResponse>(response.data)
                    );
                    break;
                case "ping":
                    clientWorker.TaskCompletionSource.SetResult(
                        JsonUtility.FromJson<PingResponse>(response.data)
                    );
                    break;
                default:
                    throw new InvalidOperationException("Received unknown type: " + response.type);
            }

            return true;
        }

        /// <summary>
        /// Parses the received responses from the server and converts them into a format that can be used by the client.
        /// </summary>
        /// <param name="responses">The responses received from the server, in the form of a string containing multiple JSON responses separated by "}{"</param>
        /// <returns>An array of strings, each representing a single response received from the server.</returns>
        private string[] ParseResponses(string responses)
        {
            // Works because Unity JSON format subset does not have nested JSON structures
            var jsonResponses = responses.Split("}{");
            StringBuilder curatedResponse = new("");
            string[] responsesArray = new string[jsonResponses.Length];
            for (int i = 0; i < jsonResponses.Length; i++)
            {
                curatedResponse.Append(jsonResponses[i]);
                if (i > 0)
                {
                    curatedResponse.Insert(0, "{");
                }
                if (i < jsonResponses.Length - 1)
                {
                    curatedResponse.Append("}");
                }

                if (curatedResponse.Length < 2048)
                {
                    Debug.Log(curatedResponse);
                }
                else
                {
                    Debug.Log("Response of " + (curatedResponse.Length / 1000) + "k characters received.");
                }
                responsesArray[i] = curatedResponse.ToString();
            }
            return responsesArray;
        }

        /// <summary>
        /// Checks if the received data ends with a specific stop character sequence.
        /// 
        /// The character sequence is the end of a JSON response, so anything but "\", followed by "}".
        /// </summary>
        /// <param name="charsEnumerable">The sequence of bytes to check for the stop character sequence.</param>
        /// <returns>True if the data ends with the stop character sequence, otherwise false.</returns>
        bool EndsWithStop(IEnumerable<byte> charsEnumerable)
        {
            byte[] elems = charsEnumerable.Reverse().Take(2).Reverse().ToArray();
            var chars = Encoding.UTF8.GetChars(elems);
            return chars[0] != '\\' && chars[1] == '}';
        }

        /// <summary>
        /// Reads data from the network stream and processes it.
        /// </summary>
        /// <param name="networkStream">The network stream to read data from.</param>
        /// <returns>An array of <see cref="SerializableResponse"/> objects, each representing a single response received from the server.</returns>
        private SerializableResponse[] ReadDataStream(NetworkStream networkStream)
        {
            byte[] responseData = new byte[chunkSize];
            List<byte> responseBuffer = new();
            int bytesRead;

            // Collect the data and check for overflow
            do
            {
                if (responseBuffer.Count + chunkSize > responseMaxSize)
                {
                    throw new System.Data.DataException(
                        "Data is too large! Received " + responseBuffer.Count / 1000 +
                        "k bytes, limit " + responseMaxSize / 1000 + "k bytes, trying to allocate " +
                        chunkSize + " bytes"
                    );
                }
                bytesRead = networkStream.Read(responseData, 0, responseData.Length);
                responseBuffer.AddRange(responseData.Take(bytesRead));
            } while (!EndsWithStop(responseData.Take(bytesRead)));

            // Convert the received bytes to a string
            string responseString = Encoding.UTF8.GetString(responseBuffer.ToArray());

            // Serialize the response as objects
            var jsonResponses = ParseResponses(responseString);

            SerializableResponse[] serializedResponses = new SerializableResponse[jsonResponses.Length];
            for (int i = 0; i < jsonResponses.Length; i++)
            {
                serializedResponses[i] = JsonUtility.FromJson<SerializableResponse>(jsonResponses[i]);
            }
            return serializedResponses;
        }

        /// <summary>
        /// Updates the client: send and receive data.
        ///
        /// Data to send and to receive are checked and action is taken accordingly.
        /// </summary>
        /// <returns></returns>
        public void UpdateClient()
        {
            List<int> finishedTasks = new();

            foreach (KeyValuePair<int, ClientWorker> workerPair in clientWorkersDict)
            {
                var tcpClient = workerPair.Value.TcpClient;

                // Get the network stream to send and receive data
                NetworkStream networkStream = tcpClient.GetStream();

                // Receive data from the server
                if (networkStream.DataAvailable)
                {
                    var endOfStream = false;
                    foreach (var response in ReadDataStream(networkStream))
                    {
                        // Process the response and return whether the end of stream is reached
                        endOfStream |= HandleResponse(workerPair.Value, response);
                    }

                    // Check if more data are to be expected
                    if (endOfStream)
                    {
                        // Dispose of the stream and the client
                        networkStream.Dispose();

                        tcpClient.Dispose();

                        finishedTasks.Add(workerPair.Key);
                    }
                }

            }

            // Remove finished tasks from the dictionary
            foreach (var taskId in finishedTasks)
            {
                clientWorkersDict.Remove(taskId);
            }

        }

        /// <summary>
        /// Initializes the NetworkCommunicator with the provided IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address of the server to connect to.</param>
        /// <param name="port">The target server port.</param>
        private void Initialize(IPAddress ipAddress, int port)
        {
            this.ipAddress = ipAddress;
            this.port = port;

            clientWorkersDict = new();
        }

        /// <summary>
        /// Initializes the NetworkCommunicator with the provided IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address of the server to connect to.</param>
        /// <param name="port">The target server port.</param>
        public NetworkCommunicator(IPAddress ipAddress, int port)
        {
            Initialize(ipAddress, port);
        }

        /// <summary>
        /// Initializes the NetworkCommunicator with the provided IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address of the server to connect to.</param>
        /// <param name="port">The target server port.</param>
        public NetworkCommunicator(string ipAddress, int port)
        {
            Initialize(IPAddress.Parse(ipAddress), port);
        }
    }
}
