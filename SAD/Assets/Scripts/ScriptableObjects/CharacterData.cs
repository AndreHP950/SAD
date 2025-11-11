using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Character Selector/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("General Data")]
    public string characterName;
    public GameObject characterPrefab;
    public Sprite characterIcon;

    [Header("Position Data")]
    public Vector3 spawnPosition;
    public Quaternion spawnRotation;
}
