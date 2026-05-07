using System;
using UnityEngine;

namespace WhoWiredThis.Util
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class RequireInterfaceAttribute : PropertyAttribute
    {
        public Type InterfaceType { get; }

        public RequireInterfaceAttribute(Type interfaceType)
        {
            InterfaceType = interfaceType;
        }
    }
}
