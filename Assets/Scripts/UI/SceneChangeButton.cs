using UnityEngine;

/// <summary>
/// Put on a UI Button (or any object) whose OnClick should trigger a scene
/// change via SceneLoader. Wire OnClick to this component's ChangeScene(),
/// not directly to a _SCENE_LOADER prefab copy: those get Destroy()'d as
/// duplicates whenever a SceneLoader has already persisted in from an
/// earlier scene, and Unity silently skips OnClick calls whose target has
/// been destroyed, which breaks the button with no error at all. This
/// component lives on a plain scene object that's never duplicated or
/// destroyed, and resolves SceneLoader.Instance at click-time instead.
/// </summary>
public class SceneChangeButton : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void ChangeScene()
    {
        SceneLoader.Instance.ChangeScene(sceneName);
    }
}
