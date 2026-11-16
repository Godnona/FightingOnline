using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CharacterSelectButton : MonoBehaviour
{
    [SerializeField] private CharacterSelectManager characterSelectManager;
    [SerializeField] private int characterId;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (characterSelectManager == null)
        {
            characterSelectManager = FindFirstObjectByType<CharacterSelectManager>();
        }
    }

    private void OnEnable()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        button.onClick.AddListener(SelectCharacter);
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(SelectCharacter);
        }
    }

    public void SelectCharacter()
    {
        if (characterSelectManager == null)
        {
            characterSelectManager = FindFirstObjectByType<CharacterSelectManager>();
        }

        if (characterSelectManager == null)
        {
            Debug.LogError("CharacterSelectButton cannot find a CharacterSelectManager in this scene.");
            return;
        }

        characterSelectManager.SelectCharacter(characterId);
    }
}
