// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Diagnostics.Tracing
{
    public class EventSource
    {
        protected void WriteEvent(int v)
        {

        }

        protected void WriteEvent(int v, int newState)
        {

        }

        protected void WriteEvent(int v, string newState)
        {

        }

        protected void WriteEvent(int v, string type, string sdp)
        {

        }

        protected void WriteEvent(int v, int width, int height)
        {

        }

        protected void WriteEvent(int v, int bitsPerSample, int channelCount, int frameCount)
        {

        }

        protected void WriteEvent(int v, int id, string label)
        {

        }

        protected unsafe void WriteEventCore(int eventId, int v, EventData* dataDesc)
        {

        }
    }
}
