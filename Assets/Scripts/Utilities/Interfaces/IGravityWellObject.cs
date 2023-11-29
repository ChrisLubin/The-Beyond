using UnityEngine;

public interface IGravityWellObject
{
    public bool CanBeReParented();
    public Transform transform { get; }
}
