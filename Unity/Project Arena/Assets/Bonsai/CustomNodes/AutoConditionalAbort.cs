using Bonsai.Core;
using Bonsai.Utility;
using UnityEngine;

namespace Bonsai.CustomNodes
{
    public abstract class AutoConditionalAbort: ConditionalAbort
    {
        private readonly Timer timer = new()
        {
            interval = 0.0001f
        };

        public override void OnStart()
        {
            base.OnStart();
            timer.Start();
            timer.OnTimeout += TimerOnOnTimeout;
        }

        private void TimerOnOnTimeout()
        {
            timer.interval = 0.001f;
            timer.Start();
            Evaluate();
        }

        protected override void OnObserverBegin()
        {
            base.OnObserverBegin();
            timer.interval = 0.0001f;
            timer.Start();
            Tree.AddTimer(timer);
        }

        protected override void OnObserverEnd()
        {
            base.OnObserverEnd();
            Tree.RemoveTimer(timer);
        }

    }
}