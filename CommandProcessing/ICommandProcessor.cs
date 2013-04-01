namespace CommandProcessing
{
    using System.Diagnostics.CodeAnalysis;

    public interface ICommandProcessor
    {
        ProcessorConfiguration Configuration { get; }
        
        TResult Process<TCommand, TResult>(TCommand command, HandlerRequest currentRequest) where TCommand : ICommand;

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Using", Justification = "Nom de m�thode volontairement proche du mot cl� using.")]
        TService Using<TService>() where TService : class;
    }
}