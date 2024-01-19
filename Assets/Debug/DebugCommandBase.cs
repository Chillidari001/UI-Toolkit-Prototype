using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugCommandBase
{
    private string command_id;
    private string command_description;
    private string command_format;

    public string CommandID { get { return command_id; } }
    public string CommandDescription { get { return command_description; } }
    public string CommandFormat { get { return command_format; } }

    public DebugCommandBase(string id, string description, string format)
    {
        command_id = id;
        command_description = description;
        command_format = format;
    }
}

public class DebugCommand : DebugCommandBase
{
    private Action command;
    public DebugCommand(string id, string description, string format, Action command) : base(id, description, format)
    {
        this.command = command;
    }

    public void Invoke()
    {
        command.Invoke();
    }
}

public class DebugCommand<T1> : DebugCommandBase
{
    private Action<T1> command;
    public DebugCommand(string id, string description, string format, Action<T1> command) : base(id, description, format)
    {
        this.command = command;
    }

    public void Invoke(T1 value)
    {
        command.Invoke(value);
    }
}
