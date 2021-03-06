﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Reflection
{

    public abstract partial class TypeInfo : Type, IReflectableType
    {
        public virtual Type AsType() => this;

        public virtual Type[] GenericTypeParameters => IsGenericTypeDefinition ? GetGenericArguments() : Type.EmptyTypes;

        public virtual EventInfo GetDeclaredEvent(string name) => GetEvent(name, TypeInfo.DeclaredOnlyLookup);
        public virtual FieldInfo GetDeclaredField(string name) => GetField(name, TypeInfo.DeclaredOnlyLookup);
        public virtual MethodInfo GetDeclaredMethod(string name) => GetMethod(name, TypeInfo.DeclaredOnlyLookup);
        public virtual TypeInfo GetDeclaredNestedType(string name) => GetNestedType(name, TypeInfo.DeclaredOnlyLookup)?.GetTypeInfo();
        public virtual PropertyInfo GetDeclaredProperty(string name) => GetProperty(name, TypeInfo.DeclaredOnlyLookup);

        public virtual IEnumerable<MethodInfo> GetDeclaredMethods(string name)
        {
            foreach (MethodInfo method in GetMethods(TypeInfo.DeclaredOnlyLookup))
            {
                if (method.Name == name)
                    yield return method;
            }
        }

        public virtual IEnumerable<ConstructorInfo> DeclaredConstructors => GetConstructors(TypeInfo.DeclaredOnlyLookup);
        public virtual IEnumerable<EventInfo> DeclaredEvents => GetEvents(TypeInfo.DeclaredOnlyLookup);
        public virtual IEnumerable<FieldInfo> DeclaredFields => GetFields(TypeInfo.DeclaredOnlyLookup);
        public virtual IEnumerable<MemberInfo> DeclaredMembers => GetMembers(TypeInfo.DeclaredOnlyLookup);
        public virtual IEnumerable<MethodInfo> DeclaredMethods => GetMethods(TypeInfo.DeclaredOnlyLookup);
        public virtual IEnumerable<System.Reflection.TypeInfo> DeclaredNestedTypes
        {
            get
            {
                foreach (Type t in GetNestedTypes(TypeInfo.DeclaredOnlyLookup))
                {
                    yield return t.GetTypeInfo();
                }
            }
        }
        public virtual IEnumerable<PropertyInfo> DeclaredProperties => GetProperties(TypeInfo.DeclaredOnlyLookup);

        public virtual IEnumerable<Type> ImplementedInterfaces => GetInterfaces();

        internal static string GetRankString(int rank)
        {
            if (rank <= 0)
                throw new IndexOutOfRangeException();

            return rank == 1 ?
                "[*]" :
                "[" + new string(',', rank - 1) + "]";
        }

        private const BindingFlags DeclaredOnlyLookup = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    }

    //all today's runtime Type derivations derive now from TypeInfo
    //we make TypeInfo implement IRCT - simplifies work
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public abstract partial class TypeInfo : Type, IReflectableType
    {
        [FriendAccessAllowed]
        internal TypeInfo() { }

        TypeInfo IReflectableType.GetTypeInfo()
        {
            return this;
        }

        //a re-implementation of ISAF from Type, skipping the use of UnderlyingType
        [Pure]
        public virtual bool IsAssignableFrom(TypeInfo typeInfo)
        {
            if (typeInfo == null)
                return false;

            if (this == typeInfo)
                return true;

            // If c is a subclass of this class, then c can be cast to this type.
            if (typeInfo.IsSubclassOf(this))
                return true;

            if (this.IsInterface)
            {
                return typeInfo.ImplementInterface(this);
            }
            else if (IsGenericParameter)
            {
                Type[] constraints = GetGenericParameterConstraints();
                for (int i = 0; i < constraints.Length; i++)
                    if (!constraints[i].IsAssignableFrom(typeInfo))
                        return false;

                return true;
            }

            return false;
        }
        #region moved over from Type
        // Fields

        #endregion

    }
}
