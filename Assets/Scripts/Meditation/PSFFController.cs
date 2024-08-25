using UnityEngine;


namespace AiWorldGeneration.Meditation
{

    public class PSFFController : MonoBehaviour
    {
        [Tooltip("Allow manual control")]
        [SerializeField]
        private bool manualControl;

        [Tooltip("Current breathing strength ratio. Set higher value to strenghten this force field.")]
        [Range(0, 1)]
        public float breathValue;

        private readonly float breathStrength = .5f;

        ParticleSystemForceField particleSystemForceField;

        void Start()
        {
            particleSystemForceField = GetComponent<ParticleSystemForceField>();
        }

        void Update()
        {
            if (manualControl)
            {
                Debug.Log("aaaaaaaaaaaa");
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
                if (particleSystemForceField.shape == ParticleSystemForceFieldShape.Sphere)
                {
                    particleSystemForceField.gravityFocus = Mathf.Lerp(0.35f, .8f, breathValue);
                }
            }
            //particleSystemForceField.gravity = breathValue * 2 - 1;
        }
    }
}
