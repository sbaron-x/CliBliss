namespace Cli_bliss.Elements;
using Cli_bliss.Core;

public enum SelectorMsgType : byte
{
    Up,
    Down,
    Left,
    Right,
    Select,
    Confirm,
    Cancel
}

public readonly struct SelectorMsg : IMsg
{
    public SelectorMsgType Type { get; }
    
    public SelectorMsg(SelectorMsgType type)
    {
        Type = type;
    }

    public static implicit operator SelectorMsg(SelectorMsgType type) => new(type);
}

public class SelectorModel<T> : IModel<SelectorModel<T>>
{
    public IReadOnlyList<T> Choices { get; }
    public IReadOnlyList<T> SelectedItems { get; }
    public int CurrentIndex { get; }
    public string Title { get; }
    public int ItemPerColumn { get; } = 4;
    public int ColumnWidth { get; } = 20; // Don't know what this means
    public bool IsCompleted { get; }
    public bool WasCancelled { get; } // Don't knwo what this is 
    
    public SelectorModel(
        IEnumerable<T> choices,
        IEnumerable<T> selectedItems,
        int currentIndex,
        string title,
        bool isCompleted = false,
        bool wasCancelled = false)
    {
        Choices = choices?.ToList().AsReadOnly() ?? new List<T>().AsReadOnly();
        SelectedItems = selectedItems?.ToList().AsReadOnly() ?? new List<T>().AsReadOnly();
        CurrentIndex = ValidateIndex(currentIndex, Choices.Count);
        Title = title ?? string.Empty;
        IsCompleted = isCompleted;
        WasCancelled = wasCancelled;
    }

    private static int ValidateIndex(int index, int count)
    {
        if (count == 0) return 0;
        if (index < 0) return 0;
        if (index >= count) return count - 1;
        return index;
    }

    public SelectorModel<T> With(
        IReadOnlyList<T>? choices = null,
        IReadOnlyList<T>? selectedItems = null,
        int? currentIndex = null,
        string? title = null,
        bool? isCompleted = null,
        bool? wasCancelled = null)
    {
        return new SelectorModel<T>(
            choices ?? Choices,
            selectedItems ?? SelectedItems,
            currentIndex ?? CurrentIndex,
            title ?? Title,
            isCompleted ?? IsCompleted,
            wasCancelled ?? WasCancelled
        );
    }

    public SelectorModel<T> State => this;
}

public class SelectorUpdate<T> : IUpdate<SelectorModel<T>, SelectorMsg>
{
    public SelectorModel<T> Apply(SelectorModel<T> model, SelectorMsg msg)
    {
        return msg.Type switch
        {
            SelectorMsgType.Up => HandleUpMovement(model),
            SelectorMsgType.Down => HandleDownMovement(model),
            SelectorMsgType.Left => HandleLeftMovement(model),
            SelectorMsgType.Right => HandleRightMovement(model),
            SelectorMsgType.Select => HandleSelection(model),
            SelectorMsgType.Cancel => model.With(wasCancelled: true, isCompleted: true),
            SelectorMsgType.Confirm => model.With(isCompleted: true),
            _ => model
        };
    }

    private static SelectorModel<T> HandleUpMovement(SelectorModel<T> model)
    {
        var (row, col) = GetPositionFromIndex(model.CurrentIndex, model.ItemPerColumn);
        if (row > 0)
        {
            int newIndex = GetIndexFromPosition(row - 1, col, model.ItemPerColumn);
            if (newIndex >= 0 && newIndex < model.Choices.Count)
            {
                return model.With(currentIndex: newIndex);
            }
        }
        return model;
    }

    private static SelectorModel<T> HandleDownMovement(SelectorModel<T> model)
    {
        var (row, col) = GetPositionFromIndex(model.CurrentIndex, model.ItemPerColumn);
        var maxRows = (model.Choices.Count + model.ItemPerColumn - 1) / model.ItemPerColumn;
        if (row < maxRows - 1)
        {
            int newIndex = GetIndexFromPosition(row + 1, col, model.ItemPerColumn);
            if (newIndex >= 0 && newIndex < model.Choices.Count)
            {
                return model.With(currentIndex: newIndex);
            }
        }
        return model;
    }

    private static SelectorModel<T> HandleLeftMovement(SelectorModel<T> model)
    {
        var (row, col) = GetPositionFromIndex(model.CurrentIndex, model.ItemPerColumn);
        if (col > 0)
        {
            int newIndex = GetIndexFromPosition(row, col - 1, model.ItemPerColumn);
            if (newIndex >= 0 && newIndex < model.Choices.Count)
            {
                return model.With(currentIndex: newIndex);
            }
        }
        return model;
    }

    private static SelectorModel<T> HandleRightMovement(SelectorModel<T> model)
    {
        var (row, col) = GetPositionFromIndex(model.CurrentIndex, model.ItemPerColumn);
        int newIndex = GetIndexFromPosition(row, col + 1, model.ItemPerColumn);
        if (newIndex >= 0 && newIndex < model.Choices.Count)
        {
            return model.With(currentIndex: newIndex);
        }
        return model;
    }

    private static SelectorModel<T> HandleSelection(SelectorModel<T> model)
    {
        var currentChoice = model.Choices[model.CurrentIndex];
        var newSelectedItems = model.SelectedItems.ToList();
        
        if (newSelectedItems.Contains(currentChoice))
        {
            newSelectedItems.Remove(currentChoice);
        }
        else
        {
            newSelectedItems.Add(currentChoice);
        }
        
        return model.With(selectedItems: newSelectedItems.AsReadOnly());
    }

    private static (int row, int col) GetPositionFromIndex(int index, int itemsPerColumn)
    {
        int row = index / itemsPerColumn;
        int col = index % itemsPerColumn;
        return (row, col);
    }

    private static int GetIndexFromPosition(int row, int col, int itemsPerColumn)
    {
        return (row * itemsPerColumn) + col;
    }
}

public class SelectorView<T> : IView<SelectorModel<T>, SelectorMsg>
{
    public SelectorModel<T> Render(SelectorModel<T> model)
    {
        if (model.IsCompleted || model.WasCancelled)
            return model;

        Console.Clear();
        
        // Render title
        if (!string.IsNullOrEmpty(model.Title))
        {
            Console.WriteLine(model.Title);
            Console.WriteLine();
        }

        // Show instructions
        Console.WriteLine("Use arrows or hjkl to navigate, space to select, enter to confirm, esc to cancel");
        Console.WriteLine();

        // Calculate rows and columns
        int totalRows = (model.Choices.Count + model.ItemPerColumn - 1) / model.ItemPerColumn;
        int totalColumns = model.ItemPerColumn;

        // Create a buffer for each row
        string[] rows = new string[totalRows];
        for (int i = 0; i < totalRows; i++)
        {
            rows[i] = "";
        }

        // Fill the buffer
        for (int row = 0; row < totalRows; row++)
        {
            for (int col = 0; col < totalColumns; col++)
            {
                int index = GetIndexFromPosition(row, col, model.ItemPerColumn);
                if (index >= 0 && index < model.Choices.Count)
                {
                    string item = model.Choices[index]?.ToString() ?? string.Empty;
                    if (item.Length > model.ColumnWidth - 5)
                    {
                        item = item.Substring(0, model.ColumnWidth - 8) + "...";
                    }

                    string prefix = index == model.CurrentIndex ? "> " : "  ";
                    string selected = model.SelectedItems.Contains(model.Choices[index]) ? "✓ " : "  ";
                    string paddedItem = (prefix + selected + item).PadRight(model.ColumnWidth);

                    if (index == model.CurrentIndex)
                    {
                        rows[row] += $"\x1b[47m\x1b[30m{paddedItem}\x1b[0m";
                    }
                    else
                    {
                        rows[row] += paddedItem;
                    }
                }
                else
                {
                    rows[row] += new string(' ', model.ColumnWidth);
                }
            }
        }

        // Render the buffer
        foreach (string row in rows)
        {
            Console.WriteLine(row);
        }

        return model;
    }

    private static int GetIndexFromPosition(int row, int col, int itemsPerColumn)
    {
        return (row * itemsPerColumn) + col;
    }
}

public class SelectorComponent<T> : Component<SelectorModel<T>, SelectorMsg>
{
    public static List<T> Show(IEnumerable<T> choices, string title)
    {
        SelectorComponent<T> component = new(choices, title);
        SelectorModel<T> finalState = component.Run();
        return finalState.WasCancelled ? new List<T>() : finalState.SelectedItems.ToList();
    }

    private SelectorComponent(IEnumerable<T> choices, string title)
        : base(
            new SelectorModel<T>(
                choices,
                Array.Empty<T>(),
                0,
                title
            ),
            new SelectorUpdate<T>(),
            new SelectorView<T>())
    {
    }

    protected override SelectorMsg CreateInitialMessage()
    {
        return new SelectorMsg(SelectorMsgType.Select);
    }

    protected override SelectorMsg HandleInput()
{
    ConsoleKeyInfo key = Console.ReadKey(true);
    return key.Key switch
    {
        ConsoleKey.UpArrow or ConsoleKey.K => new SelectorMsg(SelectorMsgType.Up),
        ConsoleKey.DownArrow or ConsoleKey.J => new SelectorMsg(SelectorMsgType.Down),
        ConsoleKey.LeftArrow or ConsoleKey.H => new SelectorMsg(SelectorMsgType.Left),
        ConsoleKey.RightArrow or ConsoleKey.L => new SelectorMsg(SelectorMsgType.Right),
        ConsoleKey.Spacebar => new SelectorMsg(SelectorMsgType.Select),
        ConsoleKey.Enter => new SelectorMsg(SelectorMsgType.Confirm),
        ConsoleKey.Escape => new SelectorMsg(SelectorMsgType.Cancel),
        _ => new SelectorMsg(SelectorMsgType.Select) // Default message
    };
}

    protected override bool ShouldContinue(SelectorMsg msg, SelectorModel<T> currentState)
    {
        return msg.Type != SelectorMsgType.Confirm && 
               msg.Type != SelectorMsgType.Cancel;
    }

    protected override void OnExit(SelectorModel<T> finalState)
    {
        Console.CursorVisible = true;
        Console.Clear();
        
        if (finalState.IsCompleted && finalState.SelectedItems.Any())
        {
            Console.WriteLine("You selected:");
            foreach (var item in finalState.SelectedItems)
            {
                Console.WriteLine($"- {item}");
            }
        }
        else
        {
            Console.WriteLine("Selection cancelled");
        }
    }
}
