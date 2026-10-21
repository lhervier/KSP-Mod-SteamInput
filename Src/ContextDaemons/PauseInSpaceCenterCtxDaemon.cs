using UnityEngine.SceneManagement;
using com.github.lhervier.ksp.mod.steaminput.model;

namespace com.github.lhervier.ksp.mod.steaminput 
{
    // <summary>
    //  This class is a context daemon that detects when the game is paused in the space center
    // </summary>
    public class PauseInSpaceCenterCtxDaemon : BaseContextDaemon
    {
        private static readonly ModLogger LOGGER = new ModLogger("PauseInSpaceCenterCtxDaemon");
        private static readonly int DELAY = 10;

        private DelayedActionDaemon delayedActionDaemon;

        public override EActionGroup CorrespondingActionGroup()
        {
            return EActionGroup.MenuControls;
        }

        public void Start()
        {
            LOGGER.LogInfo("Start");

            this.delayedActionDaemon = gameObject.AddComponent<DelayedActionDaemon>();

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public void OnDestroy()
        {
            LOGGER.LogInfo("OnDestroy");

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            Destroy(this.delayedActionDaemon);
        }

        protected void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LOGGER.LogDebug("OnSceneLoaded : " + scene.name);
            if( scene.name.ToUpper() != "SPACECENTER" ) return;

            GameEvents.onGUIAdministrationFacilitySpawn.Add(OnGUIAdministrationFacilitySpawn);
            GameEvents.onGUIAstronautComplexSpawn.Add(OnGUIAstronautComplexSpawn);
            GameEvents.onGUIMissionControlSpawn.Add(OnGUIMissionControlSpawn);
            GameEvents.onGUIRnDComplexSpawn.Add(OnGUIRnDComplexSpawn);
            
            GameEvents.onGamePause.Add(OnGamePause);
            GameEvents.onGameUnpause.Add(OnGameUnpause);
        }

        protected void OnSceneUnloaded(Scene scene)
        {
            LOGGER.LogDebug("OnSceneUnloaded : " + scene.name);
            if( scene.name.ToUpper() != "SPACECENTER" ) return;

            GameEvents.onGUIAdministrationFacilitySpawn.Remove(OnGUIAdministrationFacilitySpawn);
            GameEvents.onGUIAstronautComplexSpawn.Remove(OnGUIAstronautComplexSpawn);
            GameEvents.onGUIMissionControlSpawn.Remove(OnGUIMissionControlSpawn);
            GameEvents.onGUIRnDComplexSpawn.Remove(OnGUIRnDComplexSpawn);

            GameEvents.onGamePause.Remove(OnGamePause);
            GameEvents.onGameUnpause.Remove(OnGameUnpause);

            this.FireContextEnterOrLeave(false);
        }

        private void Pause() {
            LOGGER.LogDebug("   Pause: true");
            this.FireContextEnterOrLeave(true);
        }

        // ================================================================================================================
        
        protected void OnGamePause()
        {
            LOGGER.LogTrace("=> Game pause asked");
            this.delayedActionDaemon.TriggerDelayedAction(
                Pause, 
                DELAY
            );
        }
        
        private void OnGameUnpause()
        {
            LOGGER.LogTrace("=> Game unpaused");
            this.FireContextEnterOrLeave(false);
        }

        protected void OnGUIAdministrationFacilitySpawn() {
            LOGGER.LogTrace("=> OnGUIAdministrationFacilitySpawn: Cancelling pause");
            this.delayedActionDaemon.CancelDelayedAction(Pause);
        }

        protected void OnGUIAstronautComplexSpawn() {
            LOGGER.LogTrace("=> OnGUIAstronautComplexSpawn: Cancelling pause");
            this.delayedActionDaemon.CancelDelayedAction(Pause);
        }

        protected void OnGUIMissionControlSpawn() {
            LOGGER.LogTrace("=> OnGUIMissionControlSpawn: Cancelling pause");
            this.delayedActionDaemon.CancelDelayedAction(Pause);
        }

        protected void OnGUIRnDComplexSpawn() {
            LOGGER.LogTrace("=> OnGUIRnDComplexSpawn: Cancelling pause");
            this.delayedActionDaemon.CancelDelayedAction(Pause);
        }
    }
}
