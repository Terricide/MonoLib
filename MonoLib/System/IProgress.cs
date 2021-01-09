using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    //
    // Summary:
    //     Defines a provider for progress updates.
    //
    // Type parameters:
    //   T:
    //     The type of progress update value.
    public interface IProgress<in T>
    {
        //
        // Summary:
        //     Reports a progress update.
        //
        // Parameters:
        //   value:
        //     The value of the updated progress.
        void Report(T value);
    }
}
