# Local dependency drop folder

Place the required game and modding assemblies in this folder before building:

- `0Harmony.dll`
- `BepInEx.dll`
- `Newtonsoft.Json.dll`
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.IMGUIModule.dll`
- `UnityEngine.TextRenderingModule.dll`
- `Assembly-CSharp.dll`

Suggested sources:

- `0Harmony.dll`: from the `HarmonyX` package (`lib/net35/0Harmony.dll`)
- `BepInEx.dll`: from your ETG `BepInEx/core` directory
- `Newtonsoft.Json.dll`: from the Newtonsoft.Json package or an existing compatible BepInEx plugin installation
- `UnityEngine.dll`: from `Enter the Gungeon\EtG_Data\Managed`
- `UnityEngine.CoreModule.dll`: from `Enter the Gungeon\EtG_Data\Managed`
- `UnityEngine.IMGUIModule.dll`: from `Enter the Gungeon\EtG_Data\Managed`
- `UnityEngine.TextRenderingModule.dll`: from `Enter the Gungeon\EtG_Data\Managed`
- `Assembly-CSharp.dll`: from `Enter the Gungeon\EtG_Data\Managed`

Do not commit these DLLs to the repository.
