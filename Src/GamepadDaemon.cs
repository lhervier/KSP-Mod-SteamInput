using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Steamworks;
using com.github.lhervier.ksp.mod.steaminput.model;

namespace com.github.lhervier.ksp.mod.steaminput 
{
    /// <summary>
    /// Daemon in charge of listening to controller connection/disconnection
    /// It also allow to change the current action set of the controller
    /// </summary>
    public class GamepadDaemon : MonoBehaviour 
    {
        
        // ==========================================================================================
        //                          Static properties
        // ==========================================================================================

        /// <summary>
        /// Logger object
        /// </summary>
        private static readonly ModLogger LOGGER = new ModLogger("GamepadDaemon");

        private static GamepadDaemon _instance;
        public static GamepadDaemon Instance {
            get {
                return _instance;
            }
        }

        // ==========================================================================================

        /// <summary>
        /// Called when a new gamepad is connected
        /// </summary>
        public readonly EventVoid OnGamepadConnected = new EventVoid("GamepadDaemon.OnGamepadConnected");

        /// <summary>
        /// Called when a gamepad is disconnected
        /// </summary>
        public readonly EventVoid OnGamepadDisconnected = new EventVoid("GamepadDaemon.OnGamepadDisconnected");

        /// <summary>
        /// Called when an error occurs and a new gamepad cannot be connected
        /// </summary>
        public readonly EventData<string> OnGamepadConnectedWithError = new EventData<string>("GamepadDaemon.OnGamepadConnectedWithError");

        /// <summary>
        /// Is a gamepad connected ?
        /// </summary>
        public bool GamepadConnected { get; private set; }

        /// <summary>
        /// Is a gamepad connected with an error ?
        /// </summary>
        public bool GamepadConnectedWithErrors { get; private set; }

        // ==============================================

        /// <summary>
        /// The names of the action groups
        /// </summary>
        private string[] actionGroupNames;

        // <summary>
        //  Handle to the first connected gamepad. No sense if GamepadConnected = false
        // </summary>
        private ControllerHandle_t controllerHandle;

        // <summary>
        //  The action sets handles defined in the steam controller configuration template
        // </summary>
        private readonly IDictionary<string, ControllerActionSetHandle_t> actionsSetsHandles = new Dictionary<string, ControllerActionSetHandle_t>();

        // <summary>
        //  Handles to the connected steam controllers.
        //  Don't use. This array is here to prevent from instanciating a new one every cycle.
        // </summary>
        private ControllerHandle_t[] _controllerHandles = new ControllerHandle_t[Steamworks.Constants.STEAM_CONTROLLER_MAX_COUNT];

        // =======================================================================

        /// <summary>
        /// Coroutine to check for a gamepad
        /// </summary>
        private IEnumerator checkForGamepadCoroutine;

        /// <summary>
        /// Whether the failure to load the action sets handles has already been reported
        /// </summary>
        private bool errorReported;

        // =======================================================================
        //              Unity Lifecycle
        // =======================================================================

        /// <summary>
        /// Component awaked
        /// </summary>
        public void Awake() 
        {
            DontDestroyOnLoad(this);
            _instance = this;
            LOGGER.LogInfo("Awaked");
        }

        /// <summary>
        /// Startup of the component
        /// </summary>
        public void Start() 
        {
            LOGGER.LogInfo("Starting");

            this.GamepadConnected = false;
            this.GamepadConnectedWithErrors = false;

            // Load the action sets from the enumeration
            LOGGER.LogInfo("Loading action groups");
            this.actionGroupNames = Enum.GetValues(typeof(EActionGroup))
                .Cast<EActionGroup>()
                .Where(actionGroup => actionGroup != EActionGroup.None)
                .Select(actionGroup => actionGroup.ToString())
                .ToArray();
            LOGGER.LogInfo("Action groups loaded : " + this.actionGroupNames.Length);
            
            // Start the main loop
            this.checkForGamepadCoroutine = this.CheckForGamepad();
            this.StartCoroutine(this.checkForGamepadCoroutine);
            
            LOGGER.LogInfo("Started");
        }

        /// <summary>
        /// Component destroyed
        /// </summary>
        public void OnDestroy() 
        {
            this.StopCoroutine(this.checkForGamepadCoroutine);
            this.GamepadConnected = false;
            this.GamepadConnectedWithErrors = false;
            _instance = null;
            LOGGER.LogInfo("Destroyed");
        }

        // ==============================================================================
        //              Detection of connection/disconnection of gamepads
        // ==============================================================================
        
        /// <summary>
        /// Main loop to detect gamepad connection/disconnection
        /// </summary>
        private IEnumerator CheckForGamepad() 
        {
            WaitForSeconds waitFor1Second = new WaitForSeconds(1);
            while( true ) 
            {
                Steamworks.SteamController.RunFrame();

                // Detect connection/disconnection
                LOGGER.LogTrace("Detecting controllers connection/disconnection :");
                int nbGamepads = Steamworks.SteamController.GetConnectedControllers(this._controllerHandles);
                LOGGER.LogTrace("- nbGamepads connected: " + nbGamepads);
                bool newGamepad;
                bool disconnectedGamepad;
                if( nbGamepads == 0 ) 
                {
                    LOGGER.LogTrace("- No gamepad connected");
                    if( this.GamepadConnected ) 
                    {
                        LOGGER.LogDebug("  A gamepad was previously connected");
                        newGamepad = false;
                        disconnectedGamepad = true;
                    }
                    else
                    {
                        LOGGER.LogTrace("  No gamepad previously connected");
                        newGamepad = false;
                        disconnectedGamepad = false;
                    }
                }
                else
                {
                    LOGGER.LogTrace("- A gamepad is connected");
                    if( this.GamepadConnected ) 
                    {
                        if( this.controllerHandle == this._controllerHandles[0] ) 
                        {
                            LOGGER.LogTrace("  The same gamepad is connected");
                            newGamepad = false;
                            disconnectedGamepad = false;
                        }
                        else
                        {
                            LOGGER.LogDebug("  A different gamepad is connected");
                            newGamepad = true;
                            disconnectedGamepad = true;
                        }
                    }
                    else
                    {
                        LOGGER.LogTrace("  No gamepad previously connected");
                        newGamepad = true;
                        disconnectedGamepad = false;
                    }
                }
                LOGGER.LogTrace("- newGamepad: " + newGamepad);
                LOGGER.LogTrace("- disconnectedGamepad: " + disconnectedGamepad); 

                // Disconnect the current gamepad
                if( disconnectedGamepad ) 
                {
                    LOGGER.LogInfo("Gamepad disconnected");
                    this.UnloadActionSets();
                    this.GamepadConnected = false;
                    this.GamepadConnectedWithErrors = false;
                    this.OnGamepadDisconnected.Fire();
                }

                // Connects a new gamepad
                if( newGamepad ) 
                {
                    LOGGER.LogInfo("Gamepad connected");
                    this.controllerHandle = this._controllerHandles[0];
                    this.GamepadConnected = true;
                    this.GamepadConnectedWithErrors = !this.LoadActionSetsHandles();
                    if( this.GamepadConnectedWithErrors )
                    {
                        // Steam may not have loaded the controller configuration yet: try again on the next
                        // loop instead of giving up, and only tell the user once.
                        this.GamepadConnected = false;
                        this.UnloadActionSets();
                        if( !this.errorReported )
                        {
                            this.errorReported = true;
                            this.OnGamepadConnectedWithError.Fire("Unable to load action sets handles.");
                        }
                        yield return waitFor1Second;
                        continue;
                    }
                    this.errorReported = false;
                    this.StartCoroutine(this.SayHello());
                    this.OnGamepadConnected.Fire();
                }

                // Wait for 1 second
                yield return waitFor1Second;
            }
        }
        
        /// <summary>
        /// Load action sets handles.
        /// </summary>
        /// <returns>True if the action sets handles were loaded, false otherwise</returns>
        private bool LoadActionSetsHandles() 
        {
            LOGGER.LogInfo("Loading Action Set Handles");
            foreach(string actionSetName in this.actionGroupNames) 
            {
                LOGGER.LogInfo("- Getting action set handle for " + actionSetName);
                // Action Sets list should depend on the used controller. But that's not what the API is waiting for...
                ControllerActionSetHandle_t actionSetHandle = Steamworks.SteamController.GetActionSetHandle(actionSetName);
                if( actionSetHandle.m_ControllerActionSetHandle == 0L ) 
                {
                    return false;
                }
                this.actionsSetsHandles[actionSetName] = actionSetHandle;
            }
            return true;
        }

        /// <summary>
        /// Unloads the action sets
        /// </summary>
        private void UnloadActionSets() 
        {
            this.actionsSetsHandles.Clear();
        }

        // <summary>
        //  Trigger a set of pulses on the current controller to say hello
        // </summary>
        private IEnumerator SayHello() 
        {
            if( !this.GamepadConnected ) 
            {
                LOGGER.LogError("SayHello: Gamepad not connected");
                yield break;
            }

            LOGGER.LogInfo("Hello new Gamepad !!");
            for( int i = 0; i < 4; i++ ) 
            {
                Steamworks.SteamController.TriggerHapticPulse(this.controllerHandle, Steamworks.ESteamControllerPad.k_ESteamControllerPad_Right, ushort.MaxValue);
                yield return new WaitForSeconds(0.1f);
                Steamworks.SteamController.TriggerHapticPulse(this.controllerHandle, Steamworks.ESteamControllerPad.k_ESteamControllerPad_Left, ushort.MaxValue);
                yield return new WaitForSeconds(0.1f);
            }
        }

        // =========================================================================================

        // <param name="actionGroup">The action group to set</param>
        // <summary>
        //  Change the current action group
        // </summary>
        public void ChangeActionGroup(EActionGroup actionGroup) 
        {
            if( !this.GamepadConnected ) 
            {
                return;
            }
            if( actionGroup == EActionGroup.None )
            {
                return;
            }
            if( !this.actionsSetsHandles.ContainsKey(actionGroup.ToString()) )
            {
                LOGGER.LogError("ChangeActionGroup: Action group not found: " + actionGroup.ToString());
                return;
            }
            
            LOGGER.LogDebug("ChangeActionGroup: " + actionGroup.ToString());
            Steamworks.SteamController.ActivateActionSet(
                this.controllerHandle, 
                this.actionsSetsHandles[actionGroup.ToString()]
            );
        }
    }
}