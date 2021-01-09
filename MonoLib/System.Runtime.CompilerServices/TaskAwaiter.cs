//
// TaskAwaiter.cs
//
// Authors:
//	Marek Safar  <marek.safar@gmail.com>
//
// Copyright (C) 2011 Novell, Inc (http://www.novell.com)
// Copyright (C) 2011 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#if NET_4_5

using System.Threading;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;

namespace System.Runtime.CompilerServices
{
	public struct TaskAwaiter : ICriticalNotifyCompletion
	{
		readonly Task m_task;

		internal TaskAwaiter (Task task)
		{
			this.m_task = task;
		}

		public bool IsCompleted {
			get {
				return m_task.IsCompleted;
			}
		}

        /// <summary>Ends the await on the completed <see cref="System.Threading.Tasks.Task"/>.</summary>
        /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
        /// <exception cref="System.Threading.Tasks.TaskCanceledException">The task was canceled.</exception>
        /// <exception cref="System.Exception">The task completed in a Faulted state.</exception>
        public void GetResult()
        {
            ValidateEnd(m_task);
        }

        /// <summary>
        /// Fast checks for the end of an await operation to determine whether more needs to be done
        /// prior to completing the await.
        /// </summary>
        /// <param name="task">The awaited task.</param>
        internal static void ValidateEnd(Task task)
        {
            HandleNonSuccessAndDebuggerNotification(task);
        }

        /// <summary>
        /// Ensures the task is completed, triggers any necessary debugger breakpoints for completing 
        /// the await on the task, and throws an exception if the task did not complete successfully.
        /// </summary>
        /// <param name="task">The awaited task.</param>
        private static void HandleNonSuccessAndDebuggerNotification(Task task)
        {
            // NOTE: The JIT refuses to inline ValidateEnd when it contains the contents
            // of HandleNonSuccessAndDebuggerNotification, hence the separation.

            // Synchronously wait for the task to complete.  When used by the compiler,
            // the task will already be complete.  This code exists only for direct GetResult use,
            // for cases where the same exception propagation semantics used by "await" are desired,
            // but where for one reason or another synchronous rather than asynchronous waiting is needed.
            if (!task.IsCompleted)
            {
                bool taskCompleted = task.Wait(Timeout.Infinite, default(CancellationToken));
                Contract.Assert(taskCompleted, "With an infinite timeout, the task should have always completed.");
            }

            // And throw an exception if the task is faulted or canceled.
            if (!task.IsRanToCompletion) ThrowForNonSuccess(task);
        }

        /// <summary>Throws an exception to handle a task that completed in a state other than RanToCompletion.</summary>
        private static void ThrowForNonSuccess(Task task)
        {
            throw new Exception("Task must have been completed by now.");
        }

        //      public void GetResult ()
        //{
        //          if (m_task.Status == TaskStatus.WaitingForActivation || m_task.Status == TaskStatus.)

        //          if (!m_task.IsCompleted)
        //          {
        //              m_task.Wait(Timeout.Infinite, default(CancellationToken));
        //          }

        //          if (m_task.Status != TaskStatus.RanToCompletion) {
        //		// Merge current and dispatched stack traces if there is any
        //		ExceptionDispatchInfo.Capture (HandleUnexpectedTaskResult (m_task)).Throw ();
        //	}
        //}

        internal static Exception HandleUnexpectedTaskResult (Task task)
		{
			switch (task.Status) {
			case TaskStatus.Canceled:
				return new TaskCanceledException (task);
			case TaskStatus.Faulted:
				return task.Exception.InnerException;
			default:
				return new InvalidOperationException ("The task has not finished yet");
			}
		}

        internal static void HandleOnCompleted (Task task, Action continuation, bool continueOnSourceContext, bool manageContext)
		{
			if (continueOnSourceContext && SynchronizationContext.Current != null) {
				task.ContinueWith (new SynchronizationContextContinuation (continuation, SynchronizationContext.Current));
			} else {
				task.ContinueWith (new AwaiterActionContinuation(continuation));
			}
		}

		public void OnCompleted (Action continuation)
		{
			if (continuation == null)
				throw new ArgumentNullException ("continuation");

			HandleOnCompleted (m_task, continuation, true, true);
		}
		
		public void UnsafeOnCompleted (Action continuation)
		{
			if (continuation == null)
				throw new ArgumentNullException ("continuation");

			HandleOnCompleted (m_task, continuation, true, false);
		}
	}
}

#endif
