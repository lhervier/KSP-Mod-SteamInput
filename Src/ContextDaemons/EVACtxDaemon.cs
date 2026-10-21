using UnityEngine.SceneManagement;
using com.github.lhervier.ksp.mod.steaminput.model;

namespace com.github.lhervier.ksp.mod.steaminput 
{
    // <summary>
    //  This class is a context daemon that detects when the game is in EVA mode
    // </summary>
    public class EVACtxDaemon : BaseContextDaemon
    {
        private static readonly ModLogger LOGGER = new ModLogger("EVACtxDaemon");
        
        public override EActionGroup CorrespondingActionGroup()
        {
            return EActionGroup.EVAControls;
        }

        public void Start()
        {
            LOGGER.LogInfo("Start");

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public void OnDestroy()
        {
            LOGGER.LogInfo("OnDestroy");

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        protected void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LOGGER.LogDebug("OnSceneLoaded : " + scene.name);
            if( scene.name.ToUpper() != "PFLIGHT4") return;
            
            GameEvents.OnMapEntered.Add(OnMapEntered);
            GameEvents.OnMapExited.Add(OnMapExited);
            
            this.OnMapExited();
        }

        protected void OnSceneUnloaded(Scene scene)
        {
            LOGGER.LogDebug("OnSceneUnloaded : " + scene.name);
            if( scene.name.ToUpper() != "PFLIGHT4" ) {
                return;
            }
            this.FireContextEnterOrLeave(false);

            GameEvents.OnMapEntered.Remove(OnMapEntered);
            GameEvents.OnMapExited.Remove(OnMapExited);
            
            GameEvents.onVesselChange.Remove(OnVesselChange);
        }
        
        // ============================================================

        private void OnMapEntered()
        {
            LOGGER.LogTrace("=> OnMapEntered");
            GameEvents.onVesselChange.Remove(OnVesselChange);
            this.FireContextEnterOrLeave(false);
        }

        private void OnMapExited()
        {
            LOGGER.LogTrace("=> OnMapExited");
            // The EVA construction mode keeps the EVA action set: the context does not leave with it.
            GameEvents.onVesselChange.Add(OnVesselChange);
            this.FireContextEnterOrLeave(
                InEVA()
            );
        }

        private void OnVesselChange(Vessel vessel)
        {
            LOGGER.LogTrace("=> OnVesselChange : " + vessel.name);
            this.FireContextEnterOrLeave(
                InEVA(vessel)
            );
        }
    }
}
