using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

using AiWorldGeneration.TCP;


namespace AiWorldGeneration.Skybox
{

    /// <summary>
    /// Defines the interaction between a skybox material and the scene skybox.
    /// </summary>
    [RequireComponent(typeof(ClientController))]
    public class SkyboxOrchestrator : MonoBehaviour
    {
        /// <summary>
        /// Show the progress on the progress bar.
        /// </summary>
        public bool ReportProgress { get; set; }

        [Tooltip("Script responsible for the masking effect.")]
        [SerializeField]
        SkyboxMasker skyboxMasker;

        [Tooltip("What action should trigger the painting.")]
        [SerializeField]
        InputActionReference paintActionReference;

        [Tooltip("Texture that should be rewrited.")]
        [SerializeField]
        Texture2D rewritableTexture;

        [Tooltip("Material to apply as the skybox.")]
        [SerializeField]
        Material rewritableMaterial;

        /// <summary>
        /// Invoked when a generation task gets completed.
        /// </summary>
        [SerializeField]
        UnityEvent onCompleteTask = new();

        /// <summary>
        /// Class responsible for communication with the TCP server.
        /// </summary>
        ClientController clientController;

        /// <summary>
        /// A small tracker for the latest skybox update task.
        /// </summary>
        int skyboxTaskId;


        /// <summary>
        /// Change the current skybox
        /// </summary>
        /// <param name="newSkybox">The cubemap texture to apply as the skybox.</param>
        public void ChangeSkybox(Cubemap newSkybox)
        {
            Material skyboxMaterial = new(Shader.Find("Skybox/Cubemap"))
            {
                name = "Temporary Skybox Material"
            };
            skyboxMaterial.SetFloat("_Rotation", 270);
            skyboxMaterial.SetTexture("_Tex", newSkybox);
            RenderSettings.skybox = skyboxMaterial;
        }


        /// <summary>
        /// Return the speculative progress on the current task between 0 and 1.
        /// </summary>
        public float GetProgress()
        {
            var completion = clientController.GetCompletionStatus(skyboxTaskId);
            return completion / 100f;
        }

        /// <summary>
        /// Applies the specified image file as a skybox.
        /// </summary>
        /// <param name="filePath">The path of the image file to be applied as a skybox.</param>
        /// <returns>The cubemap texture created from the specified image.</returns>
        Cubemap ApplyFileAsSkybox(string filePath)
        {
            SkyboxImporter importer = new();
            var newImagePath = importer.ImportImage(filePath);
            var cubemap = importer.ImportToCubemap(newImagePath);
            ChangeSkybox(cubemap);
            return cubemap;
        }

        /// <summary>
        /// Edits a texture with the provided file, pasting the image on the texture.
        /// </summary>
        /// <param name="filePath">Image file to paste.</param>
        /// <param name="outputTexture">Texture to paste unto.</param>
        void ApplyFileOnTexture(string filePath, Texture2D outputTexture)
        {
            byte[] imageData = System.IO.File.ReadAllBytes(filePath);
            outputTexture.LoadImage(imageData);
            outputTexture.Apply();
        }

        /// <summary>
        /// Loads a new skybox from the specified file path.
        /// </summary>
        /// <param name="filepath">The path of the image file to be applied as a skybox.</param>
        void LoadSkybox(string filepath)
        {
            ApplyFileOnTexture(filepath, rewritableTexture);
            RenderSettings.skybox = rewritableMaterial;
        }


        /// <summary>
        /// Loads a new skybox from the specified file path, apply fade-out and fade-in around.
        /// We start by a fade-out to black, then we change the skybox and apply a fade-in to the exposur of 1.
        /// The fading effects works by modifying the skybox's material exposure.
        /// </summary>
        /// <param name="newImagePath">The path of the image file to be applied as a skybox.</param>
        /// <param name="transitionDuration">The time for the skybox change.</param>
        IEnumerator SmoothLoadSkybox(string newImagePath, float transitionDuration)
        {
            var exposure = 1f;
            var shaderProp = "_Exposure";
            while (exposure > 0)
            {
                rewritableMaterial.SetFloat(shaderProp, exposure);
                exposure -= Time.deltaTime / transitionDuration;
                yield return null;
            }
            rewritableMaterial.SetFloat(shaderProp, 0);
            yield return null;

            LoadSkybox(newImagePath);

            while (exposure < 1)
            {
                rewritableMaterial.SetFloat(shaderProp, exposure);
                exposure += Time.deltaTime / transitionDuration;
                yield return null;
            }
            rewritableMaterial.SetFloat(shaderProp, 1);
        }


        /// <summary>
        /// Sets an image as the skybox.
        /// It will edit the main texture asset.
        /// </summary>
        /// <param name="newImagePath">Path to the image file.</param>
        /// <param name="transitionDuration">Time for the sskybox change.</param>
        void SetImageAsSkybox(string newImagePath, float transitionDuration = 2f)
        {
            skyboxTaskId = 0;
            skyboxMasker.ResetTexture();
            onCompleteTask.Invoke();

            if (transitionDuration > 0f)
            {
                StartCoroutine(SmoothLoadSkybox(newImagePath, transitionDuration));
            }
            else
            {
                LoadSkybox(newImagePath);
            }
        }

        /// <summary>
        /// Generates a new unique ID for the current skybox task.
        /// If a task is still running, it logs a warning message.
        /// </summary>
        void NewSkyboxTaskId()
        {
            if (skyboxTaskId != 0)
            {
                Debug.LogWarning("A skybox creation task with the ID " + skyboxTaskId + " was not finished!");
            }
            skyboxTaskId = Random.Range(1, 10000);
        }

        /// <summary>
        /// Runs a skybox generation task and replaces the main skybox texture.
        /// 
        /// The main idea behind this method is to take a task running in any thread and bring it back to
        /// the main Unity thread.
        /// </summary>
        /// <param name="starterTask">Task to run.</param>
        void RunSkyboxTask(Task<string> starterTask)
        {
            var taskCSource = new TaskCompletionSource<string>();

            // ConfigureAwait must be true to get unity main thread context
            taskCSource.Task.ConfigureAwait(true).GetAwaiter().OnCompleted(() =>
            {
                // Apply the new texture
                SetImageAsSkybox(taskCSource.Task.Result);
            });

            // Start running the task
            Task.Run(async () =>
            {
                var taskResult = await starterTask;
                taskCSource.SetResult(taskResult);
            });
        }

        /// <summary>
        /// Generates a new skybox based on the given prompt.
        /// </summary>
        /// <param name="prompt">The textual description of the desired skybox.</param>
        /// <param name="quick">Stop the generation at the first pipeline element.</param>
        public void GenerateNewSkybox(string prompt, bool quick = true)
        {
            NewSkyboxTaskId();
            Task<string> generationTask = clientController.CreateNewSkybox(prompt, ReportProgress, skyboxTaskId, quick);
            RunSkyboxTask(generationTask);
        }

        /// <summary>
        /// Inpaints the input image with the given mask and query.
        /// </summary>
        /// <param name="prompt">User prompt for the image.</param>
        /// <param name="imageBytes">Base image bytes in PNG format.</param>
        /// <param name="maskBytes">Mask image bytes.</param>
        void InpaintSkybox(string prompt, byte[] imageBytes, byte[] maskBytes)
        {
            NewSkyboxTaskId();
            Task<string> inpaintingTask = clientController.Inpaint(
                imageBytes,
                maskBytes,
                prompt,
                skyboxTaskId,
                reportCompletion: ReportProgress
            );
            RunSkyboxTask(inpaintingTask);
        }


        /// <summary>
        /// Starts the inpainting process for the skybox.
        /// </summary>
        /// <param name="prompt">The user prompt for the image inpainting.</param>
        public void StartInpainting(string prompt)
        {
            var maskBytes = skyboxMasker.GetMaskBytes();
            var imageBytes = rewritableTexture.EncodeToPNG();
            InpaintSkybox(prompt, imageBytes, maskBytes);
        }


        /// <summary>
        /// Refines the current image with the given prompt.
        /// </summary>
        /// <param name="prompt">User prompt for the image.</param>
        public void RefineSkybox(string prompt)
        {
            var imageBytes = rewritableTexture.EncodeToPNG();
            NewSkyboxTaskId();
            Task<string> refiningTask = clientController.RefineSkybox(
                imageBytes, prompt, skyboxTaskId, ReportProgress
            );
            RunSkyboxTask(refiningTask);
        }

        /// <summary>
        /// Removes the seam line at the image border.
        /// </summary>
        public void RemoveSeam()
        {
            var imageBytes = rewritableTexture.EncodeToPNG();
            NewSkyboxTaskId();
            Task<string> seamFixingTask = clientController.RemoveSkyboxSeam(
                imageBytes, skyboxTaskId, ReportProgress
            );
            RunSkyboxTask(seamFixingTask);
        }


        /// <summary>
        /// Extends the skybox to be bigger.
        /// </summary>
        public void ExtendSkybox()
        {
            var imageBytes = rewritableTexture.EncodeToPNG();
            NewSkyboxTaskId();
            Task<string> extendSkyboxTask = clientController.ExtendSkybox(
                imageBytes, skyboxTaskId, ReportProgress
            );
            RunSkyboxTask(extendSkyboxTask);
        }


        /// <summary>
        /// Initializes the script by retrieving the ClientController component.
        /// </summary>
        void Start()
        {
            clientController = GetComponent<ClientController>();
            paintActionReference.action.started += skyboxMasker.OnStartPainting;
            paintActionReference.action.canceled += skyboxMasker.OnStopPainting;
        }
    }
}
