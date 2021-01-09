// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace System.Diagnostics.Tracing
{
    public class EventSourceAttribute : Attribute
    {
        public string Name { get; set; }
        public string Guid { get; set; }
    }
}
