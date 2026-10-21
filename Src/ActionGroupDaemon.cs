using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine.SceneManagement;
using com.github.lhervier.ksp.mod.steaminput.model;

namespace com.github.lhervier.ksp.mod.steaminput 
{
    public class ActionGroupDaemon : MonoBehaviour 
    {
        
        // <summary>
        //  Logger
        // </summary>
        private static readonly ModLogger LOGGER = new ModLogger("ActionSetDaemon");
        private static readonly ModLogger LOGGER_CONTEXT = new ModLogger("Contexts");
        private static ActionGroupDaemon _instance;
        public static ActionGroupDaemon Instance {
            get {
                return _instance;
            }
        }

        // <summary>
        //  Delay before applying an action set (in frames)
        // </summary>
        private static readonly int DELAY = 10;

        // <summary>
        //  The default action group
        // </summary>
        private static readonly EActionGroup DEFAULT_ACTION_GROUP = EActionGroup.MenuControls;

        // ==================================================================================

        // <summary>
        //  The daemons
        // </summary>
        private readonly List<BaseContextDaemon> contextDaemons = new List<BaseContextDaemon>();

        // <summary>
        //  The active contexts. Idealy, there should be only one active context.
        //  But some daemons will deactivate before the next one activates.
        //  And some daemons will activate before the previous one deactivates.
        //  So we can have zero or 2 active contexts at the same time.
        //  More than 2 active contexts should never happen.
        // </summary>
        private readonly List<BaseContextDaemon> activecontexts = new List<BaseContextDaemon>();
        public List<String> ActivatedContexts {
            get {
                return this.activecontexts.Select(c => c.GetType().Name).ToList();
            }
        }

        // <summary>
        //  Previous action group (so we don't display the message when the value has not changed)
        // </summary>
        private EActionGroup prevActionGroup;

        // <summary>
        //  Delayed Action daemon
        // </summary>
        private DelayedActionDaemon delayedActionDaemon;

        // <summary>
        //  The action group to set when triggering a delayed action
        // </summary>
        private EActionGroup actionGroupToSet;

        /// <summary>
        /// Called when the action set has changed
        /// </summary>
        public readonly EventData<EActionGroup> OnActionGroupChanged = new EventData<EActionGroup>("ActionGroupDaemon.OnActionGroupChanged");

        // ===============================================================================
        //                      Unity initialization
        // ===============================================================================

        // <summary>
        //  Make our daemon survive between scene loading
        // </summary>
        protected void Awake() 
        {
            LOGGER.LogInfo("Awake");
            DontDestroyOnLoad(this);
            LOGGER.LogDebug("Awaked");
        }

        // <summary>
        //  Start of the daemon
        // </summary>
        protected void Start() 
        {   
            LOGGER.LogInfo("Start");

            // Create the delayed action daemon
            LOGGER.LogInfo("Creating Delayed Actions Daemon");
            this.delayedActionDaemon = gameObject.AddComponent<DelayedActionDaemon>();
            LOGGER.LogInfo("Delayed Actions Daemon attached");
            this.actionGroupToSet = EActionGroup.None;
            this.prevActionGroup = EActionGroup.None;
            
            // Get all the daemons
            LOGGER.LogInfo("Loading Context Daemons");
            this.LoadContextDaemons();
            this.activecontexts.Clear();
            LOGGER.LogInfo("Context Daemons loaded");

            // Attach to the context daemons events
            LOGGER_CONTEXT.LogInfo("Attaching Context Daemons :");
            foreach(BaseContextDaemon daemon in this.contextDaemons) 
            {
                daemon.OnEnterContext().Add(this.OnEnterContext);
                daemon.OnExitContext().Add(this.OnExitContext);
                LOGGER_CONTEXT.LogInfo("- " + daemon.GetType().Name);
            }
            LOGGER_CONTEXT.LogInfo("Context Daemons attached : " + this.contextDaemons.Count);
            this.LogDaemons();

            _instance = this;
            LOGGER.LogDebug("Started");
        }

        // <summary>
        //  Plugin destroyed
        // </summary>
        public void OnDestroy() 
        {
            Destroy(this.delayedActionDaemon);
            
            foreach(BaseContextDaemon daemon in this.contextDaemons) 
            {
                daemon.OnEnterContext().Remove(this.OnEnterContext);
                daemon.OnExitContext().Remove(this.OnExitContext);
                Destroy(daemon);
            }
            this.contextDaemons.Clear();
            this.activecontexts.Clear();
            _instance = null;
            LOGGER.LogInfo("Destroyed");
        }

        // <summary>
        //  Load the context daemons
        // </summary>
        private void LoadContextDaemons()
        {
            this.contextDaemons.Clear();
            
            // Get all types that implement ControllerContextDaemon
            var daemonTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(BaseContextDaemon).IsAssignableFrom(t));

            // Add each daemon component to the GameObject
            foreach (var type in daemonTypes)
            {
                BaseContextDaemon component = gameObject.AddComponent(type) as BaseContextDaemon;
                this.contextDaemons.Add(component);
            }
        }

        // ====================================================================================

        // <summary>
        //  When a context is activated
        // </summary>
        private void OnEnterContext(BaseContextDaemon daemon)
        {
            LOGGER_CONTEXT.LogDebug("OnEnterContext : " + daemon.GetType().Name);
            this.activecontexts.Add(daemon);
            this.LogKSPContext();
            this.LogDaemons();
            this.UpdateActionGroup();
        }

        // <summary>
        //  When a context is deactivated
        // </summary>
        private void OnExitContext(BaseContextDaemon daemon)
        {
            LOGGER_CONTEXT.LogDebug("OnExitContext : " + daemon.GetType().Name);
            this.activecontexts.Remove(daemon);
            this.LogKSPContext();
            this.LogDaemons();
            this.UpdateActionGroup();
        }

        private void LogDaemons()
        {
            if( this.activecontexts.Count == 0 ) {
                LOGGER_CONTEXT.LogDebug("No active daemons contexts");
            } else if( this.activecontexts.Count == 1 ) {
                LOGGER_CONTEXT.LogDebug("Active daemon context: " + this.activecontexts[0].GetType().Name);
            } else {
                LOGGER_CONTEXT.LogDebug("Active daemons contexts: " + this.activecontexts.Count);
                foreach( BaseContextDaemon daemon in this.activecontexts ) {
                    LOGGER_CONTEXT.LogDebug("- " + daemon.GetType().Name);
                }
            }
        }

        private void LogKSPContext() {
            LOGGER_CONTEXT.LogDebug("   ");
            LOGGER_CONTEXT.LogDebug("KSP Context : ");
            LOGGER_CONTEXT.LogDebug("- Current Scene : " + SceneManager.GetActiveScene().name);
            LOGGER_CONTEXT.LogDebug("- HighLogic :");
            LOGGER_CONTEXT.LogDebug("  - LoadedScene : " + HighLogic.LoadedScene.ToString());
            LOGGER_CONTEXT.LogDebug("  - LoadedSceneHasPlanetarium : " + HighLogic.LoadedSceneHasPlanetarium);
            LOGGER_CONTEXT.LogDebug("  - LoadedSceneIsEditor : " + HighLogic.LoadedSceneIsEditor);
            LOGGER_CONTEXT.LogDebug("  - LoadedSceneIsFlight : " + HighLogic.LoadedSceneIsFlight);
            LOGGER_CONTEXT.LogDebug("  - LoadedSceneIsGame : " + HighLogic.LoadedSceneIsGame);
            LOGGER_CONTEXT.LogDebug("  - LoadedSceneIsMissionBuilder : " + HighLogic.LoadedSceneIsMissionBuilder);
            
            LOGGER_CONTEXT.LogDebug("- MapView : " + MapView.MapIsEnabled);

            LOGGER_CONTEXT.LogDebug("- FlightUIMode present : " + (FlightUIModeController.Instance != null));
            if( FlightUIModeController.Instance != null ) {
                LOGGER_CONTEXT.LogDebug("  FlightUIMode : " + FlightUIModeController.Instance.Mode.ToString());
            }

            LOGGER_CONTEXT.LogDebug("- Active Vessel present : " + (FlightGlobals.ActiveVessel != null));
            if( FlightGlobals.ActiveVessel != null ) {
                LOGGER_CONTEXT.LogDebug("  Active Vessel : " + FlightGlobals.ActiveVessel.name);
                LOGGER_CONTEXT.LogDebug("  Active Vessel is EVA : " + FlightGlobals.ActiveVessel.isEVA);
            }

            LOGGER_CONTEXT.LogDebug("- EditorFacility : " + EditorDriver.editorFacility.ToString());
            
            LOGGER_CONTEXT.LogDebug("- CameraManager present : " + (CameraManager.Instance != null));
            if( CameraManager.Instance != null ) {
                LOGGER_CONTEXT.LogDebug("  CameraMode : " + CameraManager.Instance.currentCameraMode.ToString());
            }
        }

        // ====================================================================================

        // <summary>
        //  Update the action group to use, depending on the activated contexts
        // </summary>
        private void UpdateActionGroup() 
        {
            if( this.activecontexts.Count == 0 ) {
                this.TriggerActionGroupChange(DEFAULT_ACTION_GROUP);
            } else {
                EActionGroup last = this.activecontexts[this.activecontexts.Count - 1].CorrespondingActionGroup();
                this.TriggerActionGroupChange(last);
            }
        }
        
        // ====================================================================================
        
        // <summary>
        //  Trigger an action group change
        //  <param name="actionGroup">The action group to apply</param>
        // </summary>
        private void TriggerActionGroupChange(EActionGroup actionGroup) 
        {
            this.CancelActionGroupChange();
            
            this.actionGroupToSet = actionGroup;
            this.delayedActionDaemon.TriggerDelayedAction(this._TriggerActionGroupChange, DELAY);
        }

        private void _TriggerActionGroupChange() 
        {
            if( this.actionGroupToSet == EActionGroup.None ) {
                LOGGER.LogError("No action group to set");
                return;
            }
            this._SetActionGroup(this.actionGroupToSet);
            this.actionGroupToSet = EActionGroup.None;
        }

        // <summary>
        //  Cancel an action group change
        // </summary>
        private void CancelActionGroupChange() 
        {
            this.delayedActionDaemon.CancelDelayedAction(this._TriggerActionGroupChange);
            this.actionGroupToSet = EActionGroup.None;
        }

        private void _SetActionGroup(EActionGroup actionGroup) 
        {
            if( actionGroup == EActionGroup.None ) {
                LOGGER.LogError("Action group is None");
                return;
            }
            
            if( this.prevActionGroup != EActionGroup.None )
            {
                if( actionGroup == this.prevActionGroup ) {
                    return;
                }
            }
            
            LOGGER.LogDebug("Setting action group : " + actionGroup.ToString());
            this.prevActionGroup = actionGroup;
            this.OnActionGroupChanged.Fire(actionGroup);
        }

        /// <summary>
        /// Get the current action set
        /// </summary>
        /// <returns>The current action set</returns>
        public EActionGroup GetCurrentActionGroup() {
            return this.prevActionGroup;
        }
    }
}
