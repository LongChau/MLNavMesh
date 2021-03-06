﻿using RSToolkit.Space3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RSToolkit.Animation;
using RSToolkit.AI.Helpers;
using UnityEngine.AI;

namespace RSToolkit.AI
{
    [RequireComponent(typeof(BotNavMesh))]
    [RequireComponent(typeof(BotFlying))]
    [RequireComponent(typeof(ProximityChecker))]
    public class BotFlyable : Bot
    {
        public bool StartInAir = true;
        public enum FlyableStates
        {
            NotFlying,
            Landing,
            TakingOff,
            Flying
        }

        private BotNavMesh m_botNavMeshComponent;
        public BotNavMesh BotNavMeshComponent
        {
            get
            {
                if (m_botNavMeshComponent == null)
                {
                    m_botNavMeshComponent = GetComponent<BotNavMesh>();
                }
                return m_botNavMeshComponent;
            }

        }

        private BotFlying m_botFlyingComponent;
        public BotFlying BotFlyingComponent
        {
            get
            {
                if (m_botFlyingComponent == null)
                {
                    m_botFlyingComponent = GetComponent<BotFlying>();
                }

                return m_botFlyingComponent;
            }

        }

        private BotWanderNavMesh m_botWanderNavMeshComponent;
        public BotWanderNavMesh BotWanderNavMeshComponent
        {
            get
            {
                if(m_botWanderNavMeshComponent == null)
                {
                    m_botWanderNavMeshComponent = GetComponent<BotWanderNavMesh>();
                }
                return m_botWanderNavMeshComponent;
            }
        }

        private BotWanderFlying m_botWanderFlyingComponent;
        public BotWanderFlying BotWanderFlyingComponent
        {
            get
            {
                if (m_botWanderFlyingComponent == null)
                {
                    m_botWanderFlyingComponent = GetComponent<BotWanderFlying>();
                }
                return m_botWanderFlyingComponent;
            }
        }


        private Rigidbody m_rigidBodyComponent;
        public Rigidbody RigidBodyComponent
        {
            get
            {
                if (m_rigidBodyComponent == null)
                {
                    m_rigidBodyComponent = GetComponent<Rigidbody>();
                }

                return m_rigidBodyComponent;
            }

        }


        private FiniteStateMachine<FlyableStates> m_fsm;
        protected FiniteStateMachine<FlyableStates> m_FSM
        {
            get
            {
                InitFSM();
                return m_fsm;
            }
        }

        public FlyableStates CurrentState
        {
            get
            {
                return m_FSM.State;
            }
        }

        public void AddStateChangedListener(System.Action<FlyableStates> listener)
        {
            m_FSM.Changed += listener;
        }

        public void RemoveStateChangedListener(System.Action<FlyableStates> listener)
        {
            m_FSM.Changed -= listener;
        }

        private void ToggleFlight(bool on)
        {
            if (on)
            {
                RigidBodyComponent.constraints = RigidbodyConstraints.None;
                SetCurrentBotMovement(BotFlyingComponent);
                SetCurrentBotWander(BotWanderFlyingComponent);
            }
            else
            {
                RigidBodyComponent.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                RigidBodyComponent.velocity = Vector3.zero;
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                SetCurrentBotMovement(BotNavMeshComponent);
                SetCurrentBotWander(BotWanderNavMeshComponent);
            }
            

            BotNavMeshComponent.NavMeshAgentComponent.enabled = !on;
            BotNavMeshComponent.enabled = !on;
            BotFlyingComponent.Flying3DObjectComponent.enabled = on;
            BotFlyingComponent.enabled = on;
            
        }

        public bool TakeOff()
        {
            if (CurrentState == FlyableStates.NotFlying)
            {
                m_FSM.ChangeState(FlyableStates.TakingOff);
                return true;
            }

            return false;
        }

        void TakingOff_Enter()
        {
            StopWandering();
            BotFlyingComponent.Flying3DObjectComponent.HoverWhenIdle = true;
            ToggleFlight(true);
        }

        void TakingOff_Update()
        {
            if(!BotFlyingComponent.IsFarFromGround()) // IsCloseToGround())
            {
                BotFlyingComponent.Flying3DObjectComponent.ApplyVerticalThrust(true);       
            }           
            else
            {
                RigidBodyComponent.Sleep();
                m_FSM.ChangeState(FlyableStates.Flying);
            }
        }

        void TakingOff_Exit()
        {
            RigidBodyComponent.WakeUp();
        }

        bool m_freefall = false;

        public bool Land(bool onNavMesh = true, bool freefall = false)
        {
            //if (CurrentState != FlyableStates.Flying || (checkForGround && BotFlyingComponent.IsCloseToGround()))
            if(CurrentState != FlyableStates.Flying || (onNavMesh && !BotNavMeshComponent.NavMeshAgentComponent.IsAboveNavMeshSurface()))
            {
                return false;
            }
            m_freefall = freefall;
            m_FSM.ChangeState(FlyableStates.Landing);
            
            return true;
        }

        void Landing_Enter()
        {
            StopWandering();
            BotFlyingComponent.Flying3DObjectComponent.HoverWhenIdle = false;
            
        }

        void Landing_Update()
        {
            /*
            if (BotFlyingComponent.IsCloseToGround())
            {
                m_FSM.ChangeState(FlyableStates.NotFlying);

            }
            else*/
            if (!m_freefall)
            {
                BotFlyingComponent.Flying3DObjectComponent.ApplyVerticalThrust(false);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if(CurrentState == FlyableStates.Landing)
            {
                NavMeshHit navHit;
                for(int i = 0; i < collision.contacts.Length; i++)
                {
                    if (NavMesh.SamplePosition(collision.contacts[i].point, out navHit, 1f, NavMesh.AllAreas)){
                        m_FSM.ChangeState(FlyableStates.NotFlying);
                        break;
                    }
                }
            }
        }

        void NotFlying_Enter()
        {
            ToggleFlight(false);
            CharacterAnimParams.TrySetIsGrounded(AnimatorComponent, true);
        }

        void NotFlying_Exit()
        {
            ToggleFlight(false);
            CharacterAnimParams.TrySetIsGrounded(AnimatorComponent, false);
        }

        void Flying_Enter()
        {
            ToggleFlight(true);
        }

        public bool CanMove()
        {
            return CurrentState == FlyableStates.Flying || CurrentState == FlyableStates.NotFlying;
        }

        private void InitFSM()
        {
            if (m_fsm == null)
            {
                m_fsm = FiniteStateMachine<FlyableStates>.Initialize(this, StartInAir ? FlyableStates.Flying : FlyableStates.NotFlying);
                m_fsm.Changed += Fsm_Changed;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            InitFSM();
        }

        protected override void Update()
        {
            base.Update();
            CharacterAnimParams.TrySetSpeed(AnimatorComponent, m_currentBotMovementComponent.CurrentSpeed);
        }

        private void Fsm_Changed(FlyableStates state)
        {
            try
            {
                Debug.Log($"{transform.name} FlyableStates changed from {m_FSM.LastState.ToString()} to {state.ToString()}");

            }
            catch (System.Exception ex)
            {
                Debug.Log($"{transform.name} FlyableStates changed to {state.ToString()}");
            }
        }


    }
}