using AiWorldGeneration.TCP;

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;


namespace AiWorldGeneration.Skybox
{

    /// <summary>
    /// Create a mask on the skybox.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class SkyboxMasker : MonoBehaviour
    {
        [SerializeField, Tooltip("Maximum brush stroke radius (in pixels).")]
        int maxStrokeRadius = 50;

        [SerializeField, Tooltip("Path to get the configuration data.")]
        TextAsset configurationPath;

        [SerializeField, Tooltip("Initial radius (in pixels) of the painting brush stroke."), Range(0, 50)]
        float strokeRadius;

        [Tooltip("Action to make the brush change size")]
        [SerializeField]
        InputActionReference mouseScrollAction;

        [Tooltip("Action to track the pointer's position on the screen")]
        [SerializeField]
        InputActionReference mousePointAction;

        // Event delegates triggered on click.
        [SerializeField]
        private UnityEvent onClick = new();

        /// <summary>
        /// Whether we are currently painting mode.
        ///
        /// See also <see cref="paintingEnabled"/> to check if we are allowed to paint.
        /// </summary>
        bool paintingActivated;

        /// <summary>
        /// Brush position for painting.
        /// </summary>
        Transform trackingTarget;

        /// <summary>
        /// The original texture used for this GameObject, without edit.
        /// </summary>
        Texture2D originalTexture;

        /// <summary>
        /// The texture being currently assigned to the GameObject, may be modified.
        /// </summary>
        Texture2D currentTexture;

        /// <summary>
        /// Defines if we have the right to paint.
        ///
        /// See also <see cref="paintingActivated"/> to check if we are actively painting.
        /// </summary>
        bool paintingEnabled = true;

        [Tooltip("Lets you switch between XR and desktop interaction modes.")]
        [SerializeField]
        bool desktopMode;

        /// <summary>
        /// Activate the mask painting.
        /// </summary>
        public bool PaintingEnabled { 
            get {return paintingEnabled;}
            set {paintingEnabled = value || !desktopMode;}
        }

        /// <summary>
        /// Maximum stroke radius.
        /// </summary>
        public int MaxStrokeRadius { get { return maxStrokeRadius; } }


        /// <summary>
        /// Converts Cartesian coordinates to Spherical coordinates.
        /// </summary>
        /// <param name="cartesianCoordinates">The Cartesian coordinates to be converted.</param>
        /// <returns>A Vector3 containing the calculated Spherical coordinates (radius, inclination, azimuth).</returns>
        private Vector3 CartesianToSpherical(Vector3 cartesianCoordinates)
        {
            Vector3 sphericalCoordinates = new(
                // Calculate radius
                cartesianCoordinates.magnitude,

                // Calculate inclination
                //Mathf.Atan2(cartesianCoordinates.y, Mathf.Sqrt(Mathf.Pow(cartesianCoordinates.x, 2) + Mathf.Pow(cartesianCoordinates.z, 2))) // old version, did not work,
                Mathf.Acos(cartesianCoordinates.y), // r is always 1

                // Calculate azimuth
                Mathf.Atan2(cartesianCoordinates.z, cartesianCoordinates.x)
            );

            return sphericalCoordinates;
        }

        /// <summary>
        /// Take spherical coordinates as input and converts it to UV coordinates (2D texture coordinates).
        /// </summary>
        /// <param name="sphericalCoordinates">Spherical coordinates in format (radius, altitude, azimuth)</param>
        /// <returns>A new Vector2 containing the calculated U and V coordinates</returns>
        private Vector2 SphericalToUV(Vector3 sphericalCoordinates)
        {
            return new Vector2(
                (sphericalCoordinates[2] / Mathf.PI + 1) / 2,
                1 - sphericalCoordinates[1] / Mathf.PI
            );
        }


        /// <summary>
        /// Quickly paints a circle on the texture at the specified position with the given radius and color, whenever possible.
        /// 
        /// It may be 15% faster than PaintCircle, but it is unreliable at painting seams.
        /// </summary>
        /// <param name="position">The position in the texture where the circle should be painted. This is a 2D integer vector representing the coordinates in the texture.</param>
        /// <param name="radius">The radius of the circle to be painted. This is a float value representing the distance from the center of the circle to its edge.</param>
        /// <param name="col">The color of the circle to be painted. This is a Color object representing the color of the circle.</param>
        /// <seealso cref="PaintCircle"/>
        private void PaintCircleFast(Vector2Int position, float radius, Color col)
        {
            Vector2Int maxPos = Vector2Int.RoundToInt(new(currentTexture.width, currentTexture.height));
            Vector2Int tempPos;
            int intRadius = (int)radius;
            // Biggest box inside the circle
            int boxSize = (int)(radius / Mathf.Sqrt(2));
            // Check for texture boundaries
            boxSize = Mathf.Min(
                boxSize,
                position[0] - boxSize,
                position[1] - boxSize,
                currentTexture.width - position[0] - boxSize,
                currentTexture.height - position[1] - boxSize
            );
            // Avoid negative values
            boxSize = Mathf.Max(0, boxSize);
            Color32[] color32s = new Color32[boxSize * boxSize * 4];
            Array.Fill(color32s, col);
            // Apply the texture square (at most 64% of values)
            currentTexture.SetPixels32(
                position[0] - boxSize, position[1] - boxSize, boxSize * 2, boxSize * 2, color32s
            );

            // Individually set the remaining pixels
            for (int x = position[0] - intRadius; x < position[0] + radius; x++)
            {
                for (int y = position[1] - intRadius; y < position[1] + radius; y++)
                {
                    tempPos = new(x, y);
                    // Pass if the pixels was already painted
                    if (Mathf.Abs(x - position[0]) < boxSize && Mathf.Abs(y - position[1]) < boxSize)
                        continue;
                    if ((tempPos - position).sqrMagnitude < Mathf.Pow(radius, 2))
                    {
                        tempPos = new(x % maxPos[0], Mathf.Clamp(y, 0, maxPos[1] - 1));
                        currentTexture.SetPixel(tempPos[0], tempPos[1], col);
                    }
                }
            }

            currentTexture.Apply();
        }

        /// <summary>
        /// Paints a circle on the texture at the specified position with the given radius and color.
        /// </summary>
        /// <param name="position">The position in the texture where the circle should be painted. This is a 2D integer vector representing the coordinates in the texture.</param>
        /// <param name="radius">The radius of the circle to be painted. This is a float value representing the distance from the center of the circle to its edge.</param>
        /// <param name="col">The color of the circle to be painted. This is a Color object representing the color of the circle.</param>
        /// <seealso cref="PaintCircleFast"/>
        private void PaintCircle(Vector2Int position, float radius, Color col)
        {
            Vector2Int maxPos = Vector2Int.RoundToInt(new(currentTexture.width, currentTexture.height));
            Vector2Int tempPos;
            for (int x = position[0] - (int)radius; x < position[0] + radius; x++)
            {
                for (int y = position[1] - (int)radius; y < position[1] + radius; y++)
                {
                    tempPos = new(x, y);
                    if ((tempPos - position).sqrMagnitude < Mathf.Pow(radius, 2))
                    {
                        tempPos = new(x % maxPos[0], Mathf.Clamp(y, 0, maxPos[1] - 1));
                        currentTexture.SetPixel(tempPos[0], tempPos[1], col);
                    }
                }
            }

            currentTexture.Apply();
        }

        /// <summary>
        /// Paint the the texture a circle around the pointing direction.
        /// </summary>
        /// <param name="direction">Pointing direction in world space.</param>
        void StartPainting(Vector3 direction)
        {
            Vector3 spherePos = CartesianToSpherical(direction);

            Vector2 uvCoordinates = SphericalToUV(spherePos);

            Vector2 textureCoordinate;

            /*
            // Should be mathematically exact, but doesn't work
            textureCoordinate: (sin(ray.direction.y * pi) + 1) / 2
            // A bit better but inexact
            textureCoordinate: 
                vector2(currentTexture.width, currentTexture.height) * 
                vector2(uvCoordinates[0], (direction.y + 1) / 2)
            */
            textureCoordinate = Vector2.Scale(new(currentTexture.width, currentTexture.height), uvCoordinates);

            // Add white dot at the clicked texture coordinates
            PaintCircle(Vector2Int.RoundToInt(textureCoordinate), strokeRadius, Color.white);
        }

        /// <summary>
        /// Flips the input texture horizontally.
        /// </summary>
        /// <param name="inputTexture">The texture to be flipped.</param>
        /// <returns>A new texture with the input texture flipped horizontally.</returns>
        public Texture2D FlipTextureHorizontally(Texture2D inputTexture)
        {
            Texture2D flippedTexture = new(inputTexture.width, inputTexture.height, inputTexture.format, false);
            for (int x = 0; x < inputTexture.width; x++)
            {
                flippedTexture.SetPixels(
                    inputTexture.width - x - 1,
                    0,
                    1,
                    inputTexture.height,
                    inputTexture.GetPixels(x, 0, 1, inputTexture.height)
                );
            }
            flippedTexture.Apply();
            return flippedTexture;
        }

        /// <summary>
        /// Flip the image and encode it in PNG.
        /// </summary>
        /// <returns>The bytes of the resulting image.</returns>
        public byte[] GetMaskBytes()
        {
            return FlipTextureHorizontally(currentTexture).EncodeToPNG();
        }

        /// <summary>
        /// Save the mask to disk.
        /// </summary>
        /// <returns>Path to the mask</returns>
        public string SaveTexture()
        {
            byte[] bytes = GetMaskBytes();
            var data = JsonUtility.FromJson<JsonInterface>(configurationPath.text);
            System.IO.File.WriteAllBytes(data.maskPath, bytes);
            return data.maskPath;
        }

        /// <summary>
        /// Wipe the current masked texture and replace it by the original texture.
        /// </summary>
        public void ResetTexture()
        {
            currentTexture = new(originalTexture.width, originalTexture.height, originalTexture.format, false);
            currentTexture.SetPixels(originalTexture.GetPixels());
            currentTexture.Apply();
            GetComponent<Renderer>().material.mainTexture = currentTexture;
        }

        /// <summary>
        /// Sets the radius of the brush stroke. 
        /// </summary>
        /// <param name="raidus">Stroke radius</param>
        public void SetBrushSize(float radius)
        {
            strokeRadius = Mathf.Clamp(radius, 0, maxStrokeRadius);
        }


        /// <summary>
        /// Applies the painting action once.
        /// </summary>
        public void OnPainting()
        {
            if (paintingEnabled)
            {
                Vector3 direction;
                if (desktopMode)
                {
                    // Use mouse instead of tracker
                    Vector2 mousePosition = mousePointAction.action.ReadValue<Vector2>();
                    direction = Camera.main.ScreenPointToRay(mousePosition).direction;
                }
                else
                {
                    // Works as long as the skybox masker is centered on the player camera
                    direction = trackingTarget.position - transform.position;
                    direction.Normalize();
                }

                StartPainting(direction);
            }
        }

        /// <summary>
        /// Handles the start painting event.
        /// </summary>
        void OnStartPainting()
        {
            paintingActivated = paintingEnabled;
            onClick.Invoke();
        }

        /// <summary>
        /// Handles the start painting event, it is mostly used in desktop mode.
        /// </summary>
        /// <param name="callbackContext">The InputAction.CallbackContext object containing the input data.</param>
        public void OnStartPainting(InputAction.CallbackContext callbackContext)
        {
            OnStartPainting();
        }

        /// <summary>
        /// Handles the start painting event, it is mostly used in VR mode.
        /// </summary>
        /// <param name="eventArgs">An object containing the interactable object's transform.</param>
        public void OnStartPainting(ActivateEventArgs eventArgs)
        {
            OnStartPainting();
            trackingTarget = eventArgs.interactableObject.transform;
        }

        /// <summary>
        /// Handles the stop painting event.
        /// </summary>
        /// <param name="callbackContext">The InputAction.CallbackContext object containing the input data.</param>
        public void OnStopPainting()
        {
            trackingTarget = null;
            paintingActivated = false;
        }

        /// <summary>
        /// Handles the stop painting event, it is mostly used in desktop mode.
        /// </summary>
        /// <param name="callbackContext">The InputAction.CallbackContext object containing the input data.</param>
        public void OnStopPainting(InputAction.CallbackContext callbackContext)
        {
            OnStopPainting();
        }

        /// <summary>
        /// Handles the wheel input to adjust the brush size.
        /// </summary>
        /// <param name="callbackContext">The InputAction.CallbackContext object containing the input data.</param>
        void OnWheelChange(InputAction.CallbackContext callbackContext)
        {
            SetBrushSize(strokeRadius + callbackContext.ReadValue<Vector2>()[1] / 50);
        }
        
        /// <summary>
        /// Initializes the script by setting up the material and textures.
        /// </summary>
        void Start()
        {
            Material material = GetComponent<Renderer>().material;
            if (material == null)
            {
                Debug.LogError("This GameObject has no material!");
                return;
            }
            originalTexture = (Texture2D)material.mainTexture;
            if (!originalTexture.isReadable)
            {
                Debug.LogError("Please enable 'Read/Write' error on the texture.");
                // Don't forget to use a RGB compression as well
                return;
            }
            ResetTexture();
            if (mousePointAction != null)
                mouseScrollAction.action.performed += OnWheelChange;
        }

        /// <summary>
        /// Adds paainting at this frame if necessary.
        /// </summary>
        void Update()
        {
            if (paintingActivated)
            {
                OnPainting();
            }
        }
    }
}
