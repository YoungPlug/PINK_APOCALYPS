using System.Collections;
using UnityEngine;

namespace PinkApocalypse.World
{
    /// <summary>Moves one city car along short runs, turning around at obstacles or at the end of a run.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TrafficCarController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 3.2f;
        [SerializeField, Min(1f)] private float legDistance = 12f;
        [SerializeField, Min(30f)] private float turnRate = 120f;

        private Rigidbody body;
        private Vector3 travelDirection;
        private float distanceTravelled;
        private bool ready;

        private IEnumerator Start()
        {
            foreach (MeshCollider meshCollider in GetComponentsInChildren<MeshCollider>(true))
            {
                meshCollider.enabled = false;
                Destroy(meshCollider);
            }

            yield return new WaitForFixedUpdate();

            body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            travelDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (travelDirection.sqrMagnitude < 0.001f) travelDirection = Vector3.forward;
            speed = Random.Range(2.4f, 4.2f);
            legDistance = Random.Range(9f, 17f);
            ready = true;
        }

        private void FixedUpdate()
        {
            if (!ready || body == null) return;

            float step = speed * Time.fixedDeltaTime;
            if (distanceTravelled >= legDistance || HasObstacle(step + 0.35f))
            {
                travelDirection = -travelDirection;
                distanceTravelled = 0f;
            }

            Quaternion targetRotation = Quaternion.LookRotation(travelDirection, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, turnRate * Time.fixedDeltaTime));
            body.MovePosition(body.position + travelDirection * step);
            distanceTravelled += step;
        }

        private bool HasObstacle(float distance)
        {
            foreach (RaycastHit hit in body.SweepTestAll(travelDirection, distance, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (Mathf.Abs(hit.normal.y) > 0.65f) continue;
                return true;
            }
            return false;
        }
    }
}
