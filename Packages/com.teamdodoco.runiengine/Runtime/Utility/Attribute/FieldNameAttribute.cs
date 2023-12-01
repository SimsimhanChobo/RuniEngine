#nullable enable
using System;
using UnityEngine;

namespace RuniEngine
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FieldNameAttribute : PropertyAttribute
    {
        public FieldNameAttribute(string name) => this.name = name;

        public string name { get; } = "";
    }
}