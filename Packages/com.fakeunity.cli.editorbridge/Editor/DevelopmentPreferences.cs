// Shared source: host CLI and Unity Editor use the same project-local, Git-ignored settings file.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
#if UNITY_EDITOR
using UnityEngine;
#else
using System.Text.Json;
#endif

namespace FakeUnity.Development
{
    [Serializable]
    public sealed class DevelopmentPreferences
    {
        public string schema_version = "2.0";
        public string default_mode = "";
    }
    public sealed class DevelopmentPreferencesObservation
    {
        public string Status { get; }
        public DevelopmentPreferences Settings { get; }
        public string Path { get; }
        public string Error { get; }
        public DevelopmentPreferencesObservation(string status, DevelopmentPreferences settings, string path, string error = null)
        { Status = status; Settings = settings; Path = path; Error = error; }
    }

    public sealed class DevelopmentPreferencesResolution
    {
        public string Mode { get; }
        public string Source { get; }
        public string Path { get; }
        public string Error { get; }
        public DevelopmentPreferencesResolution(string mode, string source, string path, string error = null)
        { Mode = mode; Source = source; Path = path; Error = error; }
    }

    public static class DevelopmentPreferencesStore
    {
        private const int MaxBytes = 131072;
        public static string GetPath(string projectRoot)
        {
            return System.IO.Path.Combine(NormalizeProjectRoot(projectRoot), ".fuc", "development-settings.json");
        }
        public static string NormalizeProjectRoot(string root)
        {
            if (String.IsNullOrWhiteSpace(root) || !System.IO.Path.IsPathFullyQualified(root)) throw new ArgumentException("Project root must be an absolute path.");
            var full = System.IO.Path.GetFullPath(root).Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);
            var volume = System.IO.Path.GetPathRoot(full);
            return full.Length > volume.Length ? full.TrimEnd(System.IO.Path.DirectorySeparatorChar) : full;
        }

        public static DevelopmentPreferencesObservation Read(string projectRoot, string path = null)
        {
            var resolved = path;
            try
            {
                NormalizeProjectRoot(projectRoot);
                resolved = System.IO.Path.GetFullPath(path ?? GetPath(projectRoot));
                RejectLink(System.IO.Path.GetDirectoryName(resolved)); RejectLink(resolved);
                using (var file = new FileStream(resolved, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    if (file.Length > MaxBytes) throw new IOException("Development settings exceed 128 KiB.");
                    using (var reader = new StreamReader(file, new UTF8Encoding(false, true), true))
                    {
                        var json = reader.ReadToEnd();
                        // Reject unknown/duplicate fields rather than silently dropping a newer writer's data.
                        new SchemaReader(json).Validate();
                        var settings = Deserialize(json);
                        Validate(settings);
                        return new DevelopmentPreferencesObservation("ok", settings, resolved);
                    }
                }
            }
            catch (FileNotFoundException) { return new DevelopmentPreferencesObservation("missing", new DevelopmentPreferences(), resolved); }
            catch (DirectoryNotFoundException) { return new DevelopmentPreferencesObservation("missing", new DevelopmentPreferences(), resolved); }
            catch (Exception ex) { return new DevelopmentPreferencesObservation("invalid", null, resolved, ex.GetType().Name + ": " + ex.Message); }
        }

        public static DevelopmentPreferencesResolution Resolve(string projectRoot, string path = null)
        {
            if (projectRoot == null) return new DevelopmentPreferencesResolution("auto", "builtin", null);
            var state = Read(projectRoot, path);
            if (state.Error != null) return new DevelopmentPreferencesResolution("preserve", "settings-error", state.Path, state.Error);
            return String.IsNullOrEmpty(state.Settings.default_mode)
                ? new DevelopmentPreferencesResolution("auto", "builtin", state.Path)
                : new DevelopmentPreferencesResolution(state.Settings.default_mode, "project-default", state.Path);
        }

        public static DevelopmentPreferencesObservation Set(string mode, string projectRoot, string path = null)
        {
            RequireMode(mode);
            return Update(mode, projectRoot, path);
        }

        public static DevelopmentPreferencesObservation Clear(string projectRoot, string path = null) { return Update(null, projectRoot, path); }

        // Editor window rejects an outdated draft before atomically changing the project default.
        public static DevelopmentPreferencesObservation SaveDefault(string mode, string projectRoot,
            DevelopmentPreferencesObservation expected, string path = null)
        {
            if (mode != null) RequireMode(mode);
            if (expected == null || expected.Settings == null) throw new ArgumentException("Saving the window requires a valid observed settings snapshot.");
            return Update(mode, projectRoot, path, expected);
        }
        private static DevelopmentPreferencesObservation Update(string mode, string projectRoot, string path,
            DevelopmentPreferencesObservation expected = null)
        {
            var root = NormalizeProjectRoot(projectRoot);
            var target = System.IO.Path.GetFullPath(path ?? GetPath(root));
            var directory = System.IO.Path.GetDirectoryName(target);
            RejectLink(directory);
            Directory.CreateDirectory(directory);
            var mutex = target + ".mutex";
            RejectLink(mutex);
            // Cross-process read/modify/write lease: a competing update fails clearly instead of losing data.
            using (new FileStream(mutex, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var state = Read(root, target);
                if (state.Error != null) throw new IOException("Refusing to overwrite unreadable development settings: " + state.Error);
                if (expected != null && (state.Status != expected.Status || Serialize(state.Settings) != Serialize(expected.Settings)))
                    throw new IOException("Development settings changed after the window loaded them. Reload before saving.");
                var settings = state.Settings;
                settings.default_mode = mode ?? "";
                Validate(settings);
                var bytes = new UTF8Encoding(false).GetBytes(Serialize(settings));
                if (bytes.Length > MaxBytes) throw new IOException("Development settings exceed 128 KiB.");
                RejectLink(target);
                var pending = target + ".pending-" + Guid.NewGuid().ToString("N");
                try
                {
                    using (var file = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                    if (File.Exists(target)) File.Replace(pending, target, null);
                    else File.Move(pending, target);
                }
                finally { if (File.Exists(pending)) File.Delete(pending); }
                return Read(root, target);
            }
        }

        private static void Validate(DevelopmentPreferences settings)
        {
            if (settings == null || settings.schema_version != "2.0" || settings.default_mode == null)
                throw new IOException("Unsupported or incomplete development settings schema.");
            if (settings.default_mode != "") RequireMode(settings.default_mode);
        }
        private static void RequireMode(string mode)
        { if (mode != "auto" && mode != "preserve" && mode != "free") throw new ArgumentException("Mode must be auto, preserve or free."); }

        private static void RejectLink(string path)
        {
            try { if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Development settings path cannot be a link: " + path); }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }

        // A deliberately narrow JSON grammar keeps JsonUtility and System.Text.Json equally strict.
        private sealed class SchemaReader
        {
            private readonly string _text;
            private int _position;
            internal SchemaReader(string text) { _text = text; }
            internal void Validate() { Object(); White(); if (_position != _text.Length) Fail(); }
            private void Object()
            {
                Expect('{');
                var fields = new HashSet<string>(StringComparer.Ordinal);
                if (Take('}')) Fail();
                do
                {
                    var name = String();
                    if (!fields.Add(name)) Fail();
                    Expect(':');
                    if (name == "schema_version" || name == "default_mode") String();
                    else throw new IOException("Unknown development settings field: " + name + "; preserve the file and use a compatible version.");
                } while (Take(','));
                Expect('}');
                if (fields.Count != 2) Fail();
            }
            private string String()
            {
                Expect('"');
                var value = new StringBuilder();
                while (_position < _text.Length)
                {
                    var c = _text[_position++];
                    if (c == '"') return value.ToString();
                    if (c < 32) Fail();
                    if (c == '\\')
                    {
                        if (_position >= _text.Length) Fail();
                        c = _text[_position++];
                        switch (c)
                        {
                            case '"': case '\\': case '/': break;
                            case 'b': c = '\b'; break;
                            case 'f': c = '\f'; break;
                            case 'n': c = '\n'; break;
                            case 'r': c = '\r'; break;
                            case 't': c = '\t'; break;
                            case 'u':
                                if (_position + 4 > _text.Length) Fail();
                                int code;
                                if (!Int32.TryParse(_text.Substring(_position, 4), System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture, out code)) Fail();
                                c = (char)code; _position += 4; break;
                            default: Fail(); break;
                        }
                    }
                    value.Append(c);
                }
                Fail(); return null;
            }
            private bool Take(char c) { White(); if (_position < _text.Length && _text[_position] == c) { _position++; return true; } return false; }
            private void Expect(char c) { if (!Take(c)) Fail(); }
            private void White() { while (_position < _text.Length && (_text[_position] == ' ' || _text[_position] == '\t' || _text[_position] == '\r' || _text[_position] == '\n')) _position++; }
            private static void Fail() { throw new IOException("Malformed, duplicate or incomplete development settings JSON."); }
        }

#if UNITY_EDITOR
        private static DevelopmentPreferences Deserialize(string json) { return JsonUtility.FromJson<DevelopmentPreferences>(json); }
        private static string Serialize(DevelopmentPreferences settings) { return JsonUtility.ToJson(settings, true); }
#else
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
        private static DevelopmentPreferences Deserialize(string json) { return JsonSerializer.Deserialize<DevelopmentPreferences>(json, JsonOptions); }
        private static string Serialize(DevelopmentPreferences settings) { return JsonSerializer.Serialize(settings, JsonOptions); }
#endif
    }
}

