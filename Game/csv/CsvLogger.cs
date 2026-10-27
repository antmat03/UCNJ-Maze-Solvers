using Godot;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

public static class CsvLogger
{
    public static void Append<T>(string path, T row)
{
	string fullPath = path.StartsWith("user://") || path.StartsWith("res://")
		? ProjectSettings.GlobalizePath(path)
		: path;

	// Public fields and readable properties, in declaration order.
	FieldInfo[] fields = typeof(T)
		.GetFields(BindingFlags.Public | BindingFlags.Instance);
	PropertyInfo[] props = typeof(T)
		.GetProperties(BindingFlags.Public | BindingFlags.Instance)
		.Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
		.ToArray();

	var members = fields.Select(f => (Name: f.Name, Get: (System.Func<object>)(() => f.GetValue(row))))
		.Concat(props.Select(p => (Name: p.Name, Get: (System.Func<object>)(() => p.GetValue(row)))))
		.ToList();

	if (members.Count == 0)
	{
		GD.PushError($"CsvLogger: {typeof(T).Name} has no public fields or properties to write.");
		return;
	}

	bool needsHeader = !File.Exists(fullPath) || new FileInfo(fullPath).Length == 0;

	StringBuilder sb = new();
	if (needsHeader)
	{
		sb.AppendLine(string.Join(",", members.Select(m => Escape(m.Name))));
	}
	sb.AppendLine(string.Join(",", members.Select(m => Escape(Format(m.Get())))));

	File.AppendAllText(fullPath, sb.ToString());
}

	private static string Format(object value)
	{
		// Invariant culture keeps decimals as "0.12" even on machines that use "0,12".
		return value is System.IFormattable f
			? f.ToString(null, CultureInfo.InvariantCulture)
			: value?.ToString() ?? "";
	}

	private static string Escape(string s)
	{
		return s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
			? "\"" + s.Replace("\"", "\"\"") + "\""
			: s;
	}
}