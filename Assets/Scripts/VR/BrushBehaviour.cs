using AiWorldGeneration.Skybox;

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace AiWorldGeneration.VR
{
    /// <summary>
    /// Describes the behaviour of a brush tool.
    /// </summary>
    public class BrushBehaviour : MonoBehaviour
    {
        [SerializeField, Tooltip("Skybox masker associated with the scene.")]
        SkyboxMasker skyboxMasker;

        [SerializeField, Tooltip("Max distance above which the brush radius do not decrease."), Min(0)]
        float maxDecreaseDistance = 10f;

        /// <summary>
        /// If transform attached to the current interactor grabbing the GameObject.
        /// </summary>
        Transform grabber;


        /// <summary>
        /// Sets the grabber transform to the interactor's transform.
        /// </summary>
        /// <param name="selectEnterEventArgs">
        /// The event arguments containing information about the interactor and the interaction.
        /// </param>
        public void OnSelectEntered(SelectEnterEventArgs selectEnterEventArgs)
        {
            grabber = selectEnterEventArgs.interactorObject.transform;
        }

        /// <summary>
        /// Sets the grabber transform to null.
        /// </summary>
        public void OnSelectExited()
        {
            grabber = null;
        }


        /// <summary>
        /// Sets the brush radius if grabber is initialized.
        /// </summary>
        void Update()
        {
            if (grabber != null)
            {
                float distance = Vector3.Distance(grabber.position, transform.position);
                if (distance == 0f)
                {
                    skyboxMasker.SetBrushSize(skyboxMasker.MaxStrokeRadius);
                }
                else
                {
                    skyboxMasker.SetBrushSize(
                        Mathf.Lerp(skyboxMasker.MaxStrokeRadius, 0, distance / maxDecreaseDistance)
                    );
                }
            }
        }
    }
}
