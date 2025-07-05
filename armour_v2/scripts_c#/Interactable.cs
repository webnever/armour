using System;
using System.Collections.Generic;

public class ContextMenuAction
{
    public string Label { get; set; }
    public Action Callback { get; set; }
    public bool IsEnabled { get; set; } = true;

    public ContextMenuAction(string label, Action callback, bool isEnabled = true)
    {
        Label = label;
        Callback = callback;
        IsEnabled = isEnabled;
    }
}

public interface IInteractable
{
    void Interact();
    List<ContextMenuAction> GetContextActions();
}
