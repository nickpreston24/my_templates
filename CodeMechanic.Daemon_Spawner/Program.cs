// See https://aka.ms/new-console-template for more information

using CodeMechanic.Diagnostics;
using CodeMechanic.Shargs;
using CodeMechanic.Types;

Console.WriteLine("Hello, World!");
var cmds = new ArgumentsCollection(args);
bool debug = cmds.MatchingFlag("-D", "--debug");

if (debug) cmds.Dump();

(string dname_flag, var daemon_name_input) = cmds.Matching("--name");
string project_name = (dname_flag.Equals("--name"))
    ? daemon_name_input.SingleOrDefault() ?? string.Empty
    : string.Empty;

if (project_name.IsEmpty())
    Console.WriteLine("daemon name cannot be empty!  Please use the command --name <your daemon name>");