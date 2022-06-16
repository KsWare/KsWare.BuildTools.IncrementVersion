using System.Text.RegularExpressions;

var projectFile = default(string);
var versionFile = default(string);
var desiredVersion = default(string);
var isTestOnly = false;

for (int i = 0; i < args.Length; i++) {
	switch (args[i].ToLower()) {
		case "-p": case "-project": projectFile = args[++i]; break;
		case "-f": versionFile= args[++i]; break;
		case "-v": desiredVersion= args[++i]; break;
		case "-t": case "-test": isTestOnly = true; break;
		case "-?": case "-h":case "-help": return ShowHelpEndExit();;
		default: if(projectFile==null) projectFile = args[i]; break;
	}
}
Console.WriteLine("--- Parameter ---");
Console.WriteLine($"ProjectFile:        {projectFile}");
Console.WriteLine($"DesiredVersionFile: {versionFile}");
Console.WriteLine($"Version:            {desiredVersion}");
Console.WriteLine($"IsTestOnly:         {isTestOnly}");

var testInfoLine = "# v1.1.";

var coreProj = @"...
 <Version>0.0.0.0-alpha1</Version>
 <AssemblyVersion>0.0.0.0</AssemblyVersion>
 <FileVersion>0.0.0.0</FileVersion>
 <VersionPrefix>0.0.0</VersionPrefix>
 <InformationalVersion>0.0.0-alpha</InformationalVersion>
";
var mauiProj = @"...
<ApplicationDisplayVersion>1.0.1</ApplicationDisplayVersion>
<ApplicationVersion>1</ApplicationVersion> <== Integer!
...
";
/*
 <VersionPrefix>1.0.3</VersionPrefix>
 <Version>1.0.7777.0-alpha1</Version>
 <AssemblyVersion>1.0.8888.0</AssemblyVersion>
 <FileVersion>1.0.9999.0</FileVersion>
*/



var content = default(string);
if (projectFile != null) {
	if (!File.Exists(projectFile)) return Error($"Project file not found. \n\tFilename: {projectFile}");
	using var r = File.OpenText(projectFile);
	content = r.ReadToEnd();
}
else {
	if (isTestOnly) {
		content = coreProj; //for test
		Console.WriteLine("WARNING: Project file not specified. Test data used.");
	}
	else return Error("Project file not specified.");
}
var versionProperties = Regex.Matches(content, @"<(?<name>[^>]*Version[^>]*)>\s*(?<content>[^<]*)\s*</\k<name>>")
	.ToDictionary(m=>m.Groups["name"].Value,m=>m.Groups["content"].Value);

var desiredSuffix = (string) null;
if (desiredVersion == null) {
	string? info = null;
	if (versionFile != null) {
		if (!File.Exists(versionFile)) return Error($"Info file not found. \n\tFilename: {versionFile}");
		using var r = File.OpenText(versionFile);
		info = r.ReadLine()??"";
		if (Regex.IsMatch(info, @"^# \d+\.\d+\.\d+")) return 0; // nothing to do.
		while (info!=null) {
			if (Regex.IsMatch(info, @"^# \d+\.\d+")) break;
			info = r.ReadLine();
		}
		
		if (string.IsNullOrWhiteSpace(info)) return Error("Info file contains no version number.");
		
	}
	else if (isTestOnly) {
		info = testInfoLine;
		Console.WriteLine("WARNING: Info file not specified. Test data used.");
	}
	else {
		if (versionProperties.ContainsKey("VersionPrefix")) info = versionProperties["VersionPrefix"].MajorMinor().ToString(2);
		else if (versionProperties.ContainsKey("ApplicationDisplayVersion")) info = versionProperties["ApplicationDisplayVersion"].MajorMinor().ToString(2);
		else if (versionProperties.ContainsKey("Version")) info = versionProperties["VersionPrefix"].MajorMinor().ToString(2);
		else if (versionProperties.ContainsKey("AssemblyVersion")) info = versionProperties["VersionPrefix"].MajorMinor().ToString(2);
		else return Error("Neither info file nor version specified.");
	}
	var versionMatch = Regex.Match(info, @"(?<majorminor>\d+\.\d+)(\.\d+)?(?<suffix>-[^\s]+)?");
	if (versionMatch.Success==false) return Error("First line in info file contains no version number.");
	desiredVersion = versionMatch.Groups["majorminor"].Value;
	desiredSuffix = versionMatch.Groups["suffix"]?.Value;
}
Console.WriteLine($"Desired version:    {desiredVersion}{desiredSuffix}");


var w = new Writer {Content = content};
Version newVersion;
if (versionProperties.ContainsKey("ApplicationDisplayVersion")) {
	// MAUI project
	var applicationDisplayVersion = versionProperties["ApplicationDisplayVersion"].MajorMinorBuild();
	newVersion = IncrementVersion(applicationDisplayVersion, desiredVersion.MajorMinor());

	var applicationVersion = int.Parse(versionProperties["ApplicationVersion"]);
	applicationVersion += 1;

	w.ReplaceTag("ApplicationDisplayVersion", newVersion);
	w.ReplaceTag("ApplicationVersion", applicationVersion);
}
else {
	// Core project
	Version? current = null;
	if (versionProperties.ContainsKey("VersionPrefix")) current = versionProperties["VersionPrefix"].MajorMinorBuild();
	else if (versionProperties.ContainsKey("Version")) current = versionProperties["Version"].MajorMinorBuild();
	else if (versionProperties.ContainsKey("AssemblyVersion")) current = versionProperties["AssemblyVersion"].MajorMinorBuild();
	else throw new InvalidOperationException("Version not found");
	var currentBuild = current.Build;

	var newBuild = desiredVersion.MajorMinor() > current ? 0 : currentBuild + 1;
	newVersion = new Version(desiredVersion.MajorPart(), desiredVersion.MinorPart(), newBuild);
	
	foreach (var p in versionProperties) {
		if(p.Value.Contains("$(")) continue; // skip properties with macros 
		w.ReplaceTag(p.Key, newVersion);
	}
}

// show changes
Console.WriteLine("--- Changes ---");
var versionProperties2 = Regex.Matches(w.Content, @"<(?<name>[^>]*Version[^>]*)>\s*(?<content>[^<]*)\s*</\k<name>>")
	.ToDictionary(m=>m.Groups["name"].Value,m=>m.Groups["content"].Value);
foreach (var p in versionProperties2) {
	Console.WriteLine($"{p.Key,-24} {versionProperties[p.Key],-20} => {p.Value}");
}

if (!isTestOnly) {
	if (versionFile != null) {
		using var infoFile = File.OpenText(versionFile);
		var info1=infoFile.ReadLine();
		var info = infoFile.ReadToEnd();
		infoFile.Close();
		using var infoFileW = File.CreateText(versionFile);
		infoFileW.WriteLine($"# {newVersion.ToString(3)} [{DateTime.Now.ToString("yyyy-MM-dd")}]");
		if(!Regex.IsMatch(info1,@"# \d+\.\d+([^.]|$)")) infoFileW.WriteLine(info1);
		infoFileW.Write(info);
		infoFileW.Close();
	}

	w.SaveTo(projectFile);

	Console.WriteLine("INFO: Changes has been written.");
	return 0;
}
else {
	Console.WriteLine("INFO: Test only. No changes were written.");
	return 1;
}



Version IncrementVersion(Version current, Version desired) {
	var currentBuild = current.Build;
	current = new Version(current.Major, current.Minor);
	if (desired < current) throw new InvalidOperationException("Current version is greater than desired version.");
	var newBuild = desired > current ? 0 : currentBuild + 1;
	var newVersion = new Version(desired.Major, desired.Minor, newBuild);
	return newVersion;
}

int Error(string message) {
	Console.WriteLine($"ERROR: {message}");
	return 1;
}

int ShowHelpEndExit() {
	Console.WriteLine($@"
VersionIncrement.exe [project|-p project][-f <path>|-v <version>]

Parameter:
  -project  -p     path to the project file
  -f <path>        path to file which contains the version
  -v <version>     version
");
	//                                                                        |
	return 0;
}

static class StringVersionExtension {
	public static Version MajorMinorBuild(this string s) => new Version(string.Join(".", s.Split('.').Take(3)));
	public static Version MajorMinor(this string s) => new Version(string.Join(".", s.Split('.').Take(2)));
	public static Version Major(this string s) => new Version(string.Join(".", s.Split('.').Take(1)));
	public static int MajorPart(this string s) => int.Parse(s.Split('.')[0]);
	public static int MinorPart(this string s) => int.Parse(s.Split('.')[1]);
	public static int BuildPart(this string s) => int.Parse(s.Split('.')[2]);
	public static string SuffixPart(this string s) => Regex.Match(s,@"-[^\s]+").Value;
}


class Writer {

	public string Content { get; set; }

	public void SaveTo(string file) {
		using var w = File.CreateText(file);
		w.Write(Content);
	}

	public void ReplaceTag(string name, string content) {
		Content = Regex.Replace(
			Content, 
			@"\<(?<name>"+name+@")\>(?<content>[^<]*)\</\k<name>\>", 
			$"<{name}>{content}</{name}>");
	}

	public void ReplaceTag(string name, Version version) => ReplaceTag(name, version.ToString());

	public void ReplaceTag(string name, int number) => ReplaceTag(name, number.ToString());
}
