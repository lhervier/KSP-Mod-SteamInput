using UnityEngine.SceneManagement;
using com.github.lhervier.ksp.mod.steaminput.model;

namespace com.github.lhervier.ksp.mod.steaminput 
{
    // <summary>
    //  This class is a context daemon that detects when the game 
    //  is in the docking mode, with the map displayed
    // </summary>
    public class DockingMapCtxDaemon : BaseContextDaemon
    {
        private static readonly ModLogger LOGGER = new ModLogger("DockingMapCtxDaemon");
        
        public override EActionGroup CorrespondingActionGroup()
        {
            return EActionGroup.MapControls;
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
        }

        protected void OnSceneUnloaded(Scene scene)
        {
            LOGGER.LogDebug("OnSceneUnloaded : " + scene.name);
            if( scene.name.ToUpper() != "PFLIGHT4" ) {
                return;
            }

            GameEvents.OnFlightUIModeChanged.Remove(OnFlightUIModeChanged);
            GameEvents.onVesselChange.Remove(OnVesselChange);

            GameEvents.OnMapEntered.Remove(OnMapEntered);
            GameEvents.OnMapExited.Remove(OnMapExited);
            
            this.FireContextEnterOrLeave(false);
        }

        // ============================================================

        private void OnMapEntered()
        {
            LOGGER.LogTrace("=> OnMapEntered");
            
            GameEvents.OnFlightUIModeChanged.Add(OnFlightUIModeChanged);
            GameEvents.onVesselChange.Add(OnVesselChange);

            this.FireContextEnterOrLeave(
                InDockingMode()
            );
        }

        private void OnMapExited()
        {
            LOGGER.LogTrace("=> OnMapExited");
            
            GameEvents.OnFlightUIModeChanged.Remove(OnFlightUIModeChanged);
            GameEvents.onVesselChange.Remove(OnVesselChange);

            FireContextEnterOrLeave(false);
        }

        private void OnFlightUIModeChanged(FlightUIMode mode)
        {
            LOGGER.LogTrace("=> OnFlightUIModeChanged : " + mode.ToString());
            this.FireContextEnterOrLeave(
                InDockingMode(mode)
            );
        }

        private void OnVesselChange(Vessel vessel)
        {
            LOGGER.LogTrace("=> OnVesselChange : " + vessel.name);
            this.FireContextEnterOrLeave(
                InDockingMode()
            );
        }
    }
}
