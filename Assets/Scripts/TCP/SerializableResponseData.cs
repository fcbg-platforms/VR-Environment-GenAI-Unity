using System;


namespace AiWorldGeneration.TCP
{
    /// <summary>
    /// Base property for the server response data.
    /// </summary>
    [Serializable]
    public class SerializableResponseData
    {
        /// <summary>
        /// Server response type.
        /// </summary>
        public string type;
    }

    /// <summary>
    /// Completion rate for a task.
    /// </summary>
    [Serializable]
    public class CompletionResponse : SerializableResponseData
    {
        /// <summary>
        /// Task completion between 0 and 100.
        /// </summary>
        public int taskCompletion;

        /// <summary>
        /// Tracked task.
        /// </summary>
        public int taskId;
    }

    /// <summary>
    /// Response to a ping.
    /// </summary>
    [Serializable]
    public class PingResponse : SerializableResponseData
    {
        public int responseTimestamp;
        
        public int responseMilliseconds;

        public int queryTimestamp;
    }

    /// <summary>
    /// Rsponse data for skybox generation with the image encoded inside.
    /// </summary>
    [Serializable]
    public class ImageResponse : SerializableResponseData
    {
        /// <summary>
        /// PNG image encoded as an hexadecimal string.
        /// </summary>
        public string imageHexBytes;
    }


    /// <summary>
    /// Rsponse data for skybox generation on a single computer.
    /// </summary>
    [Serializable]
    public class LocalImageResponse : SerializableResponseData
    {
        public string skyboxFilePath;
    }


    /// <summary>
    /// Response data for Automatic Speech Recognition on a single computer.
    /// </summary>
    [Serializable]
    public class AsrResponse : SerializableResponseData
    {
        public string transcription;
    }

    /// <summary>
    /// Response data for inpainting on a single computer.
    /// </summary>
    [Serializable]
    public class LocalInpaintingResponse : SerializableResponseData
    {
        public string inpaintedFilePath;
    }
}
