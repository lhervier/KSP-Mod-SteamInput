using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine.SceneManagement;
using com.github.lhervier.ksp.mod.steaminput.model;

namespace com.github.lhervier.ksp.mod.steaminput 
{
    public abstract class BaseContextDaemon : MonoBehaviour {

        /// <summary>
        /// Event triggered when the daemon detects that the game is in a context
        /// </summary>
        private readonly EventData<BaseContextDaemon> _onEnterContext = new EventData<BaseContextDaemon>("BaseContextDaemon.OnEnterContext");
        public EventData<BaseContextDaemon> OnEnterContext() {
            return _onEnterContext;
        }

        /// <summary>
        /// Event triggered when the daemon detects that the game exited a context
        /// </summary>
        private readonly EventData<BaseContextDaemon> _onExitContext = new EventData<BaseContextDaemon>("BaseContextDaemon.OnExitContext");
        public EventData<BaseContextDaemon> OnExitContext() {
            return _onExitContext;
        }

        /// <summary>
        /// Whether the daemon is currently in the context
        /// </summary>
        private bool _inContext = false;
        public bool InContext() {
            return _inContext;
        }

        /// <summary>
        /// The action group that the daemon will activate
        /// when it detects that the game is in the context
        /// </summary>
        public abstract EActionGroup CorrespondingActionGroup();

        // =========================================================================================

        /// <summary>
        /// Send an event to tell that the daemon is entering or leaving a context
        /// </summary>
        /// <param name="enteringOrLeaving">Whether the game is entering or leaving the context</param>
        protected void FireContextEnterOrLeave(bool enteringOrLeaving) {
            if( enteringOrLeaving == this._inContext ) return;
            this._inContext = enteringOrLeaving;
            if( enteringOrLeaving ) {
                this._onEnterContext.Fire(this);
            } else {
                this._onExitContext.Fire(this);
            }
        }

        // =========================================================================================
        //  Usefull methods to check if the game is in a specific context
        // =========================================================================================

        /// <summary>
        /// Whether the game is in flight mode
        /// </summary>
        /// <param name="flightUiMode">The flight UI mode to check</param>
        protected bool InFlightMode(FlightUIMode flightUiMode) {
            if( this.InEVA() ) {
                return false;
            }
            return flightUiMode == FlightUIMode.STAGING || flightUiMode == FlightUIMode.MANEUVER_INFO || flightUiMode == FlightUIMode.MANEUVER_EDIT;
        }

        /// <summary>
        /// Whether the game is in flight mode
        /// This method uses the FlightUIModeController to get the current flight UI mode
        /// </summary>
        protected bool InFlightMode() {
            if( FlightUIModeController.Instance == null ) {
                return false;
            }
            return InFlightMode(FlightUIModeController.Instance.Mode);
        }

        /// <summary>
        /// Whether the game is in docking mode
        /// </summary>
        /// <param name="flightUiMode">The flight UI mode to check</param>
        protected bool InDockingMode(FlightUIMode flightUiMode) {
            if( this.InEVA() ) {
                return false;
            }
            return flightUiMode == FlightUIMode.DOCKING;
        }

        /// <summary>
        /// Whether the game is in docking mode
        /// This method uses the FlightUIModeController to get the current flight UI mode
        /// </summary>
        protected bool InDockingMode() {
            if( FlightUIModeController.Instance == null ) {
                return false;
            }
            return InDockingMode(FlightUIModeController.Instance.Mode);
        }

        /// <summary>
        /// Whether the game is in EVA mode
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        protected bool InEVA(Vessel vessel) {
            return vessel.isEVA;
        }

        /// <summary>
        /// Whether the game is in EVA mode
        /// This method uses the FlightGlobals to get the active vessel
        /// </summary>
        protected bool InEVA() {
            if( FlightGlobals.ActiveVessel == null ) {
                return false;
            }
            return InEVA(FlightGlobals.ActiveVessel);
        }

        /// <summary>
        /// Whether the game is in IVA mode
        /// This method uses the CameraManager to get the current camera mode
        /// </summary>
        protected bool InIVA() {
            if( CameraManager.Instance == null ) {
                return false;
            }
            return CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA;
        }
    }
}