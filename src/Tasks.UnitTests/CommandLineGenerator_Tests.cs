// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Windows.Markup;
using Microsoft.Build.Framework;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.Build.Tasks.Xaml;
using Xunit;

#nullable disable

namespace Microsoft.Build.UnitTests
{
    public sealed class CommandLineGenerator_Tests
    {
        private const string testXamlFile = @"<?xml version='1.0' encoding='utf-8'?>
<Rule Name='mem' ToolName='mem.exe' PageTemplate='tool' SwitchPrefix='/' Order='10' xmlns='clr-namespace:Microsoft.Build.Framework.XamlTypes;assembly=Microsoft.Build.Framework' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:sys='clr-namespace:System;assembly=mscorlib'>
  <Rule.DisplayName>
    <sys:String>Memory Reporting Tool</sys:String>
  </Rule.DisplayName>
  <Rule.Categories>
    <Category Name='General'>
      <Category.DisplayName>
        <sys:String>General</sys:String>
      </Category.DisplayName>
    </Category>
  </Rule.Categories>
  <Rule.DataSource>
    <DataSource Persistence='ProjectFile' ItemType='ClCompile' Label='' HasConfigurationCondition='true' />
  </Rule.DataSource>
  <BoolProperty Name='Program' Category='General' Switch='P'/>
  <BoolProperty Name='Debug' Category='General' Switch='D'/>
  <BoolProperty Name='Classify' Category='General' Switch='C'/>
  <StringProperty Name='Subst' Category='Command Line' Switch='S[value]_postfix' />
  <StringProperty Name='Subst2' Category='Command Line' />
  <StringProperty Name='Subst3' Category='Command Line' Switch='AtEnd[value]' />
  <IntProperty Name='SubstInt' Category='Command Line' Switch='I[value]_postfix' />
  <StringListProperty Name='Strings' Switch='X' />
  <StringProperty Name='Sources' Category='Command Line' IsRequired='true'>
    <StringProperty.DataSource>
      <DataSource Persistence='ProjectFile' ItemType='ClCompile' SourceType='Item' Label='' HasConfigurationCondition='true' />
    </StringProperty.DataSource>
  </StringProperty>
  <EnumProperty Name='DebugInformationFormat' Category='General'>
    <EnumProperty.DisplayName>
      <sys:String>Debug Information Format</sys:String>
    </EnumProperty.DisplayName>
    <EnumProperty.Description>
      <sys:String>Specifies the type of debugging information generated by the compiler.  You must also change linker settings appropriately to match.    (/Z7, Zd, /Zi, /ZI)</sys:String>
    </EnumProperty.Description>
    <EnumValue Name='OldStyle' Switch='Z7'>
      <EnumValue.DisplayName>
        <sys:String>C7 compatible</sys:String>
      </EnumValue.DisplayName>
      <EnumValue.Description>
        <sys:String>Select the type of debugging information created for your program and whether this information is kept in object (.obj) files or in a program database (PDB).</sys:String>
      </EnumValue.Description>
    </EnumValue>
    <EnumValue Name='ProgramDatabase' Switch='Zi'>
      <EnumValue.DisplayName>
        <sys:String>Program Database</sys:String>
      </EnumValue.DisplayName>
      <EnumValue.Description>
        <sys:String>Produces a program database (PDB) that contains type information and symbolic debugging information for use with the debugger. The symbolic debugging information includes the names and types of variables, as well as functions and line numbers. </sys:String>
      </EnumValue.Description>
    </EnumValue>
    <EnumValue Name='EditAndContinue' Switch='ZI'>
      <EnumValue.DisplayName>
        <sys:String>Program Database for Edit And Continue</sys:String>
      </EnumValue.DisplayName>
      <EnumValue.Description>
        <sys:String>Produces a program database, as described above, in a format that supports the Edit and Continue feature.</sys:String>
      </EnumValue.Description>
    </EnumValue>
  </EnumProperty>
  <EnumProperty Name='EmptyTest' Category='General'>
    <EnumProperty.DisplayName>
      <sys:String>Empty Enum</sys:String>
    </EnumProperty.DisplayName>
    <EnumProperty.Description>
      <sys:String>Specifies the type of debugging information generated by the compiler.  You must also change linker settings appropriately to match.    (/Z7, Zd, /Zi, /ZI)</sys:String>
    </EnumProperty.Description>
    <EnumValue Name='Empty' >
      <EnumValue.DisplayName>
        <sys:String>Empty</sys:String>
      </EnumValue.DisplayName>
      <EnumValue.Description>
        <sys:String>An empty enum switch</sys:String>
      </EnumValue.Description>
    </EnumValue>
  </EnumProperty>
</Rule>
        ";

        /// <summary>
        /// Tests a command line generated from all of the specified switch values.
        /// </summary>
        [Fact]
        [Trait("Category", "mono-osx-failing")]
        [Trait("Category", "mono-windows-failing")]
        public void BasicCommandLine()
        {
            CommandLineGenerator generator = CreateGenerator();
            string commandLine = generator.GenerateCommandLine();
            Assert.Equal("/P /SSubstituteThis!_postfix SubstituteThis!AsWell /AtEndSubstitute\\ /I42_postfix /Xone /Xtwo /Xthree a.cs b.cs /Z7", commandLine);
        }

        /// <summary>
        /// Tests a command line generated from a specific template
        /// </summary>
        [Fact]
        [Trait("Category", "mono-osx-failing")]
        [Trait("Category", "mono-windows-failing")]
        public void TemplatedCommandLine()
        {
            CommandLineGenerator generator = CreateGenerator();
            generator.CommandLineTemplate = "[Sources] [Program]";
            string commandLine = generator.GenerateCommandLine();
            Assert.Equal("a.cs b.cs /P", commandLine);
        }

        /// <summary>
        /// Tests a command line generated from a specific template is not case sensitive on the parameter names.
        /// </summary>
        [Fact]
        [Trait("Category", "mono-osx-failing")]
        [Trait("Category", "mono-windows-failing")]
        public void TemplateParametersAreCaseInsensitive()
        {
            CommandLineGenerator generator = CreateGenerator();
            generator.CommandLineTemplate = "[sources]";
            string commandLine = generator.GenerateCommandLine();
            Assert.Equal("a.cs b.cs", commandLine);
        }

        private CommandLineGenerator CreateGenerator()
        {
#if !MONO
            Rule rule = XamlReader.Parse(testXamlFile) as Rule;

            Dictionary<string, Object> switchValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            switchValues["Program"] = true;
            switchValues["Debug"] = false;
            switchValues["Subst"] = "SubstituteThis!";
            switchValues["Subst2"] = "SubstituteThis!AsWell";
            switchValues["Subst3"] = "Substitute\\";
            switchValues["SubstInt"] = (int)42;
            switchValues["Strings"] = new string[] { "one", "two", "three" };
            ITaskItem[] sources = new ITaskItem[]
            {
                new TaskItem("a.cs"),
                new TaskItem("b.cs")
            };

            switchValues["Sources"] = sources;
            switchValues["DebugInformationFormat"] = "OldStyle";
            switchValues["EmptyTest"] = "Empty";

            CommandLineGenerator generator = new CommandLineGenerator(rule, switchValues);
            return generator;
#else
            return new CommandLineGenerator(new Rule(), new Dictionary<string, object>());
#endif
        }

        private class TaskItem : ITaskItem
        {
            public TaskItem(string itemSpec)
            {
                ItemSpec = itemSpec;
            }

            #region ITaskItem Members

            public string ItemSpec
            {
                get;
                set;
            }

            public System.Collections.ICollection MetadataNames
            {
                get { throw new NotImplementedException(); }
            }

            public int MetadataCount
            {
                get { throw new NotImplementedException(); }
            }

            public string GetMetadata(string metadataName)
            {
                throw new NotImplementedException();
            }

            public void SetMetadata(string metadataName, string metadataValue)
            {
                throw new NotImplementedException();
            }

            public void RemoveMetadata(string metadataName)
            {
                throw new NotImplementedException();
            }

            public void CopyMetadataTo(ITaskItem destinationItem)
            {
                throw new NotImplementedException();
            }

            public System.Collections.IDictionary CloneCustomMetadata()
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }
}
