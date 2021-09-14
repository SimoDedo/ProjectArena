using System.Collections.Generic;
using JsonObjects.Logging.Survey;
using ScriptableObjectArchitecture;

namespace AssemblyLogging
{
    public class BaseSaveSurveyAnswersGameEvent : GameEventBase<List<JsonAnswer>>
    {
    }

    public sealed class SaveSurveyAnswersGameEvent : ScriptableObjectSingleton<BaseSaveSurveyAnswersGameEvent>
    {
    }
}