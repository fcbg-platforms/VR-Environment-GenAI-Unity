using System;

namespace AiWorldGeneration.TCP
{
    /// <summary>
    /// C# interface with the JSON configuration file.
    ///
    /// This file is usually stored at Assets/Configurations/api.json
    /// </summary>
    [Serializable]
    public class JsonInterface
    {
        /// <summary>
        /// File path to store the mask file.
        /// </summary>
        public string maskPath;

        /// <summary>
        /// Path where audio files are stored.
        /// </summary>
        public string audioPath;

        public string serverDefaultIp;

        public int serverDefaultPort;

        /// <summary>
        /// Place where the Python project is stored. Should contain a venv.
        /// </summary>
        public string pythonFallbackApiFile;
    }

    /// <summary>
    /// Interface to fetch configuration data from the server file.
    /// </summary>
    [Serializable]
    public class ServerInterface
    {
        public string serverIp;

        public int serverPort;
    }

}
