using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Character Selector/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("General Data")]
    public string characterName;
    public GameObject characterPrefab;
    public Sprite characterIcon;
    public Animal animalType;

    [Header("Position Data")]
    public Vector3 spawnPosition;
    public Quaternion spawnRotation;
    public StartArea startArea;
    public enum StartArea { City = 1, Suburbs = 2, Pier = 3 }
    public enum Animal { Cat = 0, Dog = 1 }
}
