using System;
using System.Collections.Generic;

namespace Cli_bliss.elements;



public class ColumnSelectionPrompt<T>
{
// could you make this into a Dictionary and still have it work?
    private readonly List<T> _choices;
    private readonly List<T> _selectedItems;
    private int _currentIndex;
    private const int ITEMS_PER_COLUMN = 4;
    private const int COLUMN_WIDTH = 20;
    private string _title;

    public ColumnSelectionPrompt()
    {
        _choices = new List<T>();
        _selectedItems = new List<T>();
        _currentIndex = 0;
        _title = string.Empty;
    }

    public ColumnSelectionPrompt<T> SetTitle(string title)
    {
        _title = title;
        return this;
    }

    public ColumnSelectionPrompt<T> AddChoices(IEnumerable<T> choices)
    {
        _choices.AddRange(choices);
        return this;
    }

    private static (int row, int col) GetPositionFromIndex(int index)
    {
        int row = index % ITEMS_PER_COLUMN;
        int col = index / ITEMS_PER_COLUMN;
        return (row, col);
    }

    private int GetIndexFromPosition(int row, int col)
    {
        int index = (col * ITEMS_PER_COLUMN) + row;
        return index < _choices.Count ? index : -1;
    }

    private void RenderChoices()
    {
        Console.Clear();
        
        // Render title
        if (!string.IsNullOrEmpty(_title))
        {
            Console.WriteLine(_title);
            Console.WriteLine();
        }

        // Show instructions
        Console.WriteLine("Use arrows or hjkl to navigate, space to select, enter to confirm, esc to cancel");
        Console.WriteLine();

        // Calculate number of columns needed
        int columnCount = (_choices.Count + ITEMS_PER_COLUMN - 1) / ITEMS_PER_COLUMN;

        // Create a buffer for each row
        string[] rows = new string[ITEMS_PER_COLUMN];
        for (int i = 0; i < ITEMS_PER_COLUMN; i++)
        {
            rows[i] = "";
        }

        // Fill the buffer
        for (int col = 0; col < columnCount; col++)
        {
            for (int row = 0; row < ITEMS_PER_COLUMN; row++)
            {
                int index = GetIndexFromPosition(row, col);
                if (index >= 0 && index < _choices.Count)
                {
                    string item = _choices[index].ToString();
                    if (item.Length > COLUMN_WIDTH - 5)
                    {
                        item = item.Substring(0, COLUMN_WIDTH - 8) + "...";
                    }

                    string prefix = index == _currentIndex ? "> " : "  ";
                    string selected = _selectedItems.Contains(_choices[index]) ? "✓ " : "  ";
                    string paddedItem = (prefix + selected + item).PadRight(COLUMN_WIDTH);

                    if (index == _currentIndex)
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
                    rows[row] += new string(' ', COLUMN_WIDTH);
                }
            }
        }

        // Render the buffer
        foreach (string row in rows)
        {
            Console.WriteLine(row);
        }
    }

    private void MoveUp()
    {
        var (row, col) = GetPositionFromIndex(_currentIndex);
        if (row > 0)
        {
            int newIndex = GetIndexFromPosition(row - 1, col);
            if (newIndex >= 0)
            {
                _currentIndex = newIndex;
            }
        }
    }

    private void MoveDown()
    {
        var (row, col) = GetPositionFromIndex(_currentIndex);
        if (row < ITEMS_PER_COLUMN - 1)
        {
            int newIndex = GetIndexFromPosition(row + 1, col);
            if (newIndex >= 0 && newIndex < _choices.Count)
            {
                _currentIndex = newIndex;
            }
        }
    }

    private void MoveLeft()
    {
        var (row, col) = GetPositionFromIndex(_currentIndex);
        if (col > 0)
        {
            int newIndex = GetIndexFromPosition(row, col - 1);
            if (newIndex >= 0)
            {
                _currentIndex = newIndex;
            }
        }
    }

    private void MoveRight()
    {
        var (row, col) = GetPositionFromIndex(_currentIndex);
        int newIndex = GetIndexFromPosition(row, col + 1);
        if (newIndex >= 0 && newIndex < _choices.Count)
        {
            _currentIndex = newIndex;
        }
    }

    private void ToggleSelection()
    {
        var currentChoice = _choices[_currentIndex];
        if (_selectedItems.Contains(currentChoice))
        {
            _selectedItems.Remove(currentChoice);
        }
        else
        {
            _selectedItems.Add(currentChoice);
        }
    }

    public List<T> Show()
    {
        Console.CursorVisible = false;
        
        while (true)
        {
            RenderChoices();

            var key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.K:
                    MoveUp();
                    break;

                case ConsoleKey.DownArrow:
                case ConsoleKey.J:
                    MoveDown();
                    break;

                case ConsoleKey.LeftArrow:
                case ConsoleKey.H:
                    MoveLeft();
                    break;

                case ConsoleKey.RightArrow:
                case ConsoleKey.L:
                    MoveRight();
                    break;

                case ConsoleKey.Spacebar:
                    ToggleSelection();
                    break;

                case ConsoleKey.Enter:
                    Console.CursorVisible = true;
                    return _selectedItems;

                case ConsoleKey.Escape:
                    Console.CursorVisible = true;
                    return new List<T>();
            }
        }
    }
}
