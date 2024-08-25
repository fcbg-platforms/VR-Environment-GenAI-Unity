using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;


namespace AiWorldGeneration.TCP
{
    /// <summary>
    /// A client tasking waiting for a server answer.
    /// </summary>
    public class ClientWorker
    {
        /// <summary>
        /// The TaskCompletionSource for the task to do.
        /// </summary>
        public TaskCompletionSource<SerializableResponseData> TaskCompletionSource { get; }

        /// <summary>
        /// TCP client associated with the task.
        /// </summary>
        public TcpClient TcpClient { get; }

        /// <summary>
        /// Task progress from 0 to 100.
        /// </summary>
        private byte progress;

        /// <summary>
        /// Last time the object was updated.
        /// </summary>
        private float updateTime;

        /// <summary>
        /// Before the last time the object was updated, useful for future update prediction.
        /// </summary>
        private float antepenultiamUpdateTime;

        /// <summary>
        /// Worker progress, between 0 and 100.
        /// </summary>
        public int Progress
        {
            get { return progress; }

            /// <summary>
            /// Sets the worker's progress, clamp between 0 and 100.
            /// </summary>
            set {
                int newProgress = value;

                if (newProgress < 0)
                    newProgress = 0;
                if (newProgress > 100)
                    newProgress = 100;
                progress = (byte)newProgress;
                antepenultiamUpdateTime = updateTime;
                updateTime = Time.time;
             }
        }

        /// <summary>
        /// Get a rough estimate of the workers progress at the current time.
        ///
        /// It inferes the expected progress, not the real one, and returns a value greater of equal the one of Progress.
        /// </summary>
        public float EstimateProgress()
        {
            // Last difference between two progress update
            float deltaTimestamp = updateTime - antepenultiamUpdateTime;
            // Use it to determine how close we are from the next update (between 0 and 1)
            float timeRatio = Mathf.Clamp01((Time.time - updateTime) / deltaTimestamp);
            return progress + deltaTimestamp * timeRatio;
        }

        public ClientWorker()
        {
            TcpClient = new();
            TaskCompletionSource = new();
        }
    }
}
