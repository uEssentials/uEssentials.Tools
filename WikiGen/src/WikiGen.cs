#region License
/*
 *  Copyright (C) 2016  leonardosnt
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NVelocity;
using NVelocity.App;
using NVelocity.Runtime.Directive;

namespace WikiGen {

  public class WikiGen {

    public static void Main(string[] args) {

      /*Velocity.Init();
      var context = new VelocityContext();
      var writer = new StringWriter();

      context.Put("name", "warp");
      context.Put("description", "Teleport to a warp.");
      context.Put("aliases", new [] { "alias1", "alias2" });
      context.Put("permissions", new [] {
        new KeyValuePair<string, string>("essentials.kit.bar", null),
        new KeyValuePair<string, string>("essentials.kit.foo", null),
        new KeyValuePair<string, string>("essentials.kit.'kit name'", "Allow use of specfied kit")
      });

      Velocity.Evaluate(context, writer, null, File.ReadAllText("D:/template.txt"));

      // Trim all lines
      var lines = writer.ToString().Split('\n');

      for (var i = 0; i < lines.Length; i++) {
        lines[i] = lines[i].Trim();
      }

      Console.WriteLine(string.Join("\n", lines));

      return;*/
      if (args.Length == 0) {
        Console.WriteLine("Use `wikigen [commands]`" );
        Environment.Exit(-1);
      }

      switch (args[0].ToLowerInvariant()) {
        case "commands":
          CommandReferenceGenerator.GenerateCommands(args.Skip(1).ToArray());
          break;

        default:
          Console.WriteLine($"Invalid option {args[0]}" );
          Environment.Exit(-1);
          break;
      }


#if DEBUG
      Console.ReadKey();
#endif
    }
  }

}