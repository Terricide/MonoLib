// SharedMemory (File: SharedMemory\MemoryMappedFile.cs)
// Copyright (c) 2014 Justin Stenning
// http://spazzarama.com
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// The SharedMemory library is inspired by the following Code Project article:
//   "Fast IPC Communication Using Shared Memory and InterlockedCompareExchange"
//   http://www.codeproject.com/Articles/14740/Fast-IPC-Communication-Using-Shared-Memory-and-Int
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;

namespace System.IO.MemoryMappedFiles
{
#if !NET40Plus

    /// <summary>
    /// <para>Very limited .NET 3.5 implementation of a managed wrapper around memory-mapped files to reflect the .NET 4 API.</para>
    /// <para>Only those methods and features necessary for the SharedMemory library have been implemented.</para>
    /// </summary>
#if NETFULL
    [PermissionSet(SecurityAction.LinkDemand)]
#endif
    public abstract class ObjectSecurity<T> : NativeObjectSecurity where T : struct
    {
        #region Constructors

        protected ObjectSecurity(bool isContainer, ResourceType resourceType)
            : base(isContainer, resourceType, null, null) { }

        protected ObjectSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections)
            : base(isContainer, resourceType, name, includeSections, null, null) { }

        protected ObjectSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections, ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext)
            : base(isContainer, resourceType, name, includeSections, exceptionFromErrorCode, exceptionContext) { }

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected ObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle safeHandle, AccessControlSections includeSections)
            : base(isContainer, resourceType, safeHandle, includeSections, null, null) { }

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected ObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle safeHandle, AccessControlSections includeSections, ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext)
            : base(isContainer, resourceType, safeHandle, includeSections, exceptionFromErrorCode, exceptionContext) { }

        #endregion
        #region Factories

        public override AccessRule AccessRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type)
        {
            return new AccessRule<T>(
                identityReference,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                type);
        }

        public override AuditRule AuditRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags)
        {
            return new AuditRule<T>(
                identityReference,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                flags);
        }

        #endregion
        #region Private Methods

        private AccessControlSections GetAccessControlSectionsFromChanges()
        {
            AccessControlSections persistRules = AccessControlSections.None;
            if (AccessRulesModified)
            {
                persistRules = AccessControlSections.Access;
            }
            if (AuditRulesModified)
            {
                persistRules |= AccessControlSections.Audit;
            }
            if (OwnerModified)
            {
                persistRules |= AccessControlSections.Owner;
            }
            if (GroupModified)
            {
                persistRules |= AccessControlSections.Group;
            }
            return persistRules;
        }

        #endregion
        #region Protected Methods

        // Use this in your own Persist after you have demanded any appropriate CAS permissions.
        // Note that you will want your version to be internal and use a specialized Safe Handle. 
        // <SecurityKernel Critical="True" Ring="0">
        // <Asserts Name="Declarative: [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]" />
        // </SecurityKernel>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        protected internal void Persist(SafeHandle handle)
        {
            WriteLock();

            try
            {
                AccessControlSections persistRules = GetAccessControlSectionsFromChanges();
                base.Persist(handle, persistRules);
                OwnerModified = GroupModified = AuditRulesModified = AccessRulesModified = false;
            }
            finally
            {
                WriteUnlock();
            }
        }

        // Use this in your own Persist after you have demanded any appropriate CAS permissions.
        // Note that you will want your version to be internal. 
        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        protected internal void Persist(String name)
        {
            WriteLock();

            try
            {
                AccessControlSections persistRules = GetAccessControlSectionsFromChanges();
                base.Persist(name, persistRules);
                OwnerModified = GroupModified = AuditRulesModified = AccessRulesModified = false;
            }
            finally
            {
                WriteUnlock();
            }
        }

        #endregion
        #region Public Methods

        // Override these if you need to do some custom bit remapping to hide any 
        // complexity from the user. 
        public virtual void AddAccessRule(AccessRule<T> rule)
        {
            base.AddAccessRule(rule);
        }

        public virtual void SetAccessRule(AccessRule<T> rule)
        {
            base.SetAccessRule(rule);
        }

        public virtual void ResetAccessRule(AccessRule<T> rule)
        {
            base.ResetAccessRule(rule);
        }

        public virtual bool RemoveAccessRule(AccessRule<T> rule)
        {
            return base.RemoveAccessRule(rule);
        }

        public virtual void RemoveAccessRuleAll(AccessRule<T> rule)
        {
            base.RemoveAccessRuleAll(rule);
        }

        public virtual void RemoveAccessRuleSpecific(AccessRule<T> rule)
        {
            base.RemoveAccessRuleSpecific(rule);
        }

        public virtual void AddAuditRule(AuditRule<T> rule)
        {
            base.AddAuditRule(rule);
        }

        public virtual void SetAuditRule(AuditRule<T> rule)
        {
            base.SetAuditRule(rule);
        }

        public virtual bool RemoveAuditRule(AuditRule<T> rule)
        {
            return base.RemoveAuditRule(rule);
        }

        public virtual void RemoveAuditRuleAll(AuditRule<T> rule)
        {
            base.RemoveAuditRuleAll(rule);
        }

        public virtual void RemoveAuditRuleSpecific(AuditRule<T> rule)
        {
            base.RemoveAuditRuleSpecific(rule);
        }

        #endregion
        #region some overrides

        public override Type AccessRightType
        {
            get { return typeof(T); }
        }

        public override Type AccessRuleType
        {
            get { return typeof(AccessRule<T>); }
        }

        public override Type AuditRuleType
        {
            get { return typeof(AuditRule<T>); }
        }
        #endregion
    }
#endif
}