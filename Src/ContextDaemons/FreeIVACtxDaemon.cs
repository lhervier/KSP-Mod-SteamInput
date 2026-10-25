using System;
using System.Reflection;
using UnityEngine.SceneManagement;
using com.github.lhervier.ksp.mod.steaminput.model;

namespace com.github.lhervier.ksp.mod.steaminput
{
    // <summary>
    //  This class is a context daemon that detects when a kerbal moves inside the vessel with FreeIVA
    //  (in IVA, unbuckled from its seat). A seated kerbal stays in the flight context, as in stock.
    //  The daemon does nothing when FreeIVA is not installed.
    // </summary>
    public class FreeIVACtxDaemon : BaseContextDaemon
    {
        private static readonly ModLogger LOGGER = new ModLogger("FreeIVACtxDaemon");

        private PropertyInfo instanceProperty;
        private FieldInfo buckledField;
        private bool initialized = false;
        private bool inFlightScene = false;

        public override EActionGroup CorrespondingActionGroup()
        {
            return EActionGroup.EVAControls;
        }

        public void Start()
        {
            LOGGER.LogInfo("Start");

            // KSP loads mod assemblies itself: look the type up in its loader rather than with Type.GetType.
            Type kerbalIvaAddonType = null;
            foreach( AssemblyLoader.LoadedAssembly loaded in AssemblyLoader.loadedAssemblies ) {
                if( loaded.name != "FreeIva" ) continue;
                kerbalIvaAddonType = loaded.assembly.GetType("FreeIva.KerbalIvaAddon");
                break;
            }
            if( kerbalIvaAddonType == null ) {
                LOGGER.LogInfo("=> FreeIVA mod not found");
                return;
            }

            this.instanceProperty = kerbalIvaAddonType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if( this.instanceProperty == null ) {
                LOGGER.LogError("=> Instance property not found. FreeIVA mod has probably evolved...");
                return;
            }

            this.buckledField = kerbalIvaAddonType.GetField("buckled", BindingFlags.Public | BindingFlags.Instance);
            if( this.buckledField == null ) {
                LOGGER.LogError("=> buckled field not found. FreeIVA mod has probably evolved...");
                return;
            }

            LOGGER.LogInfo("=> FreeIVA mod found");
            this.initialized = true;

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public void OnDestroy()
        {
            LOGGER.LogInfo("OnDestroy");
            if( !this.initialized ) return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            this.RemoveFlightEvents();
        }

        protected void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LOGGER.LogDebug("OnSceneLoaded : " + scene.name);
            if( scene.name.ToUpper() != "PFLIGHT4" ) return;

            GameEvents.OnFlightUIModeChanged.Add(OnFlightUIModeChanged);
            GameEvents.onVesselChange.Add(OnVesselChange);
            this.inFlightScene = true;
        }

        protected void OnSceneUnloaded(Scene scene)
        {
            LOGGER.LogDebug("OnSceneUnloaded : " + scene.name);
            if( scene.name.ToUpper() != "PFLIGHT4" ) return;

            this.RemoveFlightEvents();
            this.inFlightScene = false;
            this.FireContextEnterOrLeave(false);
        }

        private void RemoveFlightEvents()
        {
            GameEvents.OnFlightUIModeChanged.Remove(OnFlightUIModeChanged);
            GameEvents.onVesselChange.Remove(OnVesselChange);
        }

        // ============================================================

        // The last context entered wins: when the flight daemons re-enter their context (UI mode or
        // vessel change), leave ours so that the next Update enters it again, on top of theirs.
        private void OnFlightUIModeChanged(FlightUIMode mode)
        {
            LOGGER.LogTrace("=> OnFlightUIModeChanged : " + mode);
            this.FireContextEnterOrLeave(false);
        }

        private void OnVesselChange(Vessel vessel)
        {
            LOGGER.LogTrace("=> OnVesselChange : " + vessel.name);
            this.FireContextEnterOrLeave(false);
        }

        // FreeIVA raises no event when the kerbal unbuckles or sits back: its state is polled every frame.
        public void Update()
        {
            if( !this.initialized || !this.inFlightScene ) return;
            this.FireContextEnterOrLeave(this.InFreeIva());
        }

        /// <summary>
        /// Whether a kerbal is moving inside the vessel with FreeIVA: IVA view, out of the map, unbuckled.
        /// </summary>
        private bool InFreeIva()
        {
            // The map view gets its own action set: leaving the context there lets ours be entered
            // again after the flight one, when the map closes.
            if( MapView.MapIsEnabled || !this.InIVA() ) return false;

            object instance = this.instanceProperty.GetValue(null, null);
            if( instance == null ) return false;
            return !(bool) this.buckledField.GetValue(instance);
        }
    }
}
