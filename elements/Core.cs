namespace Cli_bliss.Core;

// Core interfaces
public interface IMsg { }

public interface IModel<TModel>
{
    TModel State { get; }
}

public interface IUpdate<TModel, TMsg> 
    where TMsg : IMsg
{
    TModel Apply(TModel model, TMsg msg);
}

public interface IView<TModel, TMsg> 
    where TMsg : IMsg
{
    TModel Render(TModel model);
}

// Runtime implementation
public class Runtime<TModel, TMsg> 
    where TMsg : IMsg
{
    private TModel _currentState;
    private readonly IUpdate<TModel, TMsg> _update;
    private readonly IView<TModel, TMsg> _view;
    
    public TModel CurrentState => _currentState;
    
    public Runtime(TModel initialState, IUpdate<TModel, TMsg> update, IView<TModel, TMsg> view)
    {
        _currentState = initialState;
        _update = update;
        _view = view;
    }
    
    public void Dispatch(TMsg msg)
    {
        _currentState = _update.Apply(_currentState, msg);
        _view.Render(_currentState);
    }
}

// Component base class
public abstract class Component<TModel, TMsg> 
    where TModel : IModel<TModel>
    where TMsg : IMsg
{
    protected readonly TModel _initialState;
    protected readonly IUpdate<TModel, TMsg> _update;
    protected readonly IView<TModel, TMsg> _view;
    
    protected Component(TModel initialState, IUpdate<TModel, TMsg> update, IView<TModel, TMsg> view)
    {
        _initialState = initialState;
        _update = update;
        _view = view;
    }
    
    protected abstract TMsg? HandleInput();
    protected abstract bool ShouldContinue(TMsg msg, TModel currentState);
    protected abstract void OnExit(TModel finalState);
    protected abstract TMsg CreateInitialMessage();
    
    public TModel Run()
    {
        var runtime = new Runtime<TModel, TMsg>(_initialState, _update, _view);
        
        Console.CursorVisible = false;
        runtime.Dispatch(CreateInitialMessage());
        
        while (true)
        {
            var msg = HandleInput();
            if (msg != null)
            {
                runtime.Dispatch(msg);
                
                if (!ShouldContinue(msg, runtime.CurrentState))
                {
                    OnExit(runtime.CurrentState);
                    return runtime.CurrentState;
                }
            }
        }
    }
}
