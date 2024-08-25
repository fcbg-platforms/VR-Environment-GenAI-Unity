using UnityEngine;
using UnityEngine.XR;


namespace AiWorldGeneration.Meditation
{
    public class ViewTracker : MonoBehaviour
    {

        [Tooltip("Any object that should react when viewed.")]
        public GameObject viewTarget;

        public float fixationPointRadius = .1f;

        [SerializeField]
        private Eyes eyes;

        private bool isLooking;

        // Start is called before the first frame update
        void Start()
        {
            eyes = new Eyes();
        }

        void ActivateTarget()
        {
            viewTarget.SetActive(true);
        }

        void DeactivateTarget()
        {
            viewTarget.SetActive(false);
        }


        void LookingEnter(Vector3 hitPosition)
        {
            isLooking = true;
            if (viewTarget.TryGetComponent<AudioSource>(out var audioSource))
            {
                audioSource.PlayOneShot(audioSource.clip);
            }
            Invoke(nameof(DeactivateTarget), 0.5f);
            Invoke(nameof(ActivateTarget), 2.5f);
        }

        void LookingExit()
        {
            isLooking = false;
            if (viewTarget.TryGetComponent<AudioSource>(out var audioSource))
            {
                audioSource.Stop();
            }
            CancelInvoke(nameof(DeactivateTarget));
        }

        // Update is called once per frame
        void Update()
        {
            bool gotTarget = false;
            Vector3 fixationPoint;
            if (
                eyes.TryGetFixationPoint(out fixationPoint) &&
                Physics.CheckSphere(fixationPoint, fixationPointRadius)
            )
            {
                foreach (var collider in Physics.OverlapSphere(fixationPoint, fixationPointRadius))
                {
                    if (collider.gameObject == viewTarget)
                    {
                        gotTarget = true;
                    }
                }
            }
            else if (
                Physics.Raycast(transform.position, transform.forward, out RaycastHit hitInfo, 10) &&
                hitInfo.transform.gameObject == viewTarget
            )
            {
                fixationPoint = hitInfo.point;
                gotTarget = true;
            }
            if (isLooking && !gotTarget)
            {
                LookingExit();
            }
            else if (!isLooking && gotTarget)
            {
                LookingEnter(fixationPoint);
            }
        }
    }
}
