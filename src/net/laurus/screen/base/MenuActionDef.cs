using System;
using XRL.World;

/// <summary>
/// Defines a menu action exposed in inventory.
/// </summary>
public class MenuActionDef
    {
        public string Display { get; }
        public string Verb { get; }
        public string Command { get; }
        public string Key { get; }
        public char Default { get; }
        public bool WorksTelekinetically { get; }
        public Func<InventoryActionEvent, bool> Handler { get; }

        public MenuActionDef(
            string display,
            string verb,
            string command,
            string key = null,
            char @default = '0',
            bool worksTelekinetically = false,
            Func<InventoryActionEvent, bool> handler = null)
        {
            Display = display;
            Verb = verb;
            Command = command;
            Key = key;
            Default = @default;
            WorksTelekinetically = worksTelekinetically;
            Handler = handler;
        }
    }