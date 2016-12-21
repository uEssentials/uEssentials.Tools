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
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NVelocity;
using NVelocity.App;

namespace WikiGen {

  public class CommandReferenceGenerator {

    /// <summary>
    /// arg 0 = working directory, resources must be in this directory.
    /// arg 1 = uEssentials assembly path
    /// arg 21 = markdown output path
    /// </summary>
    /// <param name="args"></param>
    public static void GenerateCommands(string[] args) {
      if (args.Length < 4) {
        Console.WriteLine("Use `GenerateCommands [Working directory] [uEssentials assembly path] [markdown output path] [Locale]");
        Environment.Exit(-1);
      }

      var workingDirectory = args[0];
      var essAssemblyPath = args[1];
      var markdownOutputPath = args[2];
      var locale = args[3];

      var templatePath = Path.Combine(workingDirectory, $"template_{locale}.txt");
      var commandSpecPath = Path.Combine(workingDirectory, $"command_spec_{locale}.json");
      var commandTemplatePath = Path.Combine(workingDirectory, $"command_template_{locale}.vm");

      // Load default files
      if (!File.Exists(templatePath)) {
        templatePath = Path.Combine(workingDirectory, "template_en-US.txt");
      }

      if (!File.Exists(commandSpecPath)) {
        commandSpecPath = Path.Combine(workingDirectory, "command_spec_en-US.json");
      }

      if (!File.Exists(commandTemplatePath)) {
        commandTemplatePath = Path.Combine(workingDirectory, "command_template_en-US.vm");
      }

      if (!File.Exists(essAssemblyPath)) {
        Console.Error.WriteLine("File not found: {0}", essAssemblyPath);
        Environment.Exit(-1);
      }

      Console.WriteLine("TemplatePath: {0}", templatePath);
      Console.WriteLine("CommandSpecPath: {0}", commandSpecPath);
      Console.WriteLine("CommandTemplatePath: {0}", commandTemplatePath);

      JObject commandsSpec = null;

      if (File.Exists(commandSpecPath)) {
        commandsSpec = JObject.Parse(File.ReadAllText(commandSpecPath));
      }

      var asmDef = AssemblyDefinition.ReadAssembly(essAssemblyPath);
      var mainModule = asmDef.MainModule;

      Func<CustomAttribute, bool> commandInfoAttrPredicate = attr => (
        "Essentials.Api.Command.CommandInfo".Equals(attr.AttributeType.FullName)
      );

      var commandsMarkdownWriter = new StringWriter();
      var quickLinksMarkdown = new StringBuilder();

      // Init velocity
      Velocity.Init();

      mainModule.Types
        .SelectMany(type => {
          var attrs = new List<CustomAttribute>();

          // Get from type
          var typeAttr = type.CustomAttributes.FirstOrDefault(commandInfoAttrPredicate);
          if (typeAttr != null) {
            attrs.Add(typeAttr);
          }

          // Get from MethodCommands
          type.Methods
            .Select(method => method.CustomAttributes.FirstOrDefault(commandInfoAttrPredicate))
            .Where(attr => attr != null)
            .ForEach(attrs.Add);

          return attrs.AsEnumerable();
        })
        .Select(attr => { // Transform to CommandTemplate
          var commandTemplate = new CommandTemplate();
          attr.Fields.ForEach(field => {
            switch (field.Name) {
              case "Name":
                commandTemplate.Name = (string) field.Argument.Value;
                break;

              case "Usage":
                commandTemplate.Usage = (string) field.Argument.Value;
                break;

              case "Description":
                commandTemplate.Description = (string) field.Argument.Value;
                break;

              case "Aliases":
                commandTemplate.Aliases = CustomAttrArrayToArray<string>(field.Argument.Value);
                break;
            }

            var defaultPermission = new Permission {
              Value = $"essentials.command.{commandTemplate.Name}"
            };

            var commandSpec = commandsSpec?[commandTemplate.Name.ToLowerInvariant()];
            var extraPermissions = commandSpec?["additional_permissions"]?.ToObject<Permission[]>();

            if (extraPermissions == null) {
              commandTemplate.Permissions = new Permission[] { defaultPermission };
            } else {
              // Concat additional permissions
              var permissions = new Permission[extraPermissions.Length + 1];

              // First is default permission
              permissions[0] = defaultPermission;

              Array.Copy(extraPermissions, 0, permissions, 1, permissions.Length - 1);
              commandTemplate.Permissions = permissions;
            }
          });
          return commandTemplate;
        })
        .OrderBy(template => template.Name)
        .ForEach(template => {
          // Render command template
          var context = new VelocityContext();

          context.Put("name", template.Name);
          context.Put("description", template.Description);
          context.Put("aliases", template.Aliases);
          context.Put("permissions", template.Permissions);
          context.Put("usage", template.Usage != null ? $"/{template.Name} {template.Usage}" : null);

          Velocity.Evaluate(context, commandsMarkdownWriter, "commandgen", File.ReadAllText(commandTemplatePath));

          // Add link to quicklinks
          quickLinksMarkdown.AppendLine($" * [{Capitalize(template.Name)}](#{template.Name})  ");
        });


      var pageTemplate = File.ReadAllText(templatePath);
      var commandsMarkdown = commandsMarkdownWriter.GetStringBuilder().ToString();

      // Trim all lines
      var lines = commandsMarkdown.Split('\n');
      for (var i = 0; i < lines.Length; i++) {
        lines[i] = lines[i].Trim();
      }
      commandsMarkdown = string.Join("\n", lines);

      File.WriteAllText(markdownOutputPath, string.Format(pageTemplate, quickLinksMarkdown, commandsMarkdown, DateTime.Now));
      Console.WriteLine("Done!");
    }

    /// <summary>
    /// Convert an array of CustomAttributeArgument to array of T's
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    private static T[] CustomAttrArrayToArray<T>(object value) {
      return ((CustomAttributeArgument[]) value)
        .Select(arg => arg.Value)
        .Cast<T>()
        .ToArray();
    }

    private static string Capitalize(string source) {
      var chars = source.ToCharArray();
      chars[0] = char.ToUpper(chars[0]);
      return new string(chars);
    }

    /// <summary>
    ///
    /// Markdown Example:
    ///
    /// Name: boom
    /// Description: Create an explosion on player's/given position
    /// Usage: /boom [player | * | x, y, z]
    /// Aliases:
    ///  - explode
    /// Permissions:
    ///  - essentials.command.boom
    ///
    /// </summary>
    private struct CommandTemplate {

      public string Name;
      public string Usage;
      public string Description;
      public string[] Aliases;
      public Permission[] Permissions;

    }

    private struct Permission {

      [JsonProperty("value")]
      public string Value { get; set; }

      [JsonProperty("description")]
      public string Description { get; set; }

    }
  }

}