using System;


namespace AiWorldGeneration.TCP
{
    /// <summary>
    /// A client query to send data to the server serializable as JSON.
    /// </summary>
    [Serializable]
    public abstract class BaseClientQuery
    {
        /// <summary>
        /// Type of query, identified by a unique name.
        /// </summary>
        public string type;

        /// <summary>
        /// Unique task ID to track it.
        /// </summary>
        public int taskId;
    }

    /// <summary>
    /// A ping query to measure the status of the server and the response time.
    /// </summary>
    [Serializable]
    public class PingQuery : BaseClientQuery
    {
        /// <summary>
        /// The timestamp at which the query was generated, in milliseconds.
        /// </summary>
        public int queryTimestamp;

        public PingQuery()
        {
            type = "ping";
            queryTimestamp = (int)(DateTime.UtcNow.Ticks / 10000);
        }
    }

    /// <summary>
    /// Ask the server to report the progress of a task.
    /// </summary>
    [Serializable]
    public class CompletionQuery : BaseClientQuery
    {
        public string taskID;

        public CompletionQuery()
        {
            type = "completion";
        }
    }

    /// <summary>
    /// A query for Automatic Speech Recognition using a local file.
    /// </summary>
    [Serializable]
    public class LocalAsrQuery : BaseClientQuery
    {
        public string audioPath;

        public LocalAsrQuery(string audioFilePath = null)
        {
            type = "asr-local";
            if (audioFilePath != null)
                audioPath = audioFilePath;
        }
    }


    /// <summary>
    /// A query for Automatic Speech Recognition.
    /// </summary>
    [Serializable]
    public class AsrQuery : BaseClientQuery
    {
        public string audioBytes;

        public AsrQuery(string audioBytes = null)
        {
            type = "asr";
            if (audioBytes != null)
                this.audioBytes = audioBytes;
        }
    }

    /// <summary>
    /// Query to generate a new skybox on the same device.
    /// </summary>
    [Serializable]
    public class LocalNewSkyboxQuery : BaseClientQuery
    {
        public string prompt;

        public string outputFilePath;

        public int reportCompletion;

        public LocalNewSkyboxQuery(string prompt = null, string destinationPath = null, int reportCompletion = 0)
        {
            type = "new-skybox-local";
            if (prompt != null) this.prompt = prompt;
            if (destinationPath != null) outputFilePath = destinationPath;
            this.reportCompletion = reportCompletion;
        }
    }

    /// <summary>
    /// Query to generate a new skybox as a sequence of bytes.
    /// </summary>
    [Serializable]
    public class NewSkyboxQuery : BaseClientQuery
    {
        /// <summary>
        /// Image generation prompt.
        /// </summary>
        public string prompt;

        /// <summary>
        /// 1 if completion reports should be sent regularly, 0 otherwise.
        /// </summary>
        public int reportCompletion;

        /// <summary>
        /// Advertise that the generation should stop at the first step of the pipeline.
        /// </summary>
        public int quick;

        /// <summary>
        /// Generate a new skybox query.
        /// </summary>
        /// <param name="prompt">Prompt to use.</param>
        /// <param name="reportCompletion">True to send completion reports regularly.</param>
        /// <param name="quickGeneration">true to stop the generation at the first step of the pipeline.</param>
        public NewSkyboxQuery(string prompt = null, bool reportCompletion = false, bool quickGeneration = false)
        {
            type = "new-skybox";
            if (prompt != null) this.prompt = prompt;
            this.reportCompletion = reportCompletion ? 1 : 0;
            quick = quickGeneration ? 1 : 0;
        }
    }

    /// <summary>
    /// Query to do an inpainting task on a single computer.
    /// </summary>
    [Serializable]
    public class LocalInpaintingQuery : BaseClientQuery
    {
        /// <summary>
        /// Base image path on which to conduct the inpainting.
        /// </summary>
        public string imagePath;

        /// <summary>
        /// Path to the mask. White areas will be inpainted.
        /// </summary>
        public string maskPath;

        /// <summary>
        /// Prompt guidance for the inpainting.
        /// </summary>
        public string prompt;

        /// <summary>
        /// Path where the generated file should be saved.
        /// </summary>
        public string outputFilePath;

        /// <summary>
        /// Report completion at regular intervals.
        /// </summary>
        public int reportCompletion;

        public LocalInpaintingQuery(
            string inputImagePath = null,
            string maskPath = null,
            string destinationPath = null,
            string prompt = null,
            int reportCompletion = 0
        )
        {
            type = "inpainting-local";
            if (inputImagePath != null) imagePath = inputImagePath;
            if (maskPath != null) this.maskPath = maskPath;
            if (destinationPath != null) outputFilePath = destinationPath;
            if (prompt != null) this.prompt = prompt;
            if (reportCompletion != 0) this.reportCompletion = reportCompletion;
        }
    }

    /// <summary>
    /// Abstract query to do an image edition task.
    /// </summary>
    [Serializable]
    public abstract class ImageEditionQuery : BaseClientQuery
    {
        /// <summary>
        /// Base image path on which to conduct the edition.
        /// </summary>
        public string imageBytes;

        /// <summary>
        /// Prompt guidance for the image edition.
        /// </summary>
        public string prompt;

        /// <summary>
        /// Report completion at regular intervals.
        /// </summary>
        public int reportCompletion;

        protected ImageEditionQuery(
            string inputImage = null,
            string prompt = null,
            int reportCompletion = 0
        )
        {
            type = "image-editing";
            if (inputImage != null) imageBytes = inputImage;
            if (prompt != null) this.prompt = prompt;
            if (reportCompletion != 0) this.reportCompletion = reportCompletion;
        }
    }

    /// <summary>
    /// Query to do an inpainting task.
    /// </summary>
    [Serializable]
    public class InpaintingQuery : ImageEditionQuery
    {

        /// <summary>
        /// Path to the mask. White areas will be inpainted.
        /// </summary>
        public string maskBytes;

        public InpaintingQuery(
            string inputImage = null,
            string maskBytes = null,
            string prompt = null,
            int reportCompletion = 0
        )
        {
            type = "inpainting";
            if (inputImage != null) imageBytes = inputImage;
            if (maskBytes != null) this.maskBytes = maskBytes;
            if (prompt != null) this.prompt = prompt;
            if (reportCompletion != 0) this.reportCompletion = reportCompletion;
        }
    }


    /// <summary>
    /// Query to do an skybox refining task.
    /// </summary>
    [Serializable]
    public class RefineSkyboxQuery : ImageEditionQuery
    {
        public RefineSkyboxQuery(
            string inputImage = null,
            string prompt = null,
            int reportCompletion = 0
        )
        {
            type = "refine-skybox";
            if (inputImage != null) imageBytes = inputImage;
            if (prompt != null) this.prompt = prompt;
            if (reportCompletion != 0) this.reportCompletion = reportCompletion;
        }
    }


    /// <summary>
    /// Query to do remove the seam line from a skybox.
    /// </summary>
    [Serializable]
    public class RemoveSeamQuery : ImageEditionQuery
    {
        public RemoveSeamQuery(
            string inputImage = null,
            int reportCompletion = 0
        )
        {
            type = "remove-seam";
            if (inputImage != null) imageBytes = inputImage;
            if (reportCompletion != 0) this.reportCompletion = reportCompletion;
        }
    }

    /// <summary>
    /// Query to extend a skybox.
    /// </summary>
    [Serializable]
    public class ExtendSkyboxQuery : ImageEditionQuery
    {
        public ExtendSkyboxQuery(
            string inputImage = null,
            int reportCompletion = 0
        )
        {
            type = "extend-skybox";
            if (inputImage != null) imageBytes = inputImage;
            if (reportCompletion != 0) this.reportCompletion = reportCompletion;
        }
    }
}
