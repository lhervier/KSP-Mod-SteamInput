using UnityEngine.SceneManagement;
using com.github.lhervier.ksp.mod.steaminput.model;

namespace com.github.lhervier.ksp.mod.steaminput 
{
    // <summary>
    //  This class is a context daemon that detects when the game is in the mission control
    // </summary>
    public class MissionControlCtxDaemon : BaseContextDaemon
    {
        private static readonly ModLogger LOGGER = new ModLogger("MissionControlCtxDaemon");

        public override EActionGroup CorrespondingActionGroup()
        {
            return EActionGroup.MenuControls;
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
            if( scene.name.ToUpper() != "SPACECENTER" ) return;

            GameEvents.onGUIMissionControlSpawn.Add(OnGUIMissionControlSpawn);
            GameEvents.onGUIMissionControlDespawn.Add(OnGUIMissionControlDespawn);
        }

        protected void OnSceneUnloaded(Scene scene)
        {
            LOGGER.LogDebug("OnSceneUnloaded : " + scene.name);
            if( scene.name.ToUpper() != "SPACECENTER" ) return;
            
            GameEvents.onGUIMissionControlSpawn.Remove(OnGUIMissionControlSpawn);
            GameEvents.onGUIMissionControlDespawn.Remove(OnGUIMissionControlDespawn);
            this.FireContextEnterOrLeave(false);
        }

        protected void OnGUIMissionControlSpawn()
        {
            LOGGER.LogTrace("=> OnGUIMissionControlSpawn");
            this.FireContextEnterOrLeave(true);
        }

        protected void OnGUIMissionControlDespawn()
        {
            LOGGER.LogTrace("=> OnGUIMissionControlDespawn");
            this.FireContextEnterOrLeave(false);
        }       
    }
}