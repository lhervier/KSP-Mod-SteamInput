# KSP-Mod-SteamInput — notes de développement

Le mod remplace le plugin Steam controller de Squad (`KSPSteamCtrlr.dll`, classe
`SteamController.KSPSteamController`) et garde ses **6 action sets** : il ne fait que choisir le bon
selon la situation. Pas de cheatsheet, pas de fenêtre, pas de manifeste `controller_config` : c'est la
différence avec **KSP-Mod-SteamInputExtended** (l'ancien mod, 12 sets, sa cheatsheet), dont il est copié.
Les deux ne doivent pas tourner ensemble : le mod refuse de démarrer si `KSPModSteamInputExtended` est
chargé.

La disposition du Steam Controller v2 et la raison de chaque choix sont dans le dépôt de la config :
`KSP-Util-SteamInputConfig`, `CLAUDE-disposition-squad.md`.

## Le code

- `KSPModSteamInput` (le `MonoBehaviour` principal) : désactive le plugin de Squad, attend Steam, crée
  les démons.
- `ActionGroupDaemon` : charge tous les `BaseContextDaemon` par réflexion ; **le dernier contexte entré
  gagne**, `MenuControls` quand aucun n'est actif ; applique le set 10 images plus tard.
- `GamepadDaemon` : connexion de la manette, handles des sets (`GetActionSetHandle`, noms exacts de
  `EActionGroup`), activation. En cas d'échec des handles, il réessaie chaque seconde.
- `ContextDaemons\` : un détecteur par situation, chacun rattaché à un set. Plusieurs situations partagent
  un set (les trois cartes et la station de suivi → `MapControls`, Mission Builder → `EditorControls`) :
  les touches sont les mêmes, KSP les interprète selon son mode.
- `ModSettings` : `GameData\KSPModSteamInput\settings.cfg` (niveau de log).
- `shared\` : `ModLogger` et `Constants`, copiés de KSP-Shared (pas de sous-module).

## Décisions

- **FreeIVA et la construction en AEV n'ont plus de détecteur** : ils tombent sur les sets Vol et AEV,
  les mêmes touches. `EVACtxDaemon` reste actif pendant la construction. (L'ancien détecteur FreeIVA ne
  voyait l'entrée en IVA que sur un changement de mode d'interface ou de vaisseau.)
- **Pas de détecteur de pause** en station de suivi, éditeur, Mission Builder : leur set reste actif en
  pause (☰ = Échap et gâchettes = clics y suffisent).

Les connaissances KSP transverses sont dans le [`CLAUDE.md` parent](../CLAUDE.md) de `kspmod\`.
