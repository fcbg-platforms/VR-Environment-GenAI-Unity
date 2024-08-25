using System;


namespace AiWorldGeneration.TCP
{
    /// <summary>
    /// Basic format that the server should follow.
    /// </summary>
    [Serializable]
    public class SerializableResponse
    {
        /// <summary>
        /// The message field contains a string that represents the message to be sent to the client.
        /// </summary>
        public string message;

        /// <summary>
        /// The status field contains an integer that represents the status of the response.
        ///
        /// It is a subset of the HTTP status codes.
        /// </summary>
        public int status;

        /// <summary>
        /// The data field contains a string that represents the data to be sent to the client.
        ///
        /// This string should be parsable as JSON.
        /// </summary>
        public string data;

        /// <summary>
        /// The taskId field contains an integer that represents the task ID associated with the response.
        /// </summary>
        public int taskId;

        /// <summary>
        /// The type field contains a string that represents the type of the response.
        /// </summary>
        public string type;
    }
}
