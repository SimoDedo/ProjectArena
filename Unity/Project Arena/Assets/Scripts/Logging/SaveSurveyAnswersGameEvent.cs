using System.Collections.Generic;
using Events;
using Logging.Logging.Survey;

namespace Logging
{
    public class BaseSaveSurveyAnswersGameEvent : GameEventBase<List<JsonAnswer>>
    {
    }

    public sealed class SaveSurveyAnswersGameEvent : ClassSingleton<BaseSaveSurveyAnswersGameEvent>
    {
    }
}