using System.Collections.Generic;
using Logging.Logging.Survey;
using ScriptableObjectArchitecture;

namespace Logging
{
    public class BaseSaveSurveyQuestionsGameEvent : GameEventBase<List<JsonQuestion>>
    {
    }

    public sealed class SaveSurveyQuestionsGameEvent : ScriptableObjectSingleton<BaseSaveSurveyQuestionsGameEvent>
    {
    }
}