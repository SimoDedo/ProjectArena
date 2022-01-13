using System.Collections.Generic;
using JsonObjects.Logging.Survey;
using ScriptableObjectArchitecture;

namespace AssemblyLogging
{
    public class BaseSaveSurveyQuestionsGameEvent : GameEventBase<List<JsonQuestion>> { }

    public sealed class SaveSurveyQuestionsGameEvent : ScriptableObjectSingleton<BaseSaveSurveyQuestionsGameEvent> { }
}