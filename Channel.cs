using UnityEngine;
using System;

[Serializable]
public partial struct Channel : IEquatable<Channel>
{
    public uint Value;

    public Channel(uint value) => Value = value;

    public static implicit operator uint(Channel c) => c.Value;
    public static implicit operator Channel(uint v) => new Channel(v);

    public static Channel operator |(Channel a, Channel b) => new Channel(a.Value | b.Value);
    public static Channel operator &(Channel a, Channel b) => new Channel(a.Value & b.Value);
    public static Channel operator ^(Channel a, Channel b) => new Channel(a.Value ^ b.Value);
    public static Channel operator ~(Channel a) => new Channel(~a.Value);

    public bool Equals(Channel other) => Value == other.Value;
    public override bool Equals(object obj) => obj is Channel other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
}
