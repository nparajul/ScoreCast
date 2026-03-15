using System.Runtime.CompilerServices;

namespace ScoreCast.Shared.Exceptions;

public class ScoreCastException : Exception
{
    public ScoreCastException([CallerMemberName] string? memberFunction = null)
        : base($"Something went wrong. Ref: {memberFunction}") { }

    public ScoreCastException(string message, [CallerMemberName] string? memberFunction = null)
        : base($"{message} in {memberFunction}") { }

    public ScoreCastException(string messageFormat, params object[] args)
        : base(string.Format(messageFormat, args)) { }

    public ScoreCastException(Exception exp, [CallerMemberName] string? memberFunction = null)
        : base($"{exp.GetBaseException().Message} in {memberFunction}", exp.InnerException) { }

    public ScoreCastException(string message, Exception exp, [CallerMemberName] string? memberFunction = null)
        : base($"{message}: {exp.GetBaseException().Message} in {memberFunction}", exp.InnerException) { }
}
