using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore.Internal;
using Sora.Database;
using Sora.Helpers;
using Console = Colorful.Console;
using Logger = Sora.Helpers.Logger;
using Users = Sora.Database.Models.Users;

namespace Sora.Services
{
    public struct Argument
    {
        public string ArgName;
    }
    
    public struct ConsoleCommand
    {
        public delegate bool ConsoleCommandExecution(string[] args);
        
        public string Command;
        public string Description;
        public List<Argument> Args;
        public bool ConsoleOnly;
        public int ExpectedArgs;
        public ConsoleCommandExecution Callback;
    }
    
    public class ConsoleCommandService
    {
        private List<ConsoleCommand> _commands;
        private Thread _consoleThread;
        private readonly object _mut;
        private bool _shouldStop;

        public List<ConsoleCommand> Commands => _commands;

        public ConsoleCommandService(SoraDbContextFactory factory)
        {
            _commands = new List<ConsoleCommand>();
            _mut = new object();
            
            #region DefaultCommands
            RegisterCommand("help", 
                            "Get a list of ALL Commands!", 
                            new List<Argument>
                            {
                                new Argument
                                {
                                    ArgName = "command"
                                }
                            },
                            0,
            args =>
            {
                string l = "\n====== Command List ======\n";
                
                foreach (ConsoleCommand cmd in _commands)
                {
                    string aList = "\t<";

                    int i = 0;
                    foreach (Argument a in cmd.Args)
                    {
                        aList += a.ArgName;
                        if (i >= cmd.ExpectedArgs)
                            aList += "?";
                        aList += ", ";
                        i++;
                    }

                    aList = aList.TrimEnd(cmd.Args.Count < 1 ? '<' : ' ').TrimEnd(',');
                    if (cmd.Args.Count > 0)
                        aList += ">";

                    l += "\n%#1c9624%/*\n";
                    l += cmd.Description;
                    l += $"\n*/{L_COL.WHITE}\n";
                    l += "\n" +  cmd.Command + aList;
                    l += "\n";
                }

                l += "\n==========================";
                
                Logger.Info(l);

                return true;
            });

            RegisterCommand("clear",
                "Clear Console",
                new List<Argument>(),
                0,
                args =>
                {
                    System.Console.Clear();
                    return true;
                });
            
            RegisterCommand("adduser",
                "Add a User to our Database",
                new List<Argument>
                {
                    new Argument
                    {
                        ArgName = "Username"
                    },
                    new Argument
                    {
                        ArgName  = "Password"
                    },
                    new Argument
                    {
                        ArgName  = "E-Mail"
                    },
                    new Argument
                    {
                        ArgName  = "Permissions"
                    }
                },
                3,
                args =>
                {
                    Users u = Users.NewUser(factory, args[0], args[1], args[2], Permission.From(args[3..].Join(", ")));
                    Logger.Info("Created User",
                                "%#F94848%" + u.Username,
                                "%#B342F4%(", Users.GetUserId(factory, u.Username), "%#B342F4%)");
                    return true;
                });
            #endregion
        }      
        
        public void RegisterCommand(string Command, string Description, List<Argument> args, int expectedArgs, ConsoleCommand.ConsoleCommandExecution cb)
        {
            lock(_mut)
                _commands.Add(new ConsoleCommand
            {
                Command = Command,
                Description = Description,
                Args = args,
                ExpectedArgs = expectedArgs,
                Callback = cb
            });
        }

        public IEnumerable<ConsoleCommand> GetCommands(string Command)
        {
            lock (_mut)
                return _commands.Where(z => z.Command == Command.Split(" ")[0]);
        }

        public void Start()
        {
            if (_consoleThread?.IsAlive ?? false)
                return;
            _shouldStop = false;
            _consoleThread = new Thread(() =>
            {
                string x = string.Empty;
                
                while (!_shouldStop)
                {
                    ConsoleKeyInfo k = Console.ReadKey();
Console.SetCursorPosition(0, Console.CursorTop);
                        System.Console.Write(x);
                        
                        switch (k.Key)
                        {
                            case ConsoleKey.Enter:
                                IEnumerable<ConsoleCommand> cmds = GetCommands(x);
                                string x1 = x;
                                new Thread(() =>
                                {
                                    IEnumerable<ConsoleCommand> consoleCommands = cmds as ConsoleCommand[] ?? cmds.ToArray();
                                    if (!consoleCommands.ToList().Any())
                                        Logger.Err("Command not found! type %#f1fc5a%help %#FFFFFF%for a Command List!");
                                    foreach (ConsoleCommand m in consoleCommands)
                                    {
                                        List<string> l = x1.Split(" ").ToList();
                                        l.RemoveAt(0);
                                        if (l.Count < m.ExpectedArgs)
                                        {
                                            string aList = "\t<";

                                            int i = 0;
                                            foreach (Argument a in m.Args)
                                            {
                                                aList += a.ArgName;
                                                if (i >= m.ExpectedArgs)
                                                    aList += "?";
                                                aList += ", ";
                                                i++;
                                            }

                                            aList = aList.TrimEnd(m.Args.Count < 1 ? '<' : ' ').TrimEnd(',');
                                            if (m.Args.Count > 0)
                                                aList += ">";
                                            
                                            Logger.Err("Insufficient amount of Arguments!\nUsage: " +m.Command + aList);
                                            break;
                                        }
                                        if (m.Callback(l.ToArray()))
                                            break;
                                    }
                                }).Start();
                                
                                x = "";
                                Console.Write(new string(' ', Console.WindowWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                break;
                            case ConsoleKey.Backspace:
                                if (x.Length == 0)
                                    break;
                                x = x.Remove(x.Length - 1);
                                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                                System.Console.Write(" ");
                                break;
                            default:
                                x += k.KeyChar;
                                System.Console.Write(k.KeyChar);
                                break;
                        }

                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                }
            });

            _consoleThread.Start();
        }

        public void Stop()
        {
            _shouldStop = true;
            _consoleThread?.Join();
        }
    }
}