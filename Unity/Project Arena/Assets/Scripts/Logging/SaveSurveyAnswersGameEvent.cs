using System.Collections.Generic;
using Logging.Logging.Survey;
using ScriptableObjectArchitecture;

namespace Logging
{
    public class BaseSaveSurveyAnswersGameEvent : GameEventBase<List<JsonAnswer>> { }

    public sealed class SaveSurveyAnswersGameEvent : ScriptableObjectSingleton<BaseSaveSurveyAnswersGameEvent> { }
}