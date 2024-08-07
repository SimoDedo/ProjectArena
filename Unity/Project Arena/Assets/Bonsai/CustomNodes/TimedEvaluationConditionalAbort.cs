using Bonsai.Core;
using Timer = Bonsai.Utility.Timer;

namespace Bonsai.CustomNodes
{
    public abstract class TimedEvaluationConditionalAbort: ConditionalAbort
    {
        private readonly Timer timer;

        protected TimedEvaluationConditionalAbort(float timeOut = 0.0001f)
        {
            timer = new Timer
            {
                interval = timeOut,
                AutoRestart = true
            };
        }


        private void TimerOnOnTimeout()
        {
            Evaluate();
        }

        protected override void OnObserverBegin()
        {
            base.OnObserverBegin();
            timer.Start();
            timer.OnTimeout += TimerOnOnTimeout;
            Tree.AddTimer(timer);
        }

        protected override void OnObserverEnd()
        {
            base.OnObserverEnd();
            Tree.RemoveTimer(timer);
            timer.OnTimeout -= TimerOnOnTimeout;
        }

    }
}