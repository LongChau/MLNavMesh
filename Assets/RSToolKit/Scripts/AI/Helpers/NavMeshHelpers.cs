﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RSToolkit.AI.Helpers
{

    public static class NavMeshHelpers
    {
        public enum OffMeshLinkPosition
        {
            Off,
            Start,
            Mid,
            End
        }
        // Might use only velovity magnitude instead
        public static float GetCurrentSpeed(this NavMeshAgent self)
        {
            return self.velocity.magnitude / self.speed;
        }

        public static bool RandomNavPosInSphere(Vector3 origin, float radius, out Vector3 position ,float offset = 0f, int areamask = NavMesh.AllAreas)
        {
            position = origin + ((offset + (radius * Random.value)) * Random.insideUnitSphere.normalized);
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(origin, position, areamask, path);
            if(path.status == NavMeshPathStatus.PathInvalid || Vector3.Distance(origin, position) < offset)
            {
                return false;
            }
            return true;
        }

        public static bool AttemptRandomNavPosInSphere(Vector3 origin, float radius, out Vector3 position, float offset = 0f, int areamask = NavMesh.AllAreas, int attempts = 100)
        {
            position = origin;
            for(int i = 0; i < attempts; i++)
            {
                if(RandomNavPosInSphere(origin, radius, out position, offset, areamask))
                {
                    return true;
                }
            }
            return false;
        }

        public static OffMeshLinkPosition GetOffMeshLinkPosition(this NavMeshAgent agent, float startend_proximity = 0.25f)
        {
            if (!agent.isOnOffMeshLink)
            {
                return OffMeshLinkPosition.Off;
            }else if(Vector3.Distance(agent.transform.position, agent.currentOffMeshLinkData.startPos) 
                    < startend_proximity)
            {
                return OffMeshLinkPosition.Start;
            }else if(Vector3.Distance(agent.transform.position, agent.currentOffMeshLinkData.endPos)
                    < startend_proximity)
            {
                return OffMeshLinkPosition.End;
            }
            else
            {
                return OffMeshLinkPosition.Mid;
            }

        }

        public static bool IsAboveNavMeshSurface(this NavMeshAgent agent, out Vector3 navPosition)
        {
            var collider = agent.GetComponent<Collider>();
            var bottomColliderPoint = collider.ClosestPointOnBounds(agent.transform.position + Vector3.down * ((agent.height / 2f) + 0.1f));
            RaycastHit rayHit;
            NavMeshHit navHit;
            if (Physics.Raycast(bottomColliderPoint, Vector3.down, out rayHit))
            {
                if(NavMesh.SamplePosition(rayHit.point, out navHit, 1f, NavMesh.AllAreas))
                {
                    navPosition = navHit.position;
                    return true;
                }
            }
            navPosition = Vector3.zero;
            return false;
        }

        public static bool IsAboveNavMeshSurface(this NavMeshAgent agent)
        {
            Vector3 navPosition;
            return agent.IsAboveNavMeshSurface(out navPosition);
        }

        public static Vector3? TryGetNavPosFromUnderneathCircle(this NavMeshAgent agent, float radius, float attempts)
        {
            var collider = agent.GetComponent<Collider>();
            var bottomColliderPoint = collider.ClosestPointOnBounds(agent.transform.position + Vector3.down * ((agent.height / 2f) + 0.1f));

            RaycastHit rayHit;
            
            Physics.Raycast(bottomColliderPoint, Vector3.down, out rayHit);
            return null;
        }

        public static void DrawGizmoDestination(NavMeshAgent agent)
        {
#if UNITY_EDITOR
            if(agent.GetCurrentSpeed() <= 0)
            {
                return;
            }
            var oldColor = UnityEditor.Handles.color;
            UnityEditor.Handles.color = new Color(1f, 1f, 0.008f, 0.55f);
            UnityEditor.Handles .DrawWireDisc(agent.destination, Vector3.up, 0.25f);

            UnityEditor.Handles.color = oldColor;
#endif
        }
    }
}