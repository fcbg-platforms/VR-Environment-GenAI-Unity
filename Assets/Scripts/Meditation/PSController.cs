using UnityEngine;


namespace AiWorldGeneration.Meditation
{
    public class PSController : MonoBehaviour
    {
        [Range(-1, 1)]
        [SerializeField]
        float breathValue;

        private readonly float breathStrength = .5f;

        ParticleSystem particleSystemm;

        void Start()
        {
            particleSystemm = GetComponent<ParticleSystem>();
        }

        void Update()
        {
            breathValue += (Input.GetKey(KeyCode.Space) ? 1 : -1) * breathStrength * Time.deltaTime;
            breathValue = Mathf.Clamp01(breathValue);
            var shape = particleSystemm.shape;
            shape.radius = (breathValue * .5f + .5f) * 5;
        }
    }
}
