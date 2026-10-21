using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using com.github.lhervier.ksp.mod.steaminput.model;

namespace com.github.lhervier.ksp.mod.steaminput
{
    /// <summary>
    /// Entry point of the mod: replaces Squad's Steam controller plugin and, for the whole game session,
    /// keeps the controller on the stock action set that matches what the player is doing.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, true)]
    public class KSPModSteamInput : MonoBehaviour
    {
        private static readonly ModLogger LOGGER = new ModLogger();

        /// <summary>
        /// Assembly name of the previous version of this mod, which declares its own action sets.
        /// </summary>
        private const string EXTENDED_ASSEMBLY = "KSPModSteamInputExtended";

        private ScreenMessage _screenMessage;
        private GamepadDaemon _gamepadDaemon;
        private ActionGroupDaemon _actionGroupDaemon;
        private IEnumerator _initializePluginCoroutine;

        // ====================================================================================
        //                      Unity Lifecycle
        // ====================================================================================

        protected void Awake()
        {
            LOGGER.LogInfo("Awake");
            DontDestroyOnLoad(this);
        }

        protected void Start()
        {
            LOGGER.LogInfo("Start");
            this._initializePluginCoroutine = InitializePlugin();
            StartCoroutine(this._initializePluginCoroutine);
        }

        public void OnDestroy()
        {
            if( this._initializePluginCoroutine != null ) {
                StopCoroutine(this._initializePluginCoroutine);
                this._initializePluginCoroutine = null;
            }
            if( this._gamepadDaemon != null ) {
                this._gamepadDaemon.OnGamepadConnected.Remove(OnControllerConnected);
                this._gamepadDaemon.OnGamepadConnectedWithError.Remove(OnControllerConnectedWithError);
                Destroy(this._gamepadDaemon);
                this._gamepadDaemon = null;
            }
            if( this._actionGroupDaemon != null ) {
                this._actionGroupDaemon.OnActionGroupChanged.Remove(OnActionGroupChanged);
                Destroy(this._actionGroupDaemon);
                this._actionGroupDaemon = null;
            }
            LOGGER.LogInfo("Destroyed");
        }

        // ====================================================================================
        //                      Initialization
        // ====================================================================================

        /// <summary>
        /// Starts the daemons once Squad's plugin is out of the way and Steam is ready. Gives up, with a
        /// message to the player, when SteamInputExtended is installed too.
        /// </summary>
        private IEnumerator InitializePlugin()
        {
            ModSettings.Load();

            // Both mods would drive the controller, and the Extended one needs an action manifest that
            // hides the stock action sets: refuse to start next to it.
            if( AssemblyLoader.loadedAssemblies.Any(a => a.name == EXTENDED_ASSEMBLY) )
            {
                LOGGER.LogError(EXTENDED_ASSEMBLY + " is installed too. Plugin will not start.");
                PopupDialog.SpawnPopupDialog(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    "KSPModSteamInput_ExtendedInstalled",
                    "Steam Input",
                    "Two Steam Input mods are installed: SteamInput and SteamInputExtended.\n\n" +
                    "Keep only one of them in GameData. SteamInput stays inactive until then.",
                    "OK",
                    true,
                    HighLogic.UISkin
                );
                yield break;
            }

            LOGGER.LogInfo("Waiting for Squad KSPSteamController plugin");
            yield return StartCoroutine(HandleKSPSteamController());

            LOGGER.LogInfo("Waiting for Steam (SteamManager)");
            bool steamReady = false;
            yield return TryInitializeSteam(() => steamReady = true);
            if (!steamReady)
            {
                yield break;
            }
            LOGGER.LogInfo("Steam ready");

            this._screenMessage = new ScreenMessage(string.Empty, 5f, ScreenMessageStyle.UPPER_RIGHT);

            this._actionGroupDaemon = gameObject.AddComponent<ActionGroupDaemon>();
            this._actionGroupDaemon.OnActionGroupChanged.Add(this.OnActionGroupChanged);

            this._gamepadDaemon = gameObject.AddComponent<GamepadDaemon>();
            this._gamepadDaemon.OnGamepadConnected.Add(this.OnControllerConnected);
            this._gamepadDaemon.OnGamepadConnectedWithError.Add(this.OnControllerConnectedWithError);

            LOGGER.LogInfo("Started");
        }

        /// <summary>
        /// Waits until Steam is initialized (one minute at most) and initializes the Steam Controller API.
        /// Calls <paramref name="onSuccess"/> only when both succeeded.
        /// </summary>
        private IEnumerator TryInitializeSteam(System.Action onSuccess)
        {
            const float timeoutSeconds = 60f;
            float elapsed = 0f;
            while (!SteamManager.Initialized && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!SteamManager.Initialized)
            {
                LOGGER.LogInfo("Steam not detected. Plugin will not start.");
                yield break;
            }

            LOGGER.LogInfo("Initializing Steam Controller API");
            if (!Steamworks.SteamController.Init())
            {
                LOGGER.LogError("SteamController.Init() failed. Plugin will not start.");
                yield break;
            }

            onSuccess?.Invoke();
        }

        /// <summary>
        /// Disables Squad's Steam controller plugin (SteamController.KSPSteamController), if it is loaded.
        /// </summary>
        private IEnumerator HandleKSPSteamController()
        {
            // Wait for the next frame to ensure KSPSteamController has started
            yield return new WaitForEndOfFrame();

            Assembly kspSteamCtrlr = null;
            Type controllerType = null;
            MonoBehaviour controller = null;

            try {
                kspSteamCtrlr = Assembly.Load("KSPSteamCtrlr");
                if (kspSteamCtrlr == null) {
                    LOGGER.LogInfo("KSPSteamCtrlr assembly not found");
                    yield break;
                }

                controllerType = kspSteamCtrlr.GetType("SteamController.KSPSteamController");
                if (controllerType == null) {
                    LOGGER.LogInfo("KSPSteamController Type not found");
                    yield break;
                }

                controller = FindObjectOfType(controllerType) as MonoBehaviour;
                if (controller == null) {
                    LOGGER.LogInfo("KSPSteamController component not found");
                    yield break;
                }
            }
            catch (Exception ex) {
                LOGGER.LogInfo("Error loading KSPSteamCtrlr: " + ex.Message);
                yield break;
            }

            while (!controller.gameObject.activeInHierarchy) {
                yield return null;
            }

            LOGGER.LogInfo("Desactivating Squad KSPSteamCtrlr Plugin");
            try {
                controller.StopAllCoroutines();
                controller.enabled = false;
                controller.gameObject.SetActive(false);
                Destroy(controller);
                LOGGER.LogInfo("Squad KSPSteamCtrlr Plugin deactivated");
            }
            catch (Exception ex) {
                LOGGER.LogInfo("Error disabling Squad KSPSteamCtrlr: " + ex.Message);
            }

            yield return new WaitForEndOfFrame();
        }

        // ==============================================================================
        //              Events
        // ==============================================================================

        private void OnActionGroupChanged(EActionGroup actionGroup)
        {
            LOGGER.LogInfo("Action set changed to : " + actionGroup);
            if( actionGroup == EActionGroup.None ) {
                LOGGER.LogError("Action group is None");
                return;
            }

            this._screenMessage.message = "Controller: " + actionGroup.ToString() + ".";
            ScreenMessages.PostScreenMessage(this._screenMessage);

            this._gamepadDaemon.ChangeActionGroup(actionGroup);
        }

        private void OnControllerConnected()
        {
            LOGGER.LogInfo("New Controller connected");
            _gamepadDaemon.ChangeActionGroup(_actionGroupDaemon.GetCurrentActionGroup());

            // The Steam version of KSP loads its settings before Steam emulates the controllers, so it
            // does not see them as joysticks: reload the settings once a controller is there.
            LOGGER.LogInfo("Reloading Game Settings so KSP will see the steam emulated controllers");
            GameSettings.LoadGameSettingsOnly();
        }

        private void OnControllerConnectedWithError(string error)
        {
            LOGGER.LogError("Controller connected with error: " + error);
            PopupDialog.SpawnPopupDialog(
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                "KSPModSteamInput_ControllerError",
                "Steam Input",
                "Steam does not know KSP's action sets (" + error + ").\n\n" +
                "If a 'game_actions_220200.vdf' file is in the 'controller_config' folder of Steam " +
                "(it comes from SteamInputExtended), remove it and restart Steam.",
                "OK",
                true,
                HighLogic.UISkin
            );
        }
    }
}
