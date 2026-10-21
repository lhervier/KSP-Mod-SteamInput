namespace com.github.lhervier.ksp.mod.steaminput.model
{
    /// <summary>
    /// The action sets the plugin activates: the six sets that Squad's own Steam controller plugin
    /// (KSPSteamCtrlr, class SteamController.KSPSteamController) declares to Steam. Their names are those
    /// of Steam's action manifest for KSP, matched exactly (case included) by GetActionSetHandle.
    /// </summary>
    public enum EActionGroup {
        None,
        MenuControls,
        FlightControls,
        DockingControls,
        EditorControls,
        MapControls,
        EVAControls
    }
}
