// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;

namespace System.Diagnostics.Tracing
{
    public class EventAttribute : Attribute
    {
        public EventAttribute(int num)
        {

        }
        public EventLevel Level { get; set; }
        public EventKeywords Keywords { get; set; }
    }
}
