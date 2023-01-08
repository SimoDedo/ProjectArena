using UnityEngine;

namespace Others
{
    /// <summary>
    ///     RotateTranslateByAxis allows to perform periodic roto-translations on the gameobject it is
    ///     attached to.
    /// </summary>
    public class RotateTranslateByAxis : MonoBehaviour
    {
        [SerializeField] private bool xTranslation;
        [SerializeField] private float xLength;
        [SerializeField] private float xTransSpeed;

        [SerializeField] private bool zTranslation;
        [SerializeField] private float zLength;
        [SerializeField] private float zTransSpeed;

        [SerializeField] private bool yTranslation;
        [SerializeField] private float yLength;
        [SerializeField] private float yTransSpeed;

        [SerializeField] private bool xRotation;
        [SerializeField] private float xRotSpeed;

        [SerializeField] private bool zRotation;
        [SerializeField] private float zRotSpeed;

        [SerializeField] private bool yRotation;
        [SerializeField] private float yRotSpeed;

        private Vector3 limitMax;
        private Vector3 limitMin;

        private bool xBack;
        private bool yBack;
        private bool zBack;

        #if UNITY_SERVER && !UNITY_EDITOR
        private void Awake()
        {
            // Do not rotate translate in server build, we don't see anything in any case
            Destroy(this);
        }
        #endif

        private void Start()
        {
            limitMax.x = transform.position.x + xLength / 2;
            limitMin.x = transform.position.x - xLength / 2;
            limitMax.y = transform.position.y + yLength / 2;
            limitMin.y = transform.position.y - yLength / 2;
            limitMax.z = transform.position.z + zLength / 2;
            limitMin.z = transform.position.z - zLength / 2;
        }

        // Rotates and translates the object with respect to the specified axis.
        private void Update()
        {
            if (xTranslation)
            {
                if (xBack == false)
                    transform.position = transform.position + transform.right * xTransSpeed * Time.deltaTime;
                else
                    transform.position = transform.position - transform.right * xTransSpeed * Time.deltaTime;
                if (transform.position.x > limitMax.x && xBack == false)
                    xBack = true;
                else if (transform.position.x < limitMin.x && xBack)
                    xBack = false;
            }

            if (zTranslation)
            {
                if (zBack)
                    transform.position = transform.position + transform.forward * zTransSpeed * Time.deltaTime;
                else
                    transform.position = transform.position - transform.forward * zTransSpeed * Time.deltaTime;
                if (transform.position.z > limitMax.z && zBack)
                    zBack = true;
                else if (transform.position.z < limitMin.z && zBack == false)
                    zBack = false;
            }

            if (yTranslation)
            {
                if (yBack)
                    transform.position = transform.position + transform.up * yTransSpeed * Time.deltaTime;
                else
                    transform.position = transform.position - transform.up * yTransSpeed * Time.deltaTime;
                if (transform.position.y > limitMax.y && yBack)
                    yBack = true;
                else if (transform.position.y < limitMin.y && yBack == false)
                    yBack = false;
            }

            if (xRotation)
                transform.Rotate(Vector3.right * Time.deltaTime * xRotSpeed);

            if (zRotation)
                transform.Rotate(Vector3.forward * Time.deltaTime * zRotSpeed);

            if (yRotation)
                transform.Rotate(Vector3.up * Time.deltaTime * yRotSpeed);
        }

        public Quaternion GetRotation()
        {
            return transform.rotation;
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }
    }
}