using System;
using System.Threading.Tasks;

namespace PAN.Mediator.Abstractions
{
    /// <summary>Represents a void return value for requests that don't produce a meaningful response.</summary>
    public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
    {
        public static readonly Unit Value = new Unit();
        public static readonly Task<Unit> Task = System.Threading.Tasks.Task.FromResult(Value);

        public bool Equals(Unit other) => true;

        public override bool Equals(object? obj) => obj is Unit;

        public override int GetHashCode() => 0;

        public int CompareTo(Unit other) => 0;

        int IComparable.CompareTo(object? obj) => 0;

        public override string ToString() => "()";

        public static bool operator ==(Unit left, Unit right) => true;

        public static bool operator !=(Unit left, Unit right) => false;
    }
}
