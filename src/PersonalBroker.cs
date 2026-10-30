// Compiled into ProGo.exe. Never loaded from source by a privileged consumer.
// The native, sealed launcher starts the System32 PowerShell host before this
// code runs. No check in managed Main is used to prevent pre-CLR code execution.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace ProGo {
    public sealed partial class ApplicationShortcuts {
        public sealed partial class PersonalInstallTransaction {
            private static readonly object brokerGate = new object();
            private static NativeBrokerSession activeBroker;
            private static bool brokerEntered;
            private static byte[] AdmitBootstrapImage(byte[] image,string origin) {
                string[] anchor=origin.TrimStart('\uFEFF').Split(new[]{'\n'},2);
                var native=new PersonalBootstrapImage(image);
                if(anchor.Length!=2 || native.Digest()!=anchor[0]) throw new SecurityException("Personal broker native origin differs.");
                byte[] archive=native.Resource(10,101),signature=native.Resource(10,102);
                using(var rsa=new RSACryptoServiceProvider()) {
                    rsa.PersistKeyInCsp=false; rsa.FromXmlString(anchor[1]);
                    if(!rsa.VerifyData(archive,"SHA256",signature)) throw new SecurityException("Personal broker payload seal differs.");
                }
                return archive;
            }
#if PROGO_PERSONAL_INSTALL_FIXTURE
            public static bool CheckFixtureSeal(byte[] image,string origin) { AdmitBootstrapImage(image,origin); return true; }
            public static byte[] CorruptFixtureArchive(byte[] image) {
                byte[] archive=new PersonalBootstrapImage(image).Resource(10,101),copy=(byte[])image.Clone();
                for(int offset=0;offset<=copy.Length-archive.Length;offset++) {
                    int n=0; while(n<archive.Length && copy[offset+n]==archive[n]) n++;
                    if(n==archive.Length) {copy[offset+archive.Length-1]^=1;return copy;}
                }
                throw new IOException("Fixture archive not found.");
            }
            public static NativeBrokerSession EnterFixtureBroker(string root,string caseName,string payloadVersion,string[] inventory,string[] hashes,string operation,string proof,string fault) {
                lock(brokerGate) {
                    if(brokerEntered) throw new SecurityException("Personal broker replay refused.");
                    brokerEntered=true;
                    activeBroker=new NativeBrokerSession(root,caseName,payloadVersion,inventory,hashes,operation,proof,fault);
                    return activeBroker;
                }
            }
#endif
            public static NativeBrokerSession EnterNativeBroker(string routingPipe) {
                lock (brokerGate) {
                    if (brokerEntered) throw new SecurityException("Personal broker replay refused.");
                    brokerEntered = true;
                    activeBroker = new NativeBrokerSession(routingPipe);
                    return activeBroker;
                }
            }
            public static PersonalInstallTransaction OpenPersonalInstallerTransaction() { return ClaimBroker("install"); }
            public static PersonalInstallTransaction OpenPersonalUninstallerTransaction() { return ClaimBroker("uninstall"); }
            private static PersonalInstallTransaction ClaimBroker(string operation) {
                lock (brokerGate) {
                    if (activeBroker == null) throw new SecurityException("Personal transaction requires its native broker.");
                    return activeBroker.Claim(operation);
                }
            }

            public sealed class NativeBrokerSession : IDisposable {
                private readonly System.Diagnostics.Stopwatch lifetime = System.Diagnostics.Stopwatch.StartNew();
                private readonly System.Collections.Generic.List<IDisposable> held = new System.Collections.Generic.List<IDisposable>();
                private System.IO.Pipes.NamedPipeClientStream pipe;
                private PersonalTokenHandle worker, original, originalToken;
                private readonly SafeFileHandle[] originalRoots=new SafeFileHandle[4];
                private string[] rootPaths;
                private string runtimeRoot, dataRoot, payloadRoot, owner, programs, startup, payloadVersion;
                private string[] names, expected;
                private bool claimed, closed, completed;
                private PersonalInstallTransaction transaction;
                public string Operation { get; private set; }
                private readonly object gate = new object();
#if PROGO_PERSONAL_INSTALL_FIXTURE
                private bool fixture,fixtureAlive;
                private string fixtureRoot,fixtureCase,fixtureFault;
                internal NativeBrokerSession(string root,string caseName,string version,string[] inventory,string[] hashes,string operation,string proof,string fault) {
                    if(proof!="valid" || (operation!="install" && operation!="uninstall")) throw new SecurityException("Synthetic native broker evidence refused.");
                    fixture=true; fixtureAlive=true; fixtureRoot=root; fixtureCase=caseName; fixtureFault=fault;
                    Operation=operation; payloadVersion=version; names=inventory; expected=hashes; owner=CurrentSid();
                    string scope=Path.Combine(root,caseName);
                    runtimeRoot=Path.Combine(scope,"program","ProGo"); dataRoot=Path.Combine(scope,"data","ProGo"); payloadRoot=Path.Combine(scope,"payload");
                    programs=Path.Combine(scope,"menu"); startup=Path.Combine(scope,"startup");
                }
                public void RevokeFixture() { lock(gate) { if(!fixture) throw new SecurityException("Not a fixture broker."); fixtureAlive=false; } }
#endif
                internal NativeBrokerSession(string routingPipe) {
                    try {
                        if (!System.Text.RegularExpressions.Regex.IsMatch(routingPipe ?? "", @"^\\\\\.\\pipe\\ProGo-personal-[a-f0-9]{32}$"))
                            throw new SecurityException("Personal broker routing differs.");
                        // Routing text confers no authority. Identify the actual
                        // server using the kernel before accepting its fixed frame.
                        pipe = new System.IO.Pipes.NamedPipeClientStream(".", routingPipe.Substring(9),
                            System.IO.Pipes.PipeDirection.InOut, System.IO.Pipes.PipeOptions.Asynchronous, TokenImpersonationLevel.Identification);
                        pipe.Connect(5000);
                        uint server;
                        if (!BrokerServerPid(pipe.SafePipeHandle, out server)) throw new SecurityException("Personal broker endpoint unavailable.");
                        worker = BrokerOpenProcess(0x00101040u, false, server); held.Add(worker);
                        if (worker == null || worker.IsInvalid) throw new SecurityException("Personal broker process unavailable.");
                        string imagePath = BrokerProcessImage(worker);
                        var image = OpenLegacyRestartRead(imagePath); held.Add(image);
                        byte[] imageBytes = BrokerRead(image, 64 * 1024 * 1024);
                        byte[] origin;
                        using (var input = typeof(PersonalInstallTransaction).Assembly.GetManifestResourceStream("ProGo.PersonalBrokerOrigin")) {
                            if (input == null || input.Length > 8192) throw new SecurityException("Personal broker code origin is absent.");
                            origin = new byte[(int)input.Length]; BrokerReadExact(input, origin);
                        }
                        byte[] archive = AdmitBootstrapImage(imageBytes,System.Text.Encoding.UTF8.GetString(origin));
                        byte[] frame = new byte[8]; BrokerPipeRead(pipe,frame);
                        uint operation = BitConverter.ToUInt32(frame,0), originalPid = BitConverter.ToUInt32(frame,4);
                        Operation = operation == 1 ? "install" : operation == 2 ? "uninstall" : null;
                        if (Operation == null || originalPid == 0 || originalPid == server) throw new SecurityException("Personal broker request differs.");
                        for(int i=0;i<originalRoots.Length;i++) {
                            byte[] reference=new byte[8]; BrokerPipeRead(pipe,reference);
                            long raw=BitConverter.ToInt64(reference,0);
                            if(raw==0 || raw==-1 || !BrokerDuplicate(worker,new IntPtr(raw),PersonalProcess(),out originalRoots[i],0,false,2))
                                throw new SecurityException("Personal original folder handle unavailable.");
                            held.Add(originalRoots[i]);
                        }
                        byte[] nativeReference=new byte[8]; BrokerPipeRead(pipe,nativeReference);
                        SafeFileHandle nativeImage;
                        if(!BrokerDuplicate(worker,new IntPtr(BitConverter.ToInt64(nativeReference,0)),PersonalProcess(),out nativeImage,0,false,2))
                            throw new SecurityException("Personal native image pin unavailable.");
                        held.Add(nativeImage);
                        if(LegacyOrigin(nativeImage)!=LegacyOrigin(image.SafeFileHandle))
                            throw new SecurityException("Personal native image namespace was substituted.");
                        original = BrokerOpenProcess(0x00101000u,false,originalPid); held.Add(original);
                        if (original == null || original.IsInvalid) throw new SecurityException("Personal broker original process unavailable.");
                        using (var originalImage = OpenLegacyRestartRead(BrokerProcessImage(original)))
                            if (LegacyOrigin(originalImage.SafeFileHandle) != LegacyOrigin(image.SafeFileHandle))
                                throw new SecurityException("Personal broker original code origin differs.");
                        if (!OpenPersonalProcessToken(original.DangerousGetHandle(),0x000Eu,out originalToken))
                            throw new SecurityException("Personal broker original token unavailable.");
                        held.Add(originalToken);
                        owner = BrokerSid(originalToken);
                        CheckIdentity();
                        string programBase=BrokerFolder(new Guid("905e63b6-c1bf-494e-b29c-65b732d3d21a"),null);
                        string dataBase=BrokerFolder(new Guid("f1b32785-6fba-4fcf-9d55-7b8e7f157091"),originalToken);
                        runtimeRoot = Path.Combine(programBase,"ProGo");
                        dataRoot = Path.Combine(dataBase,"ProGo");
                        programs = BrokerFolder(new Guid("a77f5d77-2e2b-44c3-a6a2-aba601054a51"),originalToken);
                        startup = BrokerFolder(new Guid("b97d20bb-f46a-4c97-ba10-5e3608430854"),originalToken);
                        rootPaths=new[]{programBase,dataBase,programs,startup};
                        CheckRootOrigins();
                        foreach(string folder in new[]{programs,startup}) {
                            for(string p=folder;p!=null;p=Path.GetDirectoryName(p)) held.Add(PinLegacySourceDirectory(p));
                        }
                        payloadRoot = Path.Combine(runtimeRoot,"backups","personal-broker","payload");
                        // Bootstrap code is retained independently of runtime leaves.
                        // A partial uninstall therefore never requires deleted code.
                        var retained = OpenLegacyRestartRead(Path.Combine(runtimeRoot,"backups","personal-broker","ProGo.Setup.exe")); held.Add(retained);
                        if (PhysicalPinHash(retained) != PhysicalPinHash(image)) throw new SecurityException("Personal retained bootstrap differs.");
                        ReadArchive(archive);
                        string assembly = typeof(PersonalInstallTransaction).Assembly.Location;
                        using (var loaded = OpenLegacyRestartRead(assembly))
                        using (var staged = OpenLegacyRestartRead(Path.Combine(payloadRoot,"ProGo.exe")))
                            if (LegacyOrigin(loaded.SafeFileHandle) != LegacyOrigin(staged.SafeFileHandle))
                                throw new SecurityException("Personal managed entry origin differs.");
                        Check();
                    } catch { Dispose(); throw; }
                }
                private void ReadArchive(byte[] archive) {
                    var required = new System.Collections.Generic.List<string>(LegacyOperationLeaves);
                    required.AddRange(new[] {"README.md","README.ru.md","LICENSE.md","PersonalBroker.ps1"});
                    var hashes = new System.Collections.Generic.Dictionary<string,string>(StringComparer.Ordinal);
                    using (var input = new MemoryStream(archive,false))
                    using (var reader = new BinaryReader(input,System.Text.Encoding.ASCII)) {
                        if (archive.Length > 64 * 1024 * 1024 || archive.Length < 12 || System.Text.Encoding.ASCII.GetString(reader.ReadBytes(8)) != "PGPK0001" ||
                            reader.ReadUInt32() != required.Count) throw new SecurityException("Personal sealed inventory differs.");
                        for (int i=0;i<required.Count;i++) {
                            int n=reader.ReadUInt16(); uint length=reader.ReadUInt32();
                            if (n<1 || n>511 || length>input.Length-input.Position-n) throw new SecurityException("Personal sealed entry exceeds bounds.");
                            string name=System.Text.Encoding.ASCII.GetString(reader.ReadBytes(n));
                            if (!required.Contains(name) || hashes.ContainsKey(name)) throw new SecurityException("Personal sealed entry differs.");
                            byte[] bytes=reader.ReadBytes((int)length);
                            string hash; using(var sha=SHA256.Create()) hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");
                            var stream=OpenLegacyRestartRead(Path.Combine(payloadRoot,name)); held.Add(stream);
                            if (PhysicalPinHash(stream)!=hash) throw new SecurityException("Personal staged payload differs from native seal.");
                            hashes.Add(name,hash);
                            if(name=="VERSION") payloadVersion=System.Text.Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF').Trim();
                        }
                        if(input.Position!=input.Length) throw new SecurityException("Personal sealed inventory has trailing input.");
                    }
                    required.Remove("PersonalBroker.ps1"); names=required.ToArray(); expected=new string[names.Length];
                    for(int i=0;i<names.Length;i++) expected[i]=hashes[names[i]];
                }
                private void CheckIdentity() {
                    if (CurrentSid()!=owner || BrokerWait(original,0)!=258 || BrokerWait(worker,0)!=258)
                        throw new SecurityException("Personal original context ended or changed.");
                    PersonalTokenHandle currentToken,workerToken;
                    if(!OpenPersonalProcessToken(PersonalProcess(),0x0008u,out currentToken)) throw new SecurityException("Personal process token unavailable.");
                    using(currentToken) {
                        if(!OpenPersonalProcessToken(worker.DangerousGetHandle(),0x0008u,out workerToken)) throw new SecurityException("Personal native token unavailable.");
                        using(workerToken) {
                            if(BrokerNumber(currentToken,18)!=2 || BrokerNumber(workerToken,18)!=2 || BrokerNumber(originalToken,18)!=3 ||
                                BrokerSid(workerToken)!=owner || BrokerNumber(currentToken,12)!=BrokerNumber(originalToken,12) ||
                                BrokerNumber(workerToken,12)!=BrokerNumber(originalToken,12) || BrokerAuthentication(currentToken)!=BrokerAuthentication(workerToken))
                                throw new SecurityException("Personal elevation/session identity differs.");
                            BrokerLinked(workerToken,originalToken); BrokerLinked(currentToken,originalToken);
                        }
                    }
                }
                private void CheckRootOrigins() {
                    for(int i=0;i<originalRoots.Length;i++) using(var current=PinLegacySourceDirectory(rootPaths[i]))
                        if(BrokerDirectoryOrigin(current)!=BrokerDirectoryOrigin(originalRoots[i]))
                            throw new SecurityException("Personal original folder namespace differs.");
                }
                internal void Check() {
                    lock(gate) {
#if PROGO_PERSONAL_INSTALL_FIXTURE
                        if(fixture) {
                            if(closed || completed || !fixtureAlive || lifetime.ElapsedMilliseconds>=300000 || CurrentSid()!=owner)
                                throw new SecurityException("Personal broker authority expired.");
                            return;
                        }
#endif
                        if(closed || completed || lifetime.ElapsedMilliseconds>=300000 || pipe==null || !pipe.IsConnected)
                            throw new SecurityException("Personal broker authority expired.");
                        CheckIdentity();
                        CheckRootOrigins();
                        byte[] heartbeat=BitConverter.GetBytes(2);
                        var write=pipe.WriteAsync(heartbeat,0,heartbeat.Length);
                        if(!write.Wait(5000)) { pipe.Dispose(); throw new IOException("Personal broker heartbeat did not settle."); }
                        write.GetAwaiter().GetResult(); BrokerPipeRead(pipe,heartbeat);
                        if(BitConverter.ToInt32(heartbeat,0)!=2) throw new SecurityException("Personal broker heartbeat differs.");
                    }
                }
                internal PersonalInstallTransaction Claim(string operation) {
                    lock(gate) {
                        Check();
                        if(claimed || operation!=Operation) throw new SecurityException("Personal broker operation/replay refused.");
                        claimed=true;
#if PROGO_PERSONAL_INSTALL_FIXTURE
                        if(fixture) transaction=CreateFixtureCore(fixtureRoot,fixtureCase,runtimeRoot,dataRoot,payloadRoot,owner,names,expected,payloadVersion,fixtureFault,false);
                        else
#endif
                            transaction=new PersonalInstallTransaction(runtimeRoot,dataRoot,payloadRoot,owner,names,expected,payloadVersion,
                                new NativeRegistrationStore(runtimeRoot,payloadVersion,owner));
                        transaction.ProgramsRoot=programs; transaction.StartupRoot=startup;
                        lock(shortcutRootsGate) {boundProgramsRoot=programs;boundStartupRoot=startup;}
                        transaction.brokerCheck=Check;
                        transaction.Validate(runtimeRoot,dataRoot,payloadRoot);
                        return transaction;
                    }
                }
                public void Complete(int result) {
                    lock(gate) {
                        if(closed || completed || result<0 || result>1) throw new SecurityException("Personal broker result/replay refused.");
                        if(result==0) {
                            Check();
                            if(!claimed || transaction==null || !transaction.HasSettledRegistration || !transaction.disposed)
                                throw new SecurityException("Personal broker transaction has not settled.");
                        }
                        completed=true;
#if PROGO_PERSONAL_INSTALL_FIXTURE
                        if(fixture) return;
#endif
                        byte[] bytes=BitConverter.GetBytes(result);
                        var write=pipe.WriteAsync(bytes,0,bytes.Length);
                        if(!write.Wait(5000)) { pipe.Dispose(); throw new IOException("Personal broker result did not settle."); }
                        write.GetAwaiter().GetResult();
                    }
                }
                public void Dispose() {
                    PersonalInstallTransaction issued;
                    lock(gate) {
                        if(closed) return;
                        closed=true;
                        issued=transaction;
                        if(pipe!=null) pipe.Dispose();
                        for(int i=held.Count-1;i>=0;i--) if(held[i]!=null) held[i].Dispose();
                        held.Clear();
                    }
                    if(issued!=null) issued.Dispose();
                }
            }
            private static void BrokerReadExact(Stream input,byte[] bytes) {
                int n=0; while(n<bytes.Length) { int got=input.Read(bytes,n,bytes.Length-n); if(got==0) throw new EndOfStreamException("Personal broker input ended."); n+=got; }
            }
            private static byte[] BrokerRead(FileStream input,int max) {
                if(input.Length<1 || input.Length>max) throw new IOException("Personal broker image exceeds bounds.");
                input.Position=0; byte[] bytes=new byte[(int)input.Length]; BrokerReadExact(input,bytes); return bytes;
            }
            private static void BrokerPipeRead(System.IO.Pipes.NamedPipeClientStream input,byte[] bytes) {
                int n=0; var clock=System.Diagnostics.Stopwatch.StartNew();
                while(n<bytes.Length) {
                    var read=input.ReadAsync(bytes,n,bytes.Length-n);
                    int remaining=(int)Math.Max(0,5000-clock.ElapsedMilliseconds);
                    if(!read.Wait(remaining)) { input.Dispose(); throw new IOException("Personal broker frame deadline exceeded."); }
                    int got=read.GetAwaiter().GetResult(); if(got==0) throw new EndOfStreamException("Personal broker frame ended."); n+=got;
                }
            }
            [DllImport("kernel32.dll",EntryPoint="GetNamedPipeServerProcessId",SetLastError=true)]
            [return:MarshalAs(UnmanagedType.Bool)] private static extern bool BrokerServerPid(SafePipeHandle pipe,out uint pid);
            [DllImport("kernel32.dll",EntryPoint="OpenProcess",SetLastError=true)]
            private static extern PersonalTokenHandle BrokerOpenProcess(uint access,[MarshalAs(UnmanagedType.Bool)] bool inherit,uint pid);
            [DllImport("kernel32.dll",EntryPoint="DuplicateHandle",SetLastError=true)]
            [return:MarshalAs(UnmanagedType.Bool)] private static extern bool BrokerDuplicate(PersonalTokenHandle source,IntPtr handle,IntPtr target,out SafeFileHandle duplicate,uint access,[MarshalAs(UnmanagedType.Bool)] bool inherit,uint options);
            [DllImport("kernel32.dll",EntryPoint="WaitForSingleObject",SetLastError=true)]
            private static extern uint BrokerWait(PersonalTokenHandle process,uint timeout);
            [DllImport("kernel32.dll",EntryPoint="QueryFullProcessImageNameW",CharSet=CharSet.Unicode,SetLastError=true)]
            [return:MarshalAs(UnmanagedType.Bool)] private static extern bool BrokerImage(PersonalTokenHandle process,uint flags,System.Text.StringBuilder image,ref int size);
            [DllImport("advapi32.dll",EntryPoint="GetTokenInformation",SetLastError=true)]
            [return:MarshalAs(UnmanagedType.Bool)] private static extern bool BrokerTokenInfo(PersonalTokenHandle token,int kind,IntPtr value,int size,out int needed);
            [DllImport("shell32.dll",EntryPoint="SHGetKnownFolderPath",PreserveSig=true)]
            private static extern int BrokerKnownFolder(ref Guid id,uint flags,IntPtr token,out IntPtr result);
            private static string BrokerProcessImage(PersonalTokenHandle process) {
                int size=32768; var text=new System.Text.StringBuilder(size);
                if(!BrokerImage(process,0,text,ref size)) throw new SecurityException("Personal process code origin unavailable.");
                string path=text.ToString(); BrokerLocalPath(path); return path;
            }
            private static string BrokerDirectoryOrigin(SafeFileHandle handle) {
                LegacyFileInfo info;
                if(handle==null || handle.IsInvalid || !GetFileInformationByHandle(handle,out info) || (info.Attributes&0x400)!=0 || (info.Attributes&0x10)==0)
                    throw new SecurityException("Personal original directory identity unavailable.");
                return info.Volume.ToString("X8")+info.IndexHigh.ToString("X8")+info.IndexLow.ToString("X8")+
                    info.Creation.dwHighDateTime.ToString("X8")+info.Creation.dwLowDateTime.ToString("X8");
            }
            private static byte[] BrokerTokenBytes(PersonalTokenHandle token,int kind,int size) {
                IntPtr p=Marshal.AllocHGlobal(size);
                try { int needed; if(!BrokerTokenInfo(token,kind,p,size,out needed) || needed>size) throw new SecurityException("Personal token facts unavailable.");
                    byte[] result=new byte[size]; Marshal.Copy(p,result,0,size); return result;
                } finally { Marshal.FreeHGlobal(p); }
            }
            private static int BrokerNumber(PersonalTokenHandle token,int kind) { return BitConverter.ToInt32(BrokerTokenBytes(token,kind,4),0); }
            private static string BrokerSid(PersonalTokenHandle token) { using(var identity=new WindowsIdentity(token.DangerousGetHandle())) {
                if(identity.User==null) throw new SecurityException("Personal user unavailable."); return identity.User.Value; } }
            private static long BrokerAuthentication(PersonalTokenHandle token) { return BitConverter.ToInt64(BrokerTokenBytes(token,10,56),8); }
            private static void BrokerLinked(PersonalTokenHandle elevated,PersonalTokenHandle limited) {
                byte[] raw=BrokerTokenBytes(elevated,19,IntPtr.Size);
                IntPtr handle=IntPtr.Size==8?new IntPtr(BitConverter.ToInt64(raw,0)):new IntPtr(BitConverter.ToInt32(raw,0));
                try {
                    using(var linked=new WindowsIdentity(handle)) {
                        // WindowsIdentity holds a duplicate; query the original
                        // returned linked-token handle, then close it exactly once.
                        long id; int needed; IntPtr p=Marshal.AllocHGlobal(56);
                        try {
                            if(!BrokerRawTokenInfo(handle,10,p,56,out needed)) throw new SecurityException("Personal linked token unavailable.");
                            id=Marshal.ReadInt64(p,8);
                        } finally { Marshal.FreeHGlobal(p); }
                        if(linked.User==null || linked.User.Value!=BrokerSid(limited) || id!=BrokerAuthentication(limited))
                            throw new SecurityException("Personal alternate administrator refused.");
                    }
                } finally { ClosePersonalToken(handle); }
            }
            [DllImport("advapi32.dll",EntryPoint="GetTokenInformation",SetLastError=true)]
            [return:MarshalAs(UnmanagedType.Bool)] private static extern bool BrokerRawTokenInfo(IntPtr token,int kind,IntPtr value,int size,out int needed);
            private static string BrokerFolder(Guid id,PersonalTokenHandle token) {
                IntPtr value;
                int result=BrokerKnownFolder(ref id,0x4000u,token==null?IntPtr.Zero:token.DangerousGetHandle(),out value);
                if(result!=0 || value==IntPtr.Zero) {
                    if(value!=IntPtr.Zero) Marshal.FreeCoTaskMem(value);
                    throw new SecurityException("Personal original KnownFolder unavailable.");
                }
                try {
                    string path=Canonical(Marshal.PtrToStringUni(value));
                    BrokerLocalPath(path);
                    return path;
                } finally { Marshal.FreeCoTaskMem(value); }
            }
            [DllImport("kernel32.dll",EntryPoint="GetDriveTypeW",CharSet=CharSet.Unicode)]
            private static extern uint BrokerDrive(string root);
            [DllImport("kernel32.dll",EntryPoint="QueryDosDeviceW",CharSet=CharSet.Unicode,SetLastError=true)]
            private static extern uint BrokerDevice(string drive,System.Text.StringBuilder target,int capacity);
            private static void BrokerLocalPath(string path) {
                var target=new System.Text.StringBuilder(32768);
                if(path.Length<3 || path[1]!=':' || path[2]!='\\' || BrokerDevice(path.Substring(0,2),target,target.Capacity)==0 ||
                    !target.ToString().StartsWith(@"\Device\HarddiskVolume",StringComparison.OrdinalIgnoreCase) || BrokerDrive(path.Substring(0,3))!=3)
                    throw new SecurityException("Personal path is not on a fixed local volume.");
            }
        }
    }
}
