using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace ProGo.Tests
{
    public static class MenuPublicationOriginTests
    {
        private const BindingFlags Hidden = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
        private static object Info(Type type, FileStream stream)
        {
            Type infoType = type.GetNestedType("LegacyFileInfo", BindingFlags.NonPublic);
            var method = type.GetMethod("GetFileInformationByHandle", Hidden, null, new[] { typeof(SafeFileHandle), infoType.MakeByRefType() }, null);
            object[] args = { stream.SafeFileHandle, Activator.CreateInstance(infoType) };
            Check((bool)method.Invoke(null, args), "owned native info available");
            return args[1];
        }
        private static object Field(object info, string name)
        { return info.GetType().GetField(name, Hidden).GetValue(info); }
        private static string Origin(Type type, SafeFileHandle handle)
        { return (string)type.GetMethod("LegacyOrigin", Hidden).Invoke(null, new object[] { handle }); }
        private static void Check(bool condition, string name)
        { if (!condition) throw new InvalidOperationException("Menu publication regression: " + name); }
        public static int Run(Type type, string root)
        {
            int passed = 0;
            foreach (string scenario in new[] { "rename-creation", "foreign-same-bytes", "foreign-bytes", "witness-error", "witness-replace-race", "partial-bytes" }) {
                string work = Path.Combine(root, scenario), install = Path.Combine(work, "app"), programs = Path.Combine(work, "Programs");
                Directory.CreateDirectory(install); Directory.CreateDirectory(programs);
                File.WriteAllText(Path.Combine(install, "ProGo.exe"), "synthetic executable; never run");
                object manager = Activator.CreateInstance(type, new object[] { install, programs, Path.Combine(work, "Startup") });
                type.GetMethod("Install", new[] { typeof(bool), typeof(bool), typeof(bool) }).Invoke(manager, new object[] { true, true, false });
                string primary = Path.Combine(programs, "ProGo", "ProGo.lnk"), temporary = Path.Combine(programs, "ProGo", "owned-new.lnk");
                byte[] bytes = File.ReadAllBytes(primary);
                File.Move(primary, Path.Combine(programs, "ProGo", "retained-old-name.lnk"));
                File.WriteAllBytes(temporary, scenario == "partial-bytes" ? new byte[] { 123 } : bytes);
                // Own synthetic file only. Distinguishes the cached target creation from temp
                // creation; actual native rename (not receipt tampering) changes it on this FS.
                File.SetCreationTimeUtc(temporary, new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                int calls = 0; bool raceRefused = false; string captured = null;
                var sentinel = new InvalidOperationException("synthetic original witness error");
                using (var owned = new FileStream(temporary, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete)) {
                    object before = Info(type, owned);
                    string beforeOrigin = Origin(type, owned.SafeFileHandle);
                    File.Move(temporary, primary);
                    object after = Info(type, owned);
                    Check(Equals(Field(before,"Volume"),Field(after,"Volume")) && Equals(Field(before,"IndexHigh"),Field(after,"IndexHigh")) && Equals(Field(before,"IndexLow"),Field(after,"IndexLow")), "rename preserves native object on retained handle");
                    if (scenario == "rename-creation") Check(!Equals(Field(before,"Creation"),Field(after,"Creation")) && beforeOrigin != Origin(type,owned.SafeFileHandle), "real owned rename changes creation origin");
                    string expectedOrigin = Origin(type, owned.SafeFileHandle);
                    if (scenario == "foreign-same-bytes" || scenario == "foreign-bytes") {
                        DateTime creation = File.GetCreationTimeUtc(primary);
                        File.Move(primary,Path.Combine(work,"retained-owned.lnk"));
                        File.WriteAllBytes(primary,scenario == "foreign-same-bytes" ? bytes : new byte[] { 7, 8, 9 });
                        File.SetCreationTimeUtc(primary,creation);
                    }
                    string actualBefore = Convert.ToBase64String(File.ReadAllBytes(primary));
                    Action<SafeFileHandle> witness = delegate(SafeFileHandle handle) {
                        calls++; captured = Origin(type,handle);
                        if (scenario == "witness-error") throw sentinel;
                        if (scenario == "witness-replace-race") {
                            try { File.Move(primary,Path.Combine(work,"unexpected-race.lnk")); }
                            catch (IOException) { raceRefused = true; }
                        }
                    };
                    var method = type.GetMethod("WitnessPublishedLink", Hidden, null,
                        new[] { typeof(string), typeof(byte[]), typeof(FileStream), before.GetType(), typeof(Action<SafeFileHandle>) }, null);
                    Check(method != null, "exact publication verifier signature");
                    Exception failure = null;
                    try { method.Invoke(manager,new object[] { primary, bytes, owned, before, witness }); }
                    catch (TargetInvocationException error) { failure = error.InnerException; }
                    bool foreign = scenario == "foreign-same-bytes" || scenario == "foreign-bytes";
                    if (foreign || scenario == "partial-bytes") {
                        Check(failure is IOException && calls == 0, "conflict or real partial bytes refuse before receipt witness");
                        Check(Convert.ToBase64String(File.ReadAllBytes(primary)) == actualBefore, "refusal never overwrites foreign or partial bytes");
                        if (scenario == "foreign-same-bytes") {
                            using (var current = new FileStream(primary,FileMode.Open,FileAccess.Read,FileShare.Read)) {
                                Check(Convert.ToBase64String(bytes) == actualBefore && Origin(type,current.SafeFileHandle) != expectedOrigin, "identical bytes and copied creation cannot impersonate native object");
                            }
                        }
                    } else if (scenario == "witness-error") {
                        Check(Object.ReferenceEquals(failure,sentinel) && calls == 1, "exact original witness error propagates without replay");
                    } else {
                        Check(failure == null && calls == 1 && captured == expectedOrigin, "full post-publication origin comes from retained owned handle");
                        Check(Convert.ToBase64String(File.ReadAllBytes(primary)) == Convert.ToBase64String(bytes), "exact created bytes preserved");
                        if (scenario == "witness-replace-race") Check(raceRefused && !File.Exists(Path.Combine(work,"unexpected-race.lnk")), "publication read pin blocks external replacement through callback");
                    }
                }
                using (var reopened = new FileStream(primary,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) { Check(reopened.CanRead, "all owned witness and published handles close"); }
                passed++;
                Console.WriteLine("PASS: Menu publication origin " + scenario);
            }
            return passed;
        }
    }
}
