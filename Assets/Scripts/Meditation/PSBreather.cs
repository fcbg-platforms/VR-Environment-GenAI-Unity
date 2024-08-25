using UnityEngine;

namespace AiWorldGeneration.Meditation
{
    public class PSBreather : MonoBehaviour
    {
        [Tooltip("Inspiration/expiration status. 1 is full inspiration.")]
        [Range(0, 1)]
        public float breathValue;

        private readonly float breathStrength = .5f;



        [Tooltip("Force field to apply to pqrticle to evoke inspiration.")]
        public ParticleSystemForceField inflowController;
        [Tooltip("Force field to apply to pqrticle to evoke expiration.")]
        public ParticleSystemForceField outflowController;

        void Update()
        {
            breathValue += (Input.GetKey(KeyCode.Space) ? 1 : -1) * breathStrength * Time.deltaTime;
            if (Input.GetKey(KeyCode.Space))
            {
                breathValue = Mathf.Max(breathValue, .5f);
            }
            else
            {
                breathValue = Mathf.Min(breathValue, .5f);
            }
            breathValue = Mathf.Clamp01(breathValue);
            if (breathValue > 0.5f)
            {
                inflowController.enabled = true;
                outflowController.enabled = false;
            }
            else
            {
                inflowController.enabled = false;
                outflowController.enabled = true;
            }
            inflowController.gravity = breathValue;
            outflowController.gravity = 1 - breathValue;
        }
    }
}
