using System.Collections.Generic;
using Events;
using Logging.Logging.Survey;

namespace Logging
{
    public class BaseSaveSurveyQuestionsGameEvent : GameEventBase<List<JsonQuestion>>
    {
    }

    public sealed class SaveSurveyQuestionsGameEvent : ClassSingleton<BaseSaveSurveyQuestionsGameEvent>
    {
    }
}