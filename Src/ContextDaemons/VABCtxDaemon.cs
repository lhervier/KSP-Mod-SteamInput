using UnityEngine.SceneManagement;
using com.github.lhervier.ksp.mod.steaminput.model;

namespace com.github.lhervier.ksp.mod.steaminput 
{
    // <summary>
    //  This class is a context daemon that detects when the game is in the VAB
    // </summary>
    public class VABCtxDaemon : BaseContextDaemon
    {
        private static readonly ModLogger LOGGER = new ModLogger("VABCtxDaemon");
        
        public override EActionGroup CorrespondingActionGroup()
        {
            return EActionGroup.EditorControls;
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
            if( !scene.name.ToUpper().StartsWith("VAB") ) {
                return;
            }
            this.FireContextEnterOrLeave(true);
        }

        protected void OnSceneUnloaded(Scene scene)
        {
            LOGGER.LogDebug("OnSceneUnloaded : " + scene.name);
            if( !scene.name.ToUpper().StartsWith("VAB") ) {
                return;
            }
            this.FireContextEnterOrLeave(false);
        }
    }
}