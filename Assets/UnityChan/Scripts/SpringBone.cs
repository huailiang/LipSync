using UnityEngine;

namespace UnityChan
{
    public class SpringBone : MonoBehaviour
    {
        public Transform child;

        public Vector3 boneAxis = new Vector3(-1.0f, 0.0f, 0.0f);
        public float radius = 0.05f;

        public bool isUseEachBoneForceSettings = false;
        public float stiffnessForce = 0.01f;

        public float dragForce = 0.4f;
        public Vector3 springForce = new Vector3(0.0f, -0.0001f, 0.0f);
        public SpringCollider[] colliders;
        public bool debug = true;
        public float threshold = 0.01f;
        private float springLength;
        private Quaternion localRotation;
        private Transform trs;
        private Vector3 currTipPos;
        private Vector3 prevTipPos;
        private Transform org;
        private SpringManager managerRef;

        private void Awake()
        {
            trs = transform;
            localRotation = transform.localRotation;
            managerRef = GetParentSpringManager(transform);
        }

        private SpringManager GetParentSpringManager(Transform t)
        {
            var springManager = t.GetComponent<SpringManager>();

            if (springManager != null)
                return springManager;

            if (t.parent != null)
            {
                return GetParentSpringManager(t.parent);
            }

            return null;
        }

        private void Start()
        {
            springLength = Vector3.Distance(trs.position, child.position);
            currTipPos = child.position;
            prevTipPos = child.position;
        }

        public void UpdateSpring()
        {
            //Kobayashi
            org = trs;
            trs.localRotation = Quaternion.identity * localRotation;

            float sqrDt = Time.deltaTime * Time.deltaTime;

            //stiffness
            Vector3 force = trs.rotation * (boneAxis * stiffnessForce) / sqrDt;

            //drag
            force += (prevTipPos - currTipPos) * dragForce / sqrDt;
            force += springForce / sqrDt;
            Vector3 temp = currTipPos;

            currTipPos = (currTipPos - prevTipPos) + currTipPos + (force * sqrDt);

            currTipPos = ((currTipPos - trs.position).normalized * springLength) + trs.position;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (Vector3.Distance(currTipPos, colliders[i].transform.position) <= (radius + colliders[i].radius))
                {
                    Vector3 normal = (currTipPos - colliders[i].transform.position).normalized;
                    currTipPos = colliders[i].transform.position + (normal * (radius + colliders[i].radius));
                    currTipPos = ((currTipPos - trs.position).normalized * springLength) + trs.position;
                }
            }

            prevTipPos = temp;

            Vector3 aimVector = trs.TransformDirection(boneAxis);
            Quaternion aimRotation = Quaternion.FromToRotation(aimVector, currTipPos - trs.position);
            //original
            //trs.rotation = aimRotation * trs.rotation;
            //Kobayahsi:Lerp with mixWeight
            Quaternion secondaryRotation = aimRotation * trs.rotation;
            trs.rotation = Quaternion.Lerp(org.rotation, secondaryRotation, managerRef.dynamicRatio);
        }

        private void OnDrawGizmos()
        {
            if (debug)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(currTipPos, radius);
            }
        }
    }
}
