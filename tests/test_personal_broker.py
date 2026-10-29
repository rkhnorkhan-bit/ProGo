import importlib.util
import os
import pathlib
import shutil
import struct
import subprocess
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location('broker_image', ROOT / 'scripts/personal_broker_image.py')
IMAGE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(IMAGE)


def native_image(payload=b'sealed payload', manifest=b'<assembly>asInvoker</assembly>'):
    resource = bytearray()

    def directory(tree):
        start = len(resource)
        resource.extend(bytes(16 + 8 * len(tree)))
        struct.pack_into('<H', resource, start + 14, len(tree))
        for i, (name, value) in enumerate(sorted(tree.items())):
            if isinstance(value, dict):
                target = directory(value) | 0x80000000
            else:
                target = len(resource)
                resource.extend(bytes(16))
                offset = len(resource)
                resource.extend(value)
                resource.extend(bytes((-len(resource)) % 4))
                struct.pack_into('<II', resource, target, 0x3000 + offset, len(value))
            struct.pack_into('<II', resource, start + 16 + i * 8, name, target)
        return start

    directory({10: {101: {1033: payload}, 102: {1033: b'signature'}}, 24: {1: {1033: manifest}}})
    data = bytearray(2048)
    data[:2] = b'MZ'
    struct.pack_into('<I', data, 60, 128)
    data[128:132] = b'PE\0\0'
    struct.pack_into('<HH', data, 132, 0x8664, 3)
    struct.pack_into('<H', data, 148, 240)
    opt = 152
    struct.pack_into('<H', data, opt, 0x20b)
    struct.pack_into('<I', data, opt + 108, 16)
    struct.pack_into('<II', data, opt + 128, 0x3000, len(resource))
    for i, (name, va, raw, flags) in enumerate(((b'.text', 0x1000, 512, 0x60000020),
                                               (b'.rdata', 0x2000, 1024, 0x40000040),
                                               (b'.rsrc', 0x3000, 1536, 0x40000040))):
        off = opt + 240 + i * 40
        data[off:off + 8] = name.ljust(8, b'\0')
        struct.pack_into('<IIII', data, off + 8, 512, va, 512, raw)
        struct.pack_into('<I', data, off + 36, flags)
    data[512] = 0xc3
    data[1024:1034] = b'public-key'
    data[1536:1536 + len(resource)] = resource
    return data


class PersonalBrokerTests(unittest.TestCase):
    def test_native_identity_binds_code_key_manifest_and_loader_header(self):
        original = native_image()
        digest, resources = IMAGE.inspect(original)
        self.assertEqual(resources(10, 101), b'sealed payload')
        self.assertEqual(digest, IMAGE.inspect(native_image(b'new sealed payload'))[0])
        for offset in (512, 1024, 152 + 16):
            changed = bytearray(original)
            changed[offset] ^= 1
            self.assertNotEqual(digest, IMAGE.inspect(changed)[0])
        self.assertNotEqual(digest, IMAGE.inspect(native_image(manifest=b'<assembly>foreign</assembly>'))[0])

    def test_relocation_directory_cannot_escape_hashed_section(self):
        original = native_image()
        changed = bytearray(original)
        struct.pack_into('<II', changed, 152+152, 0x31e0, 12)
        struct.pack_into('<IIHH', changed, 1536+0x1e0, 0x1000, 12, 0xa010, 0)
        with self.assertRaises(ValueError):
            IMAGE.inspect(changed)
        # A real bounded relocation section can move when UpdateResource grows
        # resources; its directory length and bytes remain part of identity.
        original.extend(bytes(1024))
        original[2560:3072] = original[512:1024]
        struct.pack_into('<I', original, 152+240+20, 2560)
        struct.pack_into('<H', original, 134, 4)
        off = 152+240+3*40
        original[off:off+8] = b'.reloc\0\0'
        struct.pack_into('<IIII', original, off+8, 12, 0x4000, 512, 2048)
        struct.pack_into('<I', original, off+36, 0x42000040)
        struct.pack_into('<II', original, 152+152, 0x4000, 12)
        struct.pack_into('<IIHH', original, 2048, 0x1000, 12, 0xa010, 0)
        digest = IMAGE.inspect(original)[0]
        moved = bytearray(original)
        struct.pack_into('<I', moved, off+12, 0x5000)
        struct.pack_into('<I', moved, 152+152, 0x5000)
        self.assertEqual(digest, IMAGE.inspect(moved)[0])
        # Model resource growth plus raw/RVA relocation placement by UpdateResource.
        moved.extend(original[2048:2560])
        moved[2048:2560] = bytes(512)
        struct.pack_into('<I', moved, off+20, 3072)
        resource_section = 152+240+2*40
        struct.pack_into('<I', moved, resource_section+8, 1024)
        struct.pack_into('<I', moved, resource_section+16, 1024)
        struct.pack_into('<I', moved, 152+132, 1024)
        self.assertEqual(digest, IMAGE.inspect(moved)[0])
        for directory in ((0x31e0,12),(0x4002,10),(0x4000,13),(0,0)):
            changed = bytearray(original)
            struct.pack_into('<II', changed, 152+152, *directory)
            with self.assertRaises(ValueError): IMAGE.inspect(changed)
        changed = bytearray(original)
        struct.pack_into('<I', changed, 152+156, 10)
        self.assertNotEqual(digest, IMAGE.inspect(changed)[0])
        changed = bytearray(original); changed[2056] ^= 1
        self.assertNotEqual(digest, IMAGE.inspect(changed)[0])

    def test_managed_tls_executable_resources_overlap_and_truncation_are_refused(self):
        for offset, value in ((152 + 224, 1), (152 + 184, 1), (152 + 144, 1),
                              (152 + 240 + 2 * 40 + 36, 0x60000040),
                              (152 + 240 + 2 * 40 + 20, 512)):
            changed = native_image()
            struct.pack_into('<I', changed, offset, value)
            with self.assertRaises(ValueError):
                IMAGE.inspect(changed)
        image = native_image()
        for size in (0, 64, 200, 600, len(image)-1):
            with self.assertRaises(ValueError):
                IMAGE.inspect(image[:size])

    def test_actual_portable_launcher_policy_under_hostile_environment(self):
        compiler = shutil.which('cc')
        if compiler is None:
            self.skipTest('portable native compiler unavailable')
        harness = r'''
#include <stdio.h>
#include "personal_broker_policy.h"
static int check(int ok,int line) { if(!ok) fprintf(stderr,"policy failure %d\n",line); return ok; }
#define C(x) do { if(!check((x),__LINE__)) return 1; } while(0)
int main(void) {
    unsigned i; wchar_t env[4096]; pg_exchange x={0,0,0,0};
    C(pg_fixture_leaf(L"ProGo-native-broker-fixture-0123456789abcdef0123456789abcdef"));
    C(!pg_fixture_leaf(L"ProGo-native-broker-fixture-0123456789abcdef0123456789abcdeg"));
    C(!pg_fixture_leaf(L"ProGo-native-broker-fixture-0123456789abcdef0123456789abcde"));
    C(!pg_fixture_leaf(L"ProGo-native-broker-fixture-0123456789abcdef0123456789abcdef0"));
    C(!pg_fixture_leaf(L"ProGo-native-broker-fixture-0123456789abcdef0123456789abcdeF"));
    C(pg_route(L"\\\\.\\pipe\\ProGo-personal-0123456789abcdef0123456789abcdef"));
    C(!pg_route(L"\\\\remote\\pipe\\ProGo-personal-0123456789abcdef0123456789abcdef"));
    C(!pg_route(L"\\\\.\\pipe\\ProGo-personal-0123456789abcdef0123456789abcdeg"));
    for(i=0;i<6;i++) {
        int facts[6]={1,1,1,1,1,1}; facts[i]=0;
        C(!pg_admit(&x,PG_INSTALL,facts[0],facts[1],facts[2],facts[3],facts[4],facts[5]));
        C(x.phase==0 && x.claimed==0);
    }
    C(!pg_admit(&x,3,1,1,1,1,1,1)); C(!pg_claim(&x,PG_INSTALL)); C(!pg_settle(&x,0));
    C(pg_admit(&x,PG_INSTALL,1,1,1,1,1,1)); C(!pg_admit(&x,PG_INSTALL,1,1,1,1,1,1));
    C(!pg_claim(&x,PG_UNINSTALL)); C(pg_claim(&x,PG_INSTALL)); C(!pg_claim(&x,PG_INSTALL));
    C(pg_heartbeat(&x,1,1)); C(!pg_heartbeat(&x,0,1)); C(!pg_heartbeat(&x,1,0));
    C(!pg_settle(&x,2)); C(pg_settle(&x,0)); C(!pg_settle(&x,0)); C(!pg_heartbeat(&x,1,1));
    C(pg_environment(env,4096,L"C:\\Windows",L"C:\\Windows\\System32",L"C:\\fixture\\scratch"));
    for(i=0;env[i] || env[i+1];i++) putchar(env[i]?(char)env[i]:'\n');
    C(!pg_environment(env,4,L"C:\\Windows",L"C:\\Windows\\System32",L"C:\\fixture"));
    C(!pg_environment(env,4096,L"C:\\Windows",L"C:\\Windows\\System32",L"bad\npath"));
    return 0;
}
'''
        with tempfile.TemporaryDirectory(prefix='ProGo-native-policy-') as temp:
            source = pathlib.Path(temp) / 'policy.c'
            executable = pathlib.Path(temp) / 'policy'
            source.write_text(harness)
            subprocess.run([compiler, '-std=c11', '-Wall', '-Wextra', '-Werror', '-I', str(ROOT/'native'),
                            str(source), '-o', str(executable)], check=True, capture_output=True, text=True)
            environment = dict(os.environ, APPDOMAIN_MANAGER_ASM='foreign', APPDOMAIN_MANAGER_TYPE='foreign',
                               COR_ENABLE_PROFILING='1', COR_PROFILER_PATH='foreign', COMPLUS_Version='foreign',
                               DOTNET_STARTUP_HOOKS='foreign')
            result = subprocess.run([str(executable)], env=environment, check=True, capture_output=True, text=True)
        rows = result.stdout.splitlines()
        self.assertEqual(rows, ['PATH=C:\\Windows\\System32',
                                'PSModulePath=C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\Modules',
                                'SystemRoot=C:\\Windows', 'TEMP=C:\\fixture\\scratch',
                                'TMP=C:\\fixture\\scratch', 'WINDIR=C:\\Windows'])

    def test_fixture_diagnostic_wire_from_actual_native_serializer(self):
        # Execute the consumed C serializer/macros, not a separately retyped oracle.
        import re
        compiler = shutil.which('cc')
        if not compiler:
            self.skipTest('existing C compiler unavailable')
        native = (ROOT/'native/personal_broker.c').read_text()
        block = native.split('#ifdef PROGO_BROKER_NATIVE_FIXTURE', 1)[1].split('#else', 1)[0]
        launch = (ROOT/'tests/NativeBrokerLaunchTests.ps1').read_text(encoding='utf-8-sig')
        pattern = re.search(r"\$diagnostic -cmatch '([^']+)'", launch).group(1)
        harness = r'''#include <stdio.h>
#include <stddef.h>
typedef unsigned long DWORD;
static DWORD last_error;
#define ERROR_SUCCESS 0
#define STD_OUTPUT_HANDLE 0
#define GetLastError() last_error
#define SetLastError(e) (last_error=(e))
#define GetStdHandle(x) (x)
#define sprintf_s snprintf
static int WriteFile(int h,const char *s,DWORD n,DWORD *written,void *unused) {
    (void)h; (void)unused; *written=n; return fwrite(s,1,n,stdout)==n;
}
''' + block + r'''
int main(void) {
    PG_STAGE(PG_D_ASSIGN); SetLastError(5); PG_ERROR(); SetLastError(99);
    PG_FLAG(created); pg_diag.temp_match=0; pg_diag.long_known=1; pg_diag.long_match=1;
    pg_diag_write();
    PG_STAGE(PG_D_DONE); PG_FLAG(assigned); PG_FLAG(resumed);
    pg_diag.error=0; pg_diag.exit_known=1; pg_diag.host_exit=7;
    pg_diag.account_ok=1; pg_diag.active=0; pg_diag.identity_result=3; pg_diag_write(); return 0;
}
'''
        with tempfile.TemporaryDirectory(prefix='ProGo-diag-contract-') as temp:
            source = pathlib.Path(temp)/'diag.c'
            executable = pathlib.Path(temp)/'diag'
            source.write_text(harness)
            subprocess.run([compiler, '-std=c11', '-Wall', '-Wextra', '-Werror',
                            str(source), '-o', str(executable)], check=True, capture_output=True)
            output = subprocess.run([str(executable)], check=True, capture_output=True, text=True).stdout
        lines = output.splitlines(keepends=True)
        self.assertEqual(len(lines), 2)
        for line in lines:
            self.assertLess(len(line.encode()), 512)
            self.assertRegex(line, pattern)
        self.assertIn('error=5 ', lines[0])  # cleanup did not overwrite immediate error
        self.assertIn('temp_match=0 long_known=1 long_match=1', lines[0])
        self.assertIn('created=1 assigned=0 resumed=0', lines[0])
        self.assertIn('stage=27 ', lines[1])
        self.assertIn('exit_known=1 host_exit=7 account_ok=1 active=0', lines[1])
        self.assertIn('identity_result=3', lines[1])
        for bad in (re.sub(r'stage=[0-9]+', 'stage=99', lines[0]),
                    lines[0] + 'extra', lines[0].replace('error=5', 'error=secret'),
                    lines[0].replace('temp_match=0', 'temp_match=2'),
                    lines[0].replace('v=2', 'v=1'),
                    lines[0].replace('identity_result=0', 'identity_result=4')):
            self.assertIsNone(re.search(pattern, bad))
        fixture = native.split('static int pg_fixture_host(void)', 1)[1].split('\n#endif', 1)[0]
        self.assertIn('if(!pg_fixture_temp_identity(root,temporary)) return 1;', fixture)
        self.assertNotIn('if(_wcsnicmp(root,temporary', fixture)
        self.assertNotIn('if(pg_diag.long_match', fixture)
        self.assertLess(launch.index('PG_NATIVE_PARENT exit='), launch.index('Check-NativeLaunch ($nativeExit -eq 0)'))

    def test_consumed_fixture_identity_with_faulted_directory_backend(self):
        # Synthetic Win32 backend: real admission/pinning/identity C bodies run.
        # This models alternate spellings; it does NOT prove Windows short names.
        compiler = shutil.which('cc')
        if not compiler:
            self.skipTest('existing C compiler unavailable')
        native = (ROOT/'native/personal_broker.c').read_text()
        primitives = native.split('static int pg_hold(', 1)[1].split('static int pg_token(', 1)[0]
        admission = native.split('static int pg_fixture_pin_directory(', 1)[1].split('static int pg_fixture_host(', 1)[0]
        harness = r'''#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <wchar.h>
#include <wctype.h>
#include "personal_broker_policy.h"
typedef unsigned long DWORD;
typedef struct { DWORD dwHighDateTime,dwLowDateTime; } FT;
typedef struct { DWORD dwFileAttributes,dwVolumeSerialNumber,nFileIndexHigh,nFileIndexLow; FT ftCreationTime; } BY_HANDLE_FILE_INFORMATION;
typedef struct handle { int object,queries,closed; } *HANDLE;
#define INVALID_HANDLE_VALUE ((HANDLE)-1)
#define FILE_READ_ATTRIBUTES 1
#define GENERIC_READ 2
#define FILE_SHARE_READ 4
#define OPEN_EXISTING 8
#define FILE_FLAG_OPEN_REPARSE_POINT 16
#define FILE_FLAG_BACKUP_SEMANTICS 32
#define FILE_ATTRIBUTE_REPARSE_POINT 64
#define FILE_ATTRIBUTE_DIRECTORY 128
#define DRIVE_FIXED 3
#define ERROR_ALREADY_EXISTS 183
#define ERROR_SUCCESS 0
static DWORD error;
#define GetLastError() error
#define SetLastError(e) (error=(e))
static struct { DWORD error; int identity_result; } pg_diag;
#define PG_ERROR() (pg_diag.error=GetLastError())
static struct { HANDLE pins[256]; unsigned pin_count; wchar_t *directories[256]; unsigned directory_count; } g;
static struct handle handles[512];
static int handle_count,mode,identity_calls,create_calls,reparse_inspected;
static const wchar_t *leaf=L"ProGo-native-broker-fixture-0123456789abcdef0123456789abcdef";
static int foldcmp(const wchar_t *a,const wchar_t *b) {
    while(*a && towlower(*a)==towlower(*b)) { ++a; ++b; }
    return (int)towlower(*a)-(int)towlower(*b);
}
#define _wcsicmp foldcmp
static int wcscpy_s(wchar_t *out,size_t n,const wchar_t *in) {
    if(wcslen(in)>=n) return 1;
    wcscpy(out,in); return 0;
}
static int QueryDosDeviceW(const wchar_t *drive,wchar_t *out,DWORD n) {
    (void)n; if(drive[0]!=L'C' && drive[0]!=L'D') { error=2; return 0; }
    wcscpy(out,L"\\Device\\HarddiskVolume1"); return 1;
}
static int GetDriveTypeW(const wchar_t *p) { (void)p; return DRIVE_FIXED; }
static int object_for(const wchar_t *p) {
    wchar_t root[256];
    if(mode==13) {
        /* Case-sensitive lookup: TEMP is a different entry, a junction to Temp.
         * Opening its final Allowed child still returns the SAME directory id2. */
        swprintf(root,256,L"C:\\B\\Temp\\Allowed\\%ls",leaf);
        if(!wcscmp(p,L"C:\\")) return 1;
        if(!wcscmp(p,L"C:\\B")) return 6;
        if(!wcscmp(p,L"C:\\B\\Temp")) return 7;
        if(!wcscmp(p,L"C:\\B\\TEMP")) return 8;
        if(!wcscmp(p,L"C:\\B\\Temp\\Allowed") || !wcscmp(p,L"C:\\B\\TEMP\\Allowed")) return 2;
        if(!wcscmp(p,root)) return 3;
        return 0;
    }
    swprintf(root,256,L"C:\\TempLong\\%ls",leaf);
    if(!foldcmp(p,L"C:\\")) return 1;
    if(!foldcmp(p,L"C:\\TempLong") || !foldcmp(p,L"C:\\TEMPAL~1") || !foldcmp(p,L"C:\\Wrap\\Alias")) return 2;
    if(!foldcmp(p,root)) return 3;
    if(!foldcmp(p,L"C:\\Other")) return 4;
    if(!foldcmp(p,L"C:\\Wrap")) return 5;
    if(!foldcmp(p,L"D:\\")) return 10;
    if(!foldcmp(p,L"D:\\Temp")) return 11;
    return 0;
}
static HANDLE CreateFileW(const wchar_t *path,DWORD access,DWORD share,void *security,DWORD disposition,DWORD flags,void *template_) {
    int id=object_for(path); (void)security; (void)template_;
    if(access!=FILE_READ_ATTRIBUTES || share!=FILE_SHARE_READ || disposition!=OPEN_EXISTING ||
       flags!=(FILE_FLAG_OPEN_REPARSE_POINT|FILE_FLAG_BACKUP_SEMANTICS)) abort();
    if(!id || (mode==5 && id==2) || (mode==12 && !foldcmp(path,L"C:\\TEMPAL~1"))) { error=(mode==5 || mode==12)?5:2; return INVALID_HANDLE_VALUE; }
    handles[handle_count]=(struct handle){id,0,0}; return &handles[handle_count++];
}
static int CloseHandle(HANDLE h) { if(!h || h==INVALID_HANDLE_VALUE || h->closed) abort(); h->closed=1; error=99; return 1; }
static int GetFileInformationByHandle(HANDLE h,BY_HANDLE_FILE_INFORMATION *out) {
    int call; if(!h || h->closed) abort(); h->queries++;
    call=h->queries>1?++identity_calls:0;
    if((mode==6 && call==1) || (mode==7 && call==2)) { error=5; return 0; }
    memset(out,0,sizeof(*out)); out->dwFileAttributes=FILE_ATTRIBUTE_DIRECTORY;
    if((mode==1 && h->object==3) || (mode==2 && h->object==2) || (mode==3 && h->object==5) ||
       (mode==8 && h->object==1)) out->dwFileAttributes|=FILE_ATTRIBUTE_REPARSE_POINT;
    if(mode==13 && h->object==8) { out->dwFileAttributes|=FILE_ATTRIBUTE_REPARSE_POINT; ++reparse_inspected; }
    if(mode==9 && h->object==3) out->dwFileAttributes=0;
    out->dwVolumeSerialNumber=h->object>=10?2:1;
    out->nFileIndexLow=h->object==11?2:(DWORD)h->object;
    out->ftCreationTime.dwLowDateTime=(mode==11 && call==2)?2:1;
    if(mode==10 && call==2) out->nFileIndexHigh=1;
    return 1;
}
static int CreateDirectoryW(const wchar_t *p,void *security) { (void)p; (void)security; ++create_calls; return 0; }
#define GetProcessHeap() NULL
static void *HeapAlloc(void *heap,DWORD flags,size_t n) { (void)heap; (void)flags; return malloc(n); }
''' + 'static int pg_hold(' + primitives + 'static int pg_fixture_pin_directory(' + admission + r'''
static int run(int fault,const wchar_t *temp,int expected,int result) {
    wchar_t root[32768],temporary[32768]; int ok; unsigned i;
    memset(&g,0,sizeof(g)); memset(&pg_diag,0,sizeof(pg_diag)); memset(handles,0,sizeof(handles));
    handle_count=identity_calls=create_calls=reparse_inspected=0; mode=fault; error=0;
    swprintf(root,32768,L"C:\\TempLong\\%ls",leaf); wcscpy(temporary,temp);
    if(fault==13) {
        swprintf(root,32768,L"C:\\B\\Temp\\Allowed\\%ls",leaf);
        if(object_for(L"C:\\B\\Temp")==object_for(L"C:\\B\\TEMP") ||
           object_for(L"C:\\B\\Temp\\Allowed")!=object_for(L"C:\\B\\TEMP\\Allowed")) return 6;
    }
    ok=pg_fixture_temp_identity(root,temporary);
    if(fault==13 && (reparse_inspected!=1 || identity_calls!=0)) {
        fprintf(stderr,"case_sensitive admitted=%d reparse_ancestor_inspected=%d\n",ok,reparse_inspected);
        return 7;
    }
    if(ok!=expected || pg_diag.identity_result!=result || create_calls) return 1;
    if((fault==6 || fault==7) && pg_diag.error!=5) return 2;
    if(expected && identity_calls!=2) return 3;
    for(i=0;i<g.pin_count;i++) if(g.pins[i]->closed) return 4;
    /* Same retained-pin lifetime as pg_main cleanup, no helper release on success. */
    for(i=g.pin_count;i>0;--i) CloseHandle(g.pins[i-1]);
    for(i=g.directory_count;i>0;--i) free(g.directories[i-1]);
    for(i=0;i<(unsigned)handle_count;i++) if(!handles[i].closed) return 5;
    return 0;
}
#define C(x) do { if(!(x)) { fprintf(stderr,"identity fixture assertion line %d\n",__LINE__); return 1; } } while(0)
int main(void) {
    (void)pg_environment; (void)pg_operation; (void)pg_route; (void)pg_admit;
    (void)pg_claim; (void)pg_settle; (void)pg_heartbeat; (void)pg_pin_tree;
    C(!run(0,L"C:\\TempLong\\",1,3));
    C(!run(0,L"C:\\TEMPAL~1\\",1,3));
    C(!run(0,L"C:\\Other\\",0,2));
    C(!run(0,L"D:\\Temp\\",0,2));
    C(!run(1,L"C:\\TEMPAL~1\\",0,1)); /* fixture reparse leaf */
    C(!run(2,L"C:\\TEMPAL~1\\",0,1)); /* fixture parent reparse */
    C(!run(3,L"C:\\Wrap\\Alias\\",0,1)); /* temp ancestor reparse */
    C(!run(8,L"C:\\TempLong\\",0,1)); /* volume root reparse */
    C(!run(9,L"C:\\TempLong\\",0,1)); /* not a directory */
    C(!run(0,L"C:\\Missing\\",0,1));
    C(!run(5,L"C:\\TEMPAL~1\\",0,1)); /* access denied */
    C(!run(6,L"C:\\TEMPAL~1\\",0,2)); /* first identity query fails */
    C(!run(7,L"C:\\TEMPAL~1\\",0,2)); /* second identity query fails */
    C(!run(10,L"C:\\TEMPAL~1\\",0,2)); /* different high file index */
    C(!run(11,L"C:\\TEMPAL~1\\",0,2)); /* different creation identity */
    C(!run(12,L"C:\\TEMPAL~1\\",0,1)); /* only temp spelling inaccessible */
    C(!run(13,L"C:\\B\\TEMP\\Allowed",0,1)); /* case-sensitive cache collision */
    puts("PASS synthetic consumed fixture pin/identity vectors17; Windows alias proof NOT_RUN");
    return 0;
}
'''
        with tempfile.TemporaryDirectory(prefix='ProGo-fixture-identity-') as temp:
            source = pathlib.Path(temp)/'identity.c'
            executable = pathlib.Path(temp)/'identity'
            source.write_text(harness)
            built = subprocess.run([compiler, '-std=c11', '-Wall', '-Wextra', '-Werror',
                                    '-I', str(ROOT/'native'), str(source), '-o', str(executable)],
                                   capture_output=True, text=True)
            self.assertEqual(built.returncode, 0, built.stderr)
            result = subprocess.run([str(executable)], capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn('vectors17; Windows alias proof NOT_RUN', result.stdout)

    def test_diagnostic_preprocessor_is_fixture_only(self):
        compiler = shutil.which('cc')
        if not compiler:
            self.skipTest('existing C compiler unavailable')
        # Empty SDK headers permit preprocessing only; this is NOT a Windows build.
        with tempfile.TemporaryDirectory(prefix='ProGo-diag-preprocess-') as temp:
            for name in ('windows.h', 'shellapi.h', 'shlobj.h', 'bcrypt.h', 'broker_key.h'):
                (pathlib.Path(temp)/name).write_text('')
            command = [compiler, '-E', '-P', '-x', 'c', '-I', temp,
                       str(ROOT/'native/personal_broker.c')]
            production = subprocess.run(command, check=True, capture_output=True, text=True).stdout
            fixture = subprocess.run(command[:-1] + ['-DPROGO_BROKER_NATIVE_FIXTURE', command[-1]],
                                     check=True, capture_output=True, text=True).stdout
        for token in ('PG_NATIVE_DIAG', 'GetLongPathNameW', 'pg_diag_write', 'pg_diag.stage', 'pg_fixture_temp_identity', 'pg_fixture_pin_directory'):
            self.assertNotIn(token, production)
            self.assertIn(token, fixture)
        self.assertIn('LOAD_LIBRARY_SEARCH_SYSTEM32', production)
        self.assertIn('CREATE_SUSPENDED|CREATE_UNICODE_ENVIRONMENT|CREATE_NO_WINDOW', production)

    def test_consumed_factory_bootstrap_and_fixture_boundaries(self):
        native = (ROOT/'native/personal_broker.c').read_text()
        managed = (ROOT/'src/PersonalBroker.cs').read_text()
        driver = (ROOT/'scripts/PersonalBroker.ps1').read_text(encoding='utf-8-sig')
        install = (ROOT/'scripts/Install-ProGo.ps1').read_text(encoding='utf-8-sig')
        uninstall = (ROOT/'scripts/Uninstall-ProGo.ps1').read_text(encoding='utf-8-sig')
        self.assertIn('::OpenPersonalInstallerTransaction()', install)
        self.assertIn('::OpenPersonalUninstallerTransaction()', uninstall)
        self.assertIn('EnterNativeBroker($BrokerPipe)', driver)
        self.assertIn('-Personal -NoStartup -PayloadDirectory $PSScriptRoot', driver)
        self.assertIn('CreateProcessW(engine,command,NULL,NULL,FALSE,CREATE_SUSPENDED|CREATE_UNICODE_ENVIRONMENT', native)
        self.assertIn('AssignProcessToJobObject(g.job,pi->hProcess)', native)
        self.assertIn('pg_start_host(script,g.system,scratch,arguments,&pi)', native)
        imports = (ROOT/'src/ApplicationShortcuts.cs').read_text(encoding='utf-8-sig')
        self.assertEqual(imports.count('[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]'), 3)
        self.assertEqual(imports.count('[DllImport("ktmw32.dll",'), 3)
        launch = (ROOT/'tests/NativeBrokerLaunchTests.ps1').read_text(encoding='utf-8-sig')
        self.assertEqual(launch.count('$unknownTree = $false'), 1)
        self.assertNotIn('$script:unknownTree = $false', launch)
        self.assertIn('if (-not $script:unknownTree -and', launch)
        self.assertIn('later normal exit cannot clear unknown tree', launch)
        self.assertIn('timeout and launcher kill/join retain unknown tree', launch)
        self.assertNotIn('ExecutionPolicy', driver)
        self.assertNotIn('GetEnvironmentVariable', managed)
        for token in ('BrokerServerPid(pipe.SafePipeHandle', 'native.Digest()', 'rsa.VerifyData',
                      'BrokerLinked(workerToken,originalToken)', 'BrokerLinked(currentToken,originalToken)',
                      'transaction.HasSettledRegistration', 'transaction.disposed', 'new NativeRegistrationStore'):
            self.assertIn(token, managed)
        tests = (ROOT/'scripts/Test-ProGo.ps1').read_text(encoding='utf-8-sig')
        for fixture in ('NativeBrokerLaunchTests.ps1','PersonalBrokerTests.ps1','NativeRegistrationTransactionTests.ps1'):
            self.assertEqual(tests.count("'tests\\"+fixture+"'"), 1)


if __name__ == '__main__':
    unittest.main()
